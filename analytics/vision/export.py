"""Exporta um checkpoint para ONNX e confere com o onnxruntime que a saida bate com o PyTorch.

    python -m vision.export --run runs/b0_grouped --version 0.1.0

Grava em models/<versao>/:
- model.onnx    entrada "image" float32 [N, 3, H, W] ja normalizada; saida "logits" [N, 3]
- labels.json   versao, classes na ordem da saida, tamanho de entrada, normalizacao, pre-processamento
- metrics.json  metricas do teste (e da validacao) copiadas da execucao, com a divisao usada
"""

from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import onnx
import onnxruntime as ort
import torch

from vision.config import IMAGENET_MEAN, IMAGENET_STD, MODELS_DIR
from vision.data import LeafDataset, eval_transform, load_split
from vision.model import load_checkpoint

OPSET = 17


def export(run_dir: Path, version: str, split: Path | None, n_check: int = 64) -> Path:
    model, meta = load_checkpoint(run_dir / "best.pt", "cpu")
    size = meta["img_size"]
    out_dir = MODELS_DIR / version
    out_dir.mkdir(parents=True, exist_ok=True)
    onnx_path = out_dir / "model.onnx"

    dummy = torch.randn(1, 3, size, size)
    torch.onnx.export(model, (dummy,), str(onnx_path), input_names=["image"], output_names=["logits"],
                      dynamic_axes={"image": {0: "batch"}, "logits": {0: "batch"}},
                      opset_version=OPSET, dynamo=False)
    onnx.checker.check_model(str(onnx_path))

    check = verify(model, onnx_path, size, split, n_check)

    labels = {
        "version": version,
        "arch": meta["arch"],
        "classes": meta["classes"],
        "display_names": {
            "cercosporiose": "cercosporiose (Cercospora spp.)",
            "saudavel": "saudavel",
            "outras_manchas": "outras manchas",
        },
        "input": {"name": "image", "shape": [None, 3, size, size], "dtype": "float32",
                  "layout": "NCHW", "color": "RGB"},
        "output": {"name": "logits", "apply": "softmax"},
        "preprocessing": {
            "resize": [size, size],
            "resize_mode": "imagem inteira para o quadrado, sem recorte; PIL Image.resize BILINEAR (com antialias)",
            "exif_orientation": "aplicar a rotacao do EXIF antes (ImageOps.exif_transpose)",
            "scale": "pixel / 255",
            "mean": list(IMAGENET_MEAN),
            "std": list(IMAGENET_STD),
        },
        "opset": OPSET,
        "exported_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "source_run": run_dir.name,
        "source_epoch": meta.get("epoch"),
    }
    (out_dir / "labels.json").write_text(json.dumps(labels, indent=2, ensure_ascii=False), encoding="utf-8")

    metrics = {"version": version, "split_file": meta.get("split_file"), "onnx_check": check}
    for part in ("test", "val"):
        path = run_dir / f"eval_{part}.json"
        if path.exists():
            metrics[part] = json.loads(path.read_text(encoding="utf-8"))
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2, ensure_ascii=False), encoding="utf-8")
    shutil.copy(run_dir / "config.json", out_dir / "train_config.json")
    size_mb = onnx_path.stat().st_size / 1e6
    print(json.dumps({"onnx": str(onnx_path), "size_mb": round(size_mb, 2), **check}, indent=2))
    return onnx_path


def verify(model: torch.nn.Module, onnx_path: Path, size: int, split: Path | None, n_check: int) -> dict:
    """Compara PyTorch e onnxruntime em imagens reais do teste (ou ruido, sem divisao)."""
    if split is not None:
        frame = load_split(split)
        frame = frame[frame["split"] == "test"].sample(n=min(n_check, (frame["split"] == "test").sum()),
                                                      random_state=0)
        dataset = LeafDataset(frame, eval_transform(size))
        batch = torch.stack([dataset[i][0] for i in range(len(dataset))])
        source = f"{len(batch)} imagens do teste"
    else:
        batch = torch.randn(n_check, 3, size, size)
        source = f"{n_check} tensores aleatorios"
    with torch.no_grad():
        expected = torch.softmax(model(batch), dim=1).numpy()
    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    logits = session.run(["logits"], {"image": batch.numpy()})[0]
    actual = np.exp(logits - logits.max(axis=1, keepdims=True))
    actual /= actual.sum(axis=1, keepdims=True)
    diff = float(np.abs(actual - expected).max())
    agree = float((actual.argmax(1) == expected.argmax(1)).mean())
    result = {"checked_on": source, "max_abs_prob_diff": diff, "argmax_agreement": agree}
    if diff > 1e-3 or agree < 1.0:
        raise AssertionError(f"ONNX diverge do PyTorch: {result}")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--run", type=Path, required=True, help="pasta da execucao (com best.pt)")
    parser.add_argument("--version", required=True)
    parser.add_argument("--split", type=Path, help="divisao para conferir em imagens reais")
    args = parser.parse_args()
    export(args.run, args.version, args.split)


if __name__ == "__main__":
    main()
