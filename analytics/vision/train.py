"""Treino do classificador (linha de base: EfficientNet-B0, fine-tuning completo).

    python -m vision.train --split data/splits/grouped.csv --run-name b0_grouped --epochs 25

Receita robusta (LP-FT): --lp-epochs treina so a cabeca com o backbone congelado (grava lp.pt, que
o vision.wise interpola com o fim do fine-tuning) e depois libera tudo com LR menor; --layer-decay reduz a LR das camadas iniciais; --ema guarda a media movel dos
pesos; --strong-aug, --mixup e --cutmix aumentam a variedade de foto.

--select val escolhe o checkpoint pelo F1 macro da validacao, com parada antecipada (como no 0.1.0).
--select last treina o numero fixo de epocas e guarda o fim (com EMA, os pesos da media): e o modo
do modelo de producao, que nao tem validacao. A parte "final" da divisao nunca e lida aqui.
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
from timm.data import Mixup
from timm.loss import SoftTargetCrossEntropy
from timm.optim import create_optimizer_v2
from timm.utils import ModelEmaV3
from torch.utils.data import DataLoader

from vision.config import CLASS_TO_INDEX, CLASSES, RUNS_DIR
from vision.data import LeafDataset, balanced_sampler, load_split, train_transform
from vision.evaluate import eval_loader, evaluate_frame, fit_temperature, predict_logits, softmax
from vision.metrics import summarize
from vision.model import create_model, load_checkpoint, normalization, save_checkpoint


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
    parser.add_argument("--arch", default="efficientnet_b0", help="nome do timm ou chave de model.BACKBONES")
    parser.add_argument("--img-size", type=int, default=224)
    parser.add_argument("--epochs", type=int, default=25, help="epocas de fine-tuning (sem contar as de LP)")
    parser.add_argument("--batch-size", type=int, default=48)
    parser.add_argument("--accum", type=int, default=1, help="passos de acumulacao de gradiente")
    parser.add_argument("--lr", type=float, default=3e-4)
    parser.add_argument("--weight-decay", type=float, default=1e-2)
    parser.add_argument("--label-smoothing", type=float, default=0.05)
    parser.add_argument("--patience", type=int, default=5)
    parser.add_argument("--select", choices=["val", "last"], default="val")
    parser.add_argument("--lp-epochs", type=int, default=0)
    parser.add_argument("--lp-lr", type=float, default=1e-3)
    parser.add_argument("--lp-clean", action="store_true", help="fase LP sem mixup/cutmix")
    parser.add_argument("--layer-decay", type=float, default=0.0, help="0 = sem decaimento por camada")
    parser.add_argument("--drop-path", type=float, default=0.0)
    parser.add_argument("--ema", type=float, default=0.0, help="decaimento da EMA (0 = sem EMA)")
    parser.add_argument("--strong-aug", action="store_true")
    parser.add_argument("--mixup", type=float, default=0.0, help="alfa do mixup (0 = desligado)")
    parser.add_argument("--cutmix", type=float, default=0.0, help="alfa do cutmix (0 = desligado)")
    parser.add_argument("--mix-prob", type=float, default=0.5)
    parser.add_argument("--grad-checkpointing", action="store_true")
    parser.add_argument("--freeze-blocks", type=int, default=0,
                        help="ViT: congela a entrada e os N primeiros blocos durante todo o treino")
    parser.add_argument("--balance", choices=["sampler", "weights", "none"], default="sampler",
                        help="sampler: amostragem balanceada; weights: perda com peso de classe")
    parser.add_argument("--balance-mode", default="label_dataset_sqrt",
                        choices=["label", "label_dataset_sqrt"],
                        help="como a amostragem equilibra (ver data.balanced_sampler)")
    parser.add_argument("--eval-test-each-epoch", action="store_true",
                        help="registra o F1 do teste no historico (so informativo)")
    parser.add_argument("--max-steps-per-epoch", type=int, default=0,
                        help="limita os passos por epoca (0 = epoca completa); para teste rapido")
    parser.add_argument("--workers", type=int, default=6)
    parser.add_argument("--seed", type=int, default=42)
    return parser.parse_args()


def set_backbone_trainable(model: torch.nn.Module, trainable: bool, freeze_blocks: int = 0) -> None:
    head = {id(p) for p in model.get_classifier().parameters()}
    for param in model.parameters():
        param.requires_grad = trainable or id(param) in head
    if trainable and freeze_blocks:
        frozen = [model.patch_embed, model.norm_pre, *model.blocks[:freeze_blocks]]
        for param in [p for module in frozen for p in module.parameters()]:
            param.requires_grad = False
        for name in ("cls_token", "reg_token", "pos_embed"):
            if getattr(model, name, None) is not None:
                getattr(model, name).requires_grad = False


def run_phase(model, loader, optimizer, scheduler, scaler, criterion, mixup, ema, args, device,
              steps_per_epoch) -> float:
    model.train()
    total_loss, seen = 0.0, 0
    optimizer.zero_grad(set_to_none=True)
    for step, (images, targets, _) in enumerate(loader):
        if step >= steps_per_epoch:
            break
        images = images.to(device, non_blocking=True)
        targets = targets.to(device, non_blocking=True)
        if mixup is not None:
            images, soft = mixup(images, targets)
        with torch.autocast(device_type=device, enabled=device == "cuda"):
            logits = model(images)
            loss = criterion(logits, soft if mixup is not None else targets)
        scaler.scale(loss / args.accum).backward()
        if (step + 1) % args.accum == 0:
            scaler.step(optimizer)
            scaler.update()
            optimizer.zero_grad(set_to_none=True)
            scheduler.step()
            if ema is not None:
                ema.update(model)
        total_loss += loss.item() * len(targets)
        seen += len(targets)
    return total_loss / max(1, seen)


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
    if args.select == "val" and not len(val_frame):
        raise SystemExit("--select val precisa de validacao; use --select last")

    norm = normalization(args.arch)
    sampler = None
    class_weights = None
    if args.balance == "sampler":
        sampler = balanced_sampler(train_frame, args.balance_mode)
    elif args.balance == "weights":
        counts = train_frame["label"].value_counts()
        class_weights = torch.tensor(
            [len(train_frame) / (len(CLASSES) * counts.get(c, 1)) for c in CLASSES],
            dtype=torch.float32, device=device)
    loader = DataLoader(LeafDataset(train_frame, train_transform(args.img_size, *norm, strong=args.strong_aug)),
                        batch_size=args.batch_size, sampler=sampler, shuffle=sampler is None,
                        num_workers=args.workers, pin_memory=device == "cuda", drop_last=True,
                        persistent_workers=args.workers > 0)

    model = create_model(args.arch, pretrained=True, img_size=args.img_size,
                         **({"drop_path_rate": args.drop_path} if args.drop_path else {})).to(device)
    if args.grad_checkpointing:
        model.set_grad_checkpointing(True)
    mixup = None
    if args.mixup or args.cutmix:
        mixup = Mixup(mixup_alpha=args.mixup, cutmix_alpha=args.cutmix, prob=args.mix_prob,
                      label_smoothing=args.label_smoothing, num_classes=len(CLASSES))
        criterion = SoftTargetCrossEntropy()
    else:
        criterion = torch.nn.CrossEntropyLoss(weight=class_weights, label_smoothing=args.label_smoothing)
    steps_per_epoch = len(loader)
    if args.max_steps_per_epoch:
        steps_per_epoch = min(steps_per_epoch, args.max_steps_per_epoch)
    updates_per_epoch = steps_per_epoch // args.accum
    scaler = torch.amp.GradScaler(enabled=device == "cuda")

    meta = {
        "arch": args.arch,
        "img_size": args.img_size,
        "classes": list(CLASSES),
        "mean": list(norm[0]),
        "std": list(norm[1]),
        "split_file": args.split.name,
        "args": {k: str(v) for k, v in vars(args).items()},
    }
    (run_dir / "config.json").write_text(json.dumps(meta, indent=2, ensure_ascii=False),
                                         encoding="utf-8")

    eval_workers = 2
    val_loader = (eval_loader(val_frame, args.img_size, norm, device, workers=eval_workers, persistent=True)
                  if len(val_frame) else None)
    test_loader = (eval_loader(test_frame, args.img_size, norm, device, workers=eval_workers, persistent=True)
                   if args.eval_test_each_epoch and len(test_frame) else None)

    def log_epoch(epoch: int, phase: str, loss: float, eval_model, started: float) -> dict:
        record = {"epoch": epoch, "phase": phase, "train_loss": round(loss, 4)}
        if len(val_frame):
            probs = softmax(predict_logits(eval_model, val_frame, args.img_size, device, norm,
                                           loader=val_loader))
            val = summarize(val_frame["label"].map(CLASS_TO_INDEX).to_numpy(), probs)
            record.update(val_macro_f1=val["macro_f1"], val_recall_cercosporiose=val["recall_cercosporiose"],
                          val_accuracy=val["accuracy"])
        if args.eval_test_each_epoch and len(test_frame):
            probs = softmax(predict_logits(eval_model, test_frame, args.img_size, device, norm,
                                           loader=test_loader))
            test = summarize(test_frame["label"].map(CLASS_TO_INDEX).to_numpy(), probs)
            record.update(test_macro_f1=test["macro_f1"], test_recall_cercosporiose=test["recall_cercosporiose"],
                          test_recall_saudavel=test["per_class"]["saudavel"]["recall"])
        record["seconds"] = round(time.time() - started, 1)
        history.append(record)
        print(json.dumps(record), flush=True)
        (run_dir / "history.json").write_text(json.dumps(history, indent=2), encoding="utf-8")
        return record

    history = []
    epoch = 0
    if args.lp_epochs:
        set_backbone_trainable(model, False)
        optimizer = torch.optim.AdamW([p for p in model.parameters() if p.requires_grad],
                                      lr=args.lp_lr, weight_decay=args.weight_decay)
        scheduler = cosine_with_warmup(optimizer, warmup_steps=max(1, updates_per_epoch // 4),
                                       total_steps=updates_per_epoch * args.lp_epochs)
        for _ in range(args.lp_epochs):
            epoch += 1
            started = time.time()
            lp_criterion = torch.nn.CrossEntropyLoss(label_smoothing=args.label_smoothing) if args.lp_clean else criterion
            loss = run_phase(model, loader, optimizer, scheduler, scaler, lp_criterion,
                             None if args.lp_clean else mixup, None, args, device, steps_per_epoch)
            log_epoch(epoch, "lp", loss, model, started)
        save_checkpoint(run_dir / "lp.pt", model, {**meta, "epoch": epoch, "phase": "lp"})
    set_backbone_trainable(model, True, args.freeze_blocks)

    optimizer = create_optimizer_v2(model, opt="adamw", lr=args.lr, weight_decay=args.weight_decay,
                                    layer_decay=args.layer_decay or None)
    scheduler = cosine_with_warmup(optimizer, warmup_steps=updates_per_epoch,
                                   total_steps=updates_per_epoch * args.epochs)
    ema = ModelEmaV3(model, decay=args.ema, use_warmup=True) if args.ema else None

    best_f1, best_epoch, stale = -1.0, -1, 0
    for _ in range(args.epochs):
        epoch += 1
        started = time.time()
        loss = run_phase(model, loader, optimizer, scheduler, scaler, criterion, mixup, ema, args,
                         device, steps_per_epoch)
        eval_model = ema.module if ema is not None else model
        record = log_epoch(epoch, "ft", loss, eval_model, started)
        if args.select == "last":
            continue
        if record["val_macro_f1"] > best_f1:
            best_f1, best_epoch, stale = record["val_macro_f1"], epoch, 0
            save_checkpoint(run_dir / "best.pt", eval_model,
                            {**meta, "epoch": epoch, "val_macro_f1": best_f1})
        else:
            stale += 1
            if stale >= args.patience:
                print(f"parada antecipada na epoca {epoch} (melhor: {best_epoch})", flush=True)
                break
    if args.select == "last":
        best_epoch = epoch
        best_f1 = history[-1].get("val_macro_f1")
        save_checkpoint(run_dir / "best.pt", ema.module if ema is not None else model,
                        {**meta, "epoch": epoch, "val_macro_f1": best_f1})

    del loader
    best_model, best_meta = load_checkpoint(run_dir / "best.pt", device)
    summary = {"best_epoch": best_epoch, "best_val_macro_f1": best_f1, "epochs_run": len(history)}
    for part, part_frame, part_loader in (("val", val_frame, val_loader), ("test", test_frame, test_loader)):
        if len(part_frame) == 0:
            continue
        logits = predict_logits(best_model, part_frame, args.img_size, device, norm, workers=eval_workers,
                                loader=part_loader)
        np.save(run_dir / f"logits_{part}.npy", logits)
        result = {"checkpoint": "best.pt", "split_file": args.split.name, "part": part,
                  "epoch": best_meta["epoch"],
                  "datasets_in_part": sorted(part_frame["dataset"].unique().tolist()),
                  **evaluate_frame(part_frame, softmax(logits))}
        if part == "val":
            result["temperature"] = fit_temperature(logits, part_frame["label"].map(CLASS_TO_INDEX).to_numpy())
        (run_dir / f"eval_{part}.json").write_text(json.dumps(result, indent=2, ensure_ascii=False),
                                                  encoding="utf-8")
        summary[part] = {k: result[k] for k in ("n", "accuracy", "macro_f1", "recall_cercosporiose")}
        summary[part]["recall_saudavel"] = result["per_class"]["saudavel"]["recall"]
        summary[part]["ece"] = result["calibration"]["ece"]
    (run_dir / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2), flush=True)


if __name__ == "__main__":
    main()
