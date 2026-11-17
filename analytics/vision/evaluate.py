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

from vision.config import CLASS_TO_INDEX, CLASSES, DATASET_DOMAIN, IMAGENET_MEAN, IMAGENET_STD
from vision.data import LeafDataset, eval_transform, load_split
from vision.metrics import summarize
from vision.model import load_checkpoint, normalization


TTA_VIEWS = {"none": 1, "flip": 2, "flip4": 4}


def eval_loader(frame: pd.DataFrame, size: int, norm: tuple, device: str, batch_size: int = 64,
                workers: int = 4, persistent: bool = False) -> DataLoader:
    return DataLoader(LeafDataset(frame, eval_transform(size, *norm)), batch_size=batch_size,
                      shuffle=False, num_workers=workers, pin_memory=device == "cuda",
                      persistent_workers=persistent and workers > 0)


@torch.no_grad()
def predict_logits(model: torch.nn.Module, frame: pd.DataFrame, size: int, device: str,
                   norm: tuple = (IMAGENET_MEAN, IMAGENET_STD), batch_size: int = 64, workers: int = 4,
                   tta: str = "none", loader: DataLoader | None = None) -> np.ndarray:
    """Logits na entrada da inferencia. tta: media dos logits com espelhamento horizontal ("flip")
    ou horizontal, vertical e os dois ("flip4")."""
    model.eval()
    loader = loader or eval_loader(frame, size, norm, device, batch_size, workers)
    outputs = []
    for images, _, _ in loader:
        images = images.to(device, non_blocking=True)
        views = [images, images.flip(3), images.flip(2), images.flip(2, 3)][:TTA_VIEWS[tta]]
        with torch.autocast(device_type=device, enabled=device == "cuda"):
            logits = torch.stack([model(view).float() for view in views]).mean(0)
        outputs.append(logits.cpu().numpy())
    return np.concatenate(outputs) if outputs else np.zeros((0, len(CLASSES)))


def softmax(logits: np.ndarray, temperature: float = 1.0) -> np.ndarray:
    z = logits / temperature
    z = np.exp(z - z.max(axis=1, keepdims=True))
    return z / z.sum(axis=1, keepdims=True)


def predict(model: torch.nn.Module, frame: pd.DataFrame, size: int, device: str,
            batch_size: int = 64, workers: int = 4, norm: tuple = (IMAGENET_MEAN, IMAGENET_STD),
            tta: str = "none") -> np.ndarray:
    return softmax(predict_logits(model, frame, size, device, norm, batch_size, workers, tta))


def fit_temperature(logits: np.ndarray, y_true: np.ndarray) -> float:
    """Temperatura que minimiza a log-verossimilhanca negativa (busca em grade log, 0,3 a 10)."""
    grid = np.exp(np.linspace(np.log(0.3), np.log(10.0), 400))
    picked = logits[np.arange(len(y_true)), y_true]
    nll = [np.mean(np.log(np.exp((logits - picked[:, None]) / t).sum(axis=1))) for t in grid]
    return float(grid[int(np.argmin(nll))])


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


def run(checkpoint: Path, split: Path, part: str, out: Path | None, workers: int = 4,
        tta: str = "none", temperature: float = 1.0, fit: bool = False) -> dict:
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model, meta = load_checkpoint(checkpoint, device)
    frame = load_split(split)
    frame = frame[frame["split"] == part].reset_index(drop=True)
    logits = predict_logits(model, frame, meta["img_size"], device, normalization(meta["arch"]),
                            workers=workers, tta=tta)
    if fit:
        temperature = fit_temperature(logits, frame["label"].map(CLASS_TO_INDEX).to_numpy())
    probs = softmax(logits, temperature)
    result = {
        "checkpoint": str(checkpoint),
        "split_file": split.name,
        "part": part,
        "tta": tta,
        "temperature": temperature,
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
        np.save(out.with_suffix(".logits.npy"), logits)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", type=Path, required=True)
    parser.add_argument("--split", type=Path, required=True)
    parser.add_argument("--part", default="test", choices=["train", "val", "test", "final"],
                        help="final = craze_final: avaliar uma vez so, com a receita ja escolhida")
    parser.add_argument("--tta", default="none", choices=list(TTA_VIEWS))
    parser.add_argument("--temperature", type=float, default=1.0)
    parser.add_argument("--fit-temperature", action="store_true",
                        help="ajusta a temperatura nesta parte (use so no craze_dev, parte test)")
    parser.add_argument("--out", type=Path)
    parser.add_argument("--workers", type=int, default=4)
    args = parser.parse_args()
    suffix = "" if args.tta == "none" else f"_tta-{args.tta}"
    out = args.out or args.checkpoint.parent / f"eval_{args.split.stem}_{args.part}{suffix}.json"
    if args.fit_temperature and args.part == "final":
        raise SystemExit("a temperatura se ajusta no craze_dev (--part test), nunca no final")
    result = run(args.checkpoint, args.split, args.part, out, args.workers, args.tta, args.temperature,
                 args.fit_temperature)
    print(json.dumps({k: result[k] for k in ("n", "accuracy", "macro_f1", "recall_cercosporiose", "temperature")}
                     | {"ece": result["calibration"]["ece"]}))
    print(f"gravado em {out}")


if __name__ == "__main__":
    main()
