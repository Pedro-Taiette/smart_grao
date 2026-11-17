"""Rodada barata: backbone congelado + regressao logistica sobre as features.

    python -m vision.probe extract --backbone dinov2_b_reg4 --size 224
    python -m vision.probe fit --backbone dinov2_b_reg4 --size 224

extract grava runs/features/<backbone>_<tam>.npz (features de todas as imagens do manifesto, com a
mesma entrada da inferencia: imagem inteira no quadrado). fit treina a logistica no treino de cada
divisao (pesos de amostra iguais ao balanceamento do treino) e mede validacao e "test". A parte
"final" (craze_final) nunca e lida aqui.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd
import torch
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import StandardScaler
from torch.utils.data import DataLoader

from vision.config import CLASS_TO_INDEX, MANIFEST_PATH, RUNS_DIR, SPLITS_DIR
from vision.data import LeafDataset, eval_transform, load_split, sample_weights
from vision.metrics import summarize
from vision.model import create_model, normalization

FEATURES_DIR = RUNS_DIR / "features"
PROBE_DIR = RUNS_DIR / "probe"
SPLITS = ["lodo_craze_halves.csv", "lodo_cds_sem_craze.csv", "lodo_seasonal_sem_craze.csv"]
C_GRID = [0.01, 0.1, 1.0]


def features_path(backbone: str, size: int) -> Path:
    return FEATURES_DIR / f"{backbone}_{size}.npz"


@torch.no_grad()
def extract(backbone: str, size: int, batch_size: int = 64, workers: int = 6) -> Path:
    device = "cuda" if torch.cuda.is_available() else "cpu"
    manifest = pd.read_csv(MANIFEST_PATH, dtype=str, keep_default_na=False)
    frame = manifest[manifest["excluded"] == ""].reset_index(drop=True)
    model = create_model(backbone, pretrained=True, num_classes=0, img_size=size).to(device).eval()
    mean, std = normalization(backbone)
    loader = DataLoader(LeafDataset(frame, eval_transform(size, mean, std)), batch_size=batch_size,
                        num_workers=workers, pin_memory=device == "cuda")
    chunks = []
    for index, (images, _, _) in enumerate(loader):
        with torch.autocast(device_type=device, dtype=torch.float16, enabled=device == "cuda"):
            chunks.append(model(images.to(device, non_blocking=True)).float().cpu().numpy())
        if index % 40 == 0:
            print(f"[{backbone}] {index * batch_size}/{len(frame)}", flush=True)
    out = features_path(backbone, size)
    out.parent.mkdir(parents=True, exist_ok=True)
    np.savez(out, paths=frame["path"].to_numpy(), features=np.concatenate(chunks).astype(np.float32))
    return out


def fit(backbone: str, size: int, splits: list[str]) -> dict:
    data = np.load(features_path(backbone, size), allow_pickle=True)
    index = {p: i for i, p in enumerate(data["paths"])}
    features = data["features"]
    results = {"backbone": backbone, "size": size, "dim": int(features.shape[1]), "splits": {}}
    for split_name in splits:
        frame = load_split(SPLITS_DIR / split_name)
        parts = {part: frame[frame["split"] == part].reset_index(drop=True) for part in ("train", "val", "test")}
        x = {part: features[[index[p] for p in f["path"]]] for part, f in parts.items()}
        y = {part: f["label"].map(CLASS_TO_INDEX).to_numpy() for part, f in parts.items()}
        scaler = StandardScaler().fit(x["train"])
        weights = sample_weights(parts["train"])
        weights = weights / weights.mean()
        by_c = {}
        for c in C_GRID:
            clf = LogisticRegression(C=c, max_iter=3000)
            clf.fit(scaler.transform(x["train"]), y["train"], sample_weight=weights)
            entry = {}
            for part in ("val", "test"):
                probs = clf.predict_proba(scaler.transform(x[part]))
                s = summarize(y[part], probs)
                entry[part] = {"macro_f1": s["macro_f1"], "accuracy": s["accuracy"],
                               "recall_cercosporiose": s["recall_cercosporiose"],
                               "precision_cercosporiose": s["per_class"]["cercosporiose"]["precision"],
                               "recall_saudavel": s["per_class"]["saudavel"]["recall"],
                               "ece": s["calibration"]["ece"],
                               "confusion": s["confusion_matrix"]["rows_true_cols_pred"]}
            by_c[str(c)] = entry
        results["splits"][split_name] = by_c
    out = PROBE_DIR / f"{backbone}_{size}.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(results, indent=2), encoding="utf-8")
    return results


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("action", choices=["extract", "fit"])
    parser.add_argument("--backbone", required=True)
    parser.add_argument("--size", type=int, default=224)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--splits", nargs="*", default=SPLITS)
    args = parser.parse_args()
    if args.action == "extract":
        print(extract(args.backbone, args.size, args.batch_size))
        return
    results = fit(args.backbone, args.size, args.splits)
    for split_name, by_c in results["splits"].items():
        for c, entry in by_c.items():
            print(split_name, f"C={c}", "val", entry["val"]["macro_f1"], "test",
                  {k: v for k, v in entry["test"].items() if k != "confusion"})


if __name__ == "__main__":
    main()
