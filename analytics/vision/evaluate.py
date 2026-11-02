"""Avaliacao de um checkpoint numa parte da divisao, com metricas gerais e por conjunto de origem.

    python -m vision.evaluate --checkpoint runs/<run>/best.pt --split data/splits/grouped.csv --part test
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd
import torch
from torch.utils.data import DataLoader

from vision.config import CLASS_TO_INDEX, CLASSES, DATASET_DOMAIN
from vision.data import LeafDataset, eval_transform, load_split
from vision.metrics import summarize
from vision.model import load_checkpoint


@torch.no_grad()
def predict(model: torch.nn.Module, frame: pd.DataFrame, size: int, device: str,
            batch_size: int = 64, workers: int = 4) -> np.ndarray:
    model.eval()
    loader = DataLoader(LeafDataset(frame, eval_transform(size)), batch_size=batch_size,
                        shuffle=False, num_workers=workers, pin_memory=device == "cuda",
                        persistent_workers=False)
    outputs = []
    for images, _, _ in loader:
        with torch.autocast(device_type=device, enabled=device == "cuda"):
            logits = model(images.to(device, non_blocking=True))
        outputs.append(torch.softmax(logits.float(), dim=1).cpu().numpy())
    return np.concatenate(outputs) if outputs else np.zeros((0, len(CLASSES)))


def evaluate_frame(frame: pd.DataFrame, probs: np.ndarray) -> dict:
    y_true = frame["label"].map(CLASS_TO_INDEX).to_numpy()
    result = summarize(y_true, probs)
    by_dataset = {}
    for dataset, index in frame.groupby("dataset").groups.items():
        positions = frame.index.get_indexer(index)
        entry = summarize(y_true[positions], probs[positions])
        entry["domain"] = DATASET_DOMAIN.get(dataset, "?")
        by_dataset[dataset] = entry
    result["by_dataset"] = by_dataset
    by_domain = {}
    for domain in sorted(set(frame["dataset"].map(DATASET_DOMAIN))):
        positions = np.flatnonzero(frame["dataset"].map(DATASET_DOMAIN).to_numpy() == domain)
        by_domain[domain] = summarize(y_true[positions], probs[positions])
    result["by_domain"] = by_domain
    return result


def run(checkpoint: Path, split: Path, part: str, out: Path | None, workers: int = 4) -> dict:
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model, meta = load_checkpoint(checkpoint, device)
    frame = load_split(split)
    frame = frame[frame["split"] == part].reset_index(drop=True)
    probs = predict(model, frame, meta["img_size"], device, workers=workers)
    result = {
        "checkpoint": str(checkpoint),
        "split_file": split.name,
        "part": part,
        "datasets_in_part": sorted(frame["dataset"].unique().tolist()),
        **evaluate_frame(frame, probs),
    }
    if out is not None:
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
        predictions = frame[["path", "dataset", "label"]].copy()
        predictions["pred"] = [CLASSES[i] for i in probs.argmax(axis=1)]
        for index, name in enumerate(CLASSES):
            predictions[f"p_{name}"] = probs[:, index].round(5)
        predictions.to_csv(out.with_suffix(".predictions.csv"), index=False)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", type=Path, required=True)
    parser.add_argument("--split", type=Path, required=True)
    parser.add_argument("--part", default="test", choices=["train", "val", "test"])
    parser.add_argument("--out", type=Path)
    parser.add_argument("--workers", type=int, default=4)
    args = parser.parse_args()
    out = args.out or args.checkpoint.parent / f"eval_{args.split.stem}_{args.part}.json"
    result = run(args.checkpoint, args.split, args.part, out, args.workers)
    print(json.dumps({k: result[k] for k in ("n", "accuracy", "macro_f1", "recall_cercosporiose")}))
    print(f"gravado em {out}")


if __name__ == "__main__":
    main()
