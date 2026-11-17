"""WiSE-FT: media nos pesos entre o fim da fase LP (backbone original + cabeca linear) e o modelo
ajustado. Alfa 0 = so LP, 1 = so fine-tuning. Interpolar devolve parte da robustez do backbone
pre-treinado fora do dominio sem perder muito dentro dele (Wortsman et al., 2022).

    python -m vision.wise --run runs/s_lpft --alphas 0.25 0.5 0.75
    python -m vision.wise --run runs/s_lpft --save 0.5      # grava runs/s_lpft_wise0.5/best.pt

A avaliacao usa so "val" e "test" da divisao da execucao; a parte "final" nunca e lida aqui.
"""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

import numpy as np
import torch

from vision.config import CLASS_TO_INDEX, SPLITS_DIR
from vision.data import load_split
from vision.evaluate import evaluate_frame, fit_temperature, predict_logits, softmax
from vision.model import load_checkpoint, normalization, save_checkpoint


def interpolate(run: Path, alpha: float, device: str) -> tuple[torch.nn.Module, dict]:
    model, meta = load_checkpoint(run / "best.pt", device)
    lp = torch.load(run / "lp.pt", map_location=device, weights_only=False)["state_dict"]
    tuned = model.state_dict()
    mixed = {k: (1 - alpha) * lp[k] + alpha * v if v.is_floating_point() else v for k, v in tuned.items()}
    model.load_state_dict(mixed)
    return model.eval(), {**meta, "wise_alpha": alpha, "wise_from": run.name}


def evaluate(model, meta: dict, split: Path, device: str, out_dir: Path | None = None) -> dict:
    frame = load_split(split)
    summary = {}
    for part in ("val", "test"):
        part_frame = frame[frame["split"] == part].reset_index(drop=True)
        if not len(part_frame):
            continue
        logits = predict_logits(model, part_frame, meta["img_size"], device, normalization(meta["arch"]))
        result = {"checkpoint": "best.pt", "split_file": split.name, "part": part,
                  "epoch": meta.get("epoch"), "wise_alpha": meta.get("wise_alpha"),
                  "datasets_in_part": sorted(part_frame["dataset"].unique().tolist()),
                  **evaluate_frame(part_frame, softmax(logits))}
        if part == "val":
            result["temperature"] = fit_temperature(logits, part_frame["label"].map(CLASS_TO_INDEX).to_numpy())
        if out_dir is not None:
            np.save(out_dir / f"logits_{part}.npy", logits)
            (out_dir / f"eval_{part}.json").write_text(json.dumps(result, indent=2, ensure_ascii=False),
                                                      encoding="utf-8")
        summary[part] = {"macro_f1": result["macro_f1"], "recall_cercosporiose": result["recall_cercosporiose"],
                         "precision_cercosporiose": result["per_class"]["cercosporiose"]["precision"],
                         "recall_saudavel": result["per_class"]["saudavel"]["recall"],
                         "ece": result["calibration"]["ece"]}
    return summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--run", type=Path, required=True)
    parser.add_argument("--alphas", type=float, nargs="*", default=[0.25, 0.5, 0.75])
    parser.add_argument("--save", type=float, help="grava a interpolacao com esse alfa como execucao")
    args = parser.parse_args()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    config = json.loads((args.run / "config.json").read_text(encoding="utf-8"))
    split = SPLITS_DIR / config["split_file"]
    if args.save is not None:
        out_dir = args.run.with_name(f"{args.run.name}_wise{args.save}")
        out_dir.mkdir(parents=True, exist_ok=True)
        model, meta = interpolate(args.run, args.save, device)
        save_checkpoint(out_dir / "best.pt", model, meta)
        shutil.copy(args.run / "config.json", out_dir / "config.json")
        summary = evaluate(model, meta, split, device, out_dir)
        (out_dir / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
        print(json.dumps(summary, indent=2))
        return
    results = {}
    for alpha in args.alphas:
        model, meta = interpolate(args.run, alpha, device)
        results[str(alpha)] = evaluate(model, meta, split, device)
        print(alpha, json.dumps(results[str(alpha)]), flush=True)
    (args.run / "wise.json").write_text(json.dumps(results, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
