"""Treino da linha de base: EfficientNet-B0 pre-treinada no ImageNet, fine-tuning completo.

    python -m vision.train --split data/splits/grouped.csv --run-name b0_grouped --epochs 25

Escolhe o melhor checkpoint pelo F1 macro na validacao e para cedo quando ele nao melhora por
`--patience` epocas. No fim avalia o melhor checkpoint na validacao e no teste da mesma divisao.
"""

from __future__ import annotations

import argparse
import json
import math
import random
import time
from pathlib import Path

import numpy as np
import torch
from torch.utils.data import DataLoader

from vision.config import CLASS_TO_INDEX, CLASSES, RUNS_DIR
from vision.data import LeafDataset, balanced_sampler, load_split, train_transform
from vision.evaluate import evaluate_frame, predict
from vision.metrics import summarize
from vision.model import create_model, load_checkpoint, save_checkpoint


def seed_everything(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)


def cosine_with_warmup(optimizer, warmup_steps: int, total_steps: int):
    def factor(step: int) -> float:
        if step < warmup_steps:
            return (step + 1) / warmup_steps
        progress = (step - warmup_steps) / max(1, total_steps - warmup_steps)
        return 0.5 * (1 + math.cos(math.pi * min(1.0, progress)))
    return torch.optim.lr_scheduler.LambdaLR(optimizer, factor)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--split", type=Path, required=True)
    parser.add_argument("--run-name", required=True)
    parser.add_argument("--arch", default="efficientnet_b0")
    parser.add_argument("--img-size", type=int, default=224)
    parser.add_argument("--epochs", type=int, default=25)
    parser.add_argument("--batch-size", type=int, default=48)
    parser.add_argument("--lr", type=float, default=3e-4)
    parser.add_argument("--weight-decay", type=float, default=1e-2)
    parser.add_argument("--label-smoothing", type=float, default=0.05)
    parser.add_argument("--patience", type=int, default=5)
    parser.add_argument("--balance", choices=["sampler", "weights", "none"], default="sampler",
                        help="sampler: amostragem balanceada; weights: perda com peso de classe")
    parser.add_argument("--balance-mode", default="label_dataset_sqrt",
                        choices=["label", "label_dataset_sqrt"],
                        help="como a amostragem equilibra (ver data.balanced_sampler)")
    parser.add_argument("--max-steps-per-epoch", type=int, default=0,
                        help="limita os passos por epoca (0 = epoca completa); para teste rapido")
    parser.add_argument("--workers", type=int, default=6)
    parser.add_argument("--seed", type=int, default=42)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    seed_everything(args.seed)
    device = "cuda" if torch.cuda.is_available() else "cpu"
    run_dir = RUNS_DIR / args.run_name
    run_dir.mkdir(parents=True, exist_ok=True)

    frame = load_split(args.split)
    train_frame = frame[frame["split"] == "train"].reset_index(drop=True)
    val_frame = frame[frame["split"] == "val"].reset_index(drop=True)
    test_frame = frame[frame["split"] == "test"].reset_index(drop=True)
    print(f"treino {len(train_frame)} | validacao {len(val_frame)} | teste {len(test_frame)}",
          flush=True)

    sampler = None
    class_weights = None
    if args.balance == "sampler":
        sampler = balanced_sampler(train_frame, args.balance_mode)
    elif args.balance == "weights":
        counts = train_frame["label"].value_counts()
        class_weights = torch.tensor(
            [len(train_frame) / (len(CLASSES) * counts.get(c, 1)) for c in CLASSES],
            dtype=torch.float32, device=device)
    loader = DataLoader(LeafDataset(train_frame, train_transform(args.img_size)),
                        batch_size=args.batch_size, sampler=sampler, shuffle=sampler is None,
                        num_workers=args.workers, pin_memory=device == "cuda", drop_last=True,
                        persistent_workers=args.workers > 0)

    model = create_model(args.arch, pretrained=True).to(device)
    criterion = torch.nn.CrossEntropyLoss(weight=class_weights, label_smoothing=args.label_smoothing)
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=args.weight_decay)
    steps_per_epoch = len(loader)
    if args.max_steps_per_epoch:
        steps_per_epoch = min(steps_per_epoch, args.max_steps_per_epoch)
    scheduler = cosine_with_warmup(optimizer, warmup_steps=steps_per_epoch,
                                   total_steps=steps_per_epoch * args.epochs)
    scaler = torch.amp.GradScaler(enabled=device == "cuda")

    meta = {
        "arch": args.arch,
        "img_size": args.img_size,
        "classes": list(CLASSES),
        "split_file": args.split.name,
        "args": {k: str(v) for k, v in vars(args).items()},
    }
    (run_dir / "config.json").write_text(json.dumps(meta, indent=2, ensure_ascii=False),
                                         encoding="utf-8")

    history = []
    best_f1, best_epoch, stale = -1.0, -1, 0
    for epoch in range(1, args.epochs + 1):
        model.train()
        started = time.time()
        total_loss, seen = 0.0, 0
        for step, (images, targets, _) in enumerate(loader):
            if args.max_steps_per_epoch and step >= args.max_steps_per_epoch:
                break
            images = images.to(device, non_blocking=True)
            targets = targets.to(device, non_blocking=True)
            with torch.autocast(device_type=device, enabled=device == "cuda"):
                loss = criterion(model(images), targets)
            optimizer.zero_grad(set_to_none=True)
            scaler.scale(loss).backward()
            scaler.step(optimizer)
            scaler.update()
            scheduler.step()
            total_loss += loss.item() * len(targets)
            seen += len(targets)

        probs = predict(model, val_frame, args.img_size, device, workers=args.workers)
        val = summarize(val_frame["label"].map(CLASS_TO_INDEX).to_numpy(), probs)
        record = {
            "epoch": epoch,
            "train_loss": round(total_loss / max(1, seen), 4),
            "val_macro_f1": val["macro_f1"],
            "val_recall_cercosporiose": val["recall_cercosporiose"],
            "val_accuracy": val["accuracy"],
            "seconds": round(time.time() - started, 1),
        }
        history.append(record)
        print(json.dumps(record), flush=True)
        (run_dir / "history.json").write_text(json.dumps(history, indent=2), encoding="utf-8")
        if val["macro_f1"] > best_f1:
            best_f1, best_epoch, stale = val["macro_f1"], epoch, 0
            save_checkpoint(run_dir / "best.pt", model,
                            {**meta, "epoch": epoch, "val_macro_f1": best_f1})
        else:
            stale += 1
            if stale >= args.patience:
                print(f"parada antecipada na epoca {epoch} (melhor: {best_epoch})", flush=True)
                break

    best_model, best_meta = load_checkpoint(run_dir / "best.pt", device)
    summary = {"best_epoch": best_epoch, "best_val_macro_f1": best_f1, "epochs_run": len(history)}
    for part, part_frame in (("val", val_frame), ("test", test_frame)):
        if len(part_frame) == 0:
            continue
        probs = predict(best_model, part_frame, args.img_size, device, workers=args.workers)
        result = {"checkpoint": "best.pt", "split_file": args.split.name, "part": part,
                  "epoch": best_meta["epoch"],
                  "datasets_in_part": sorted(part_frame["dataset"].unique().tolist()),
                  **evaluate_frame(part_frame, probs)}
        (run_dir / f"eval_{part}.json").write_text(json.dumps(result, indent=2, ensure_ascii=False),
                                                  encoding="utf-8")
        summary[part] = {k: result[k] for k in ("n", "accuracy", "macro_f1", "recall_cercosporiose")}
    (run_dir / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2), flush=True)


if __name__ == "__main__":
    main()
