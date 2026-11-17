"""Exporta um checkpoint para ONNX e confere com o onnxruntime que a saida bate com o PyTorch.

    python -m vision.export --run runs/b0_grouped --version 0.1.0 --split data/splits/grouped.csv

Grava em models/<versao>/:
- model.onnx    entrada "image" float32 [N, 3, H, W] ja normalizada; saida "logits" [N, 3]
- labels.json   versao, classes na ordem da saida, tamanho de entrada, normalizacao, pre-processamento
- metrics.json  metricas do teste (e da validacao) copiadas da execucao, com a divisao usada

Opcoes do 0.2.0 em diante:
- --temperature T   divide os logits por T dentro do grafo (o servico continua so aplicando softmax)
- --test-eval / --ood-eval   eval_*.json de outras execucoes para as chaves "test" e "out_of_domain"
  (o modelo de producao treina em tudo e nao tem teste proprio; o out_of_domain sai no formato do
  0.1.0, a partir do vision.evaluate --part final)
- --variants   grava tambem model.fp16.onnx e model.int8.onnx (quantizacao dinamica) e mede a
  concordancia com o fp32 nas imagens do teste
- tempo de CPU por imagem (1 e lote de 64) de cada arquivo
"""

from __future__ import annotations

import argparse
import json
import shutil
import time
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


class WithTemperature(torch.nn.Module):
    def __init__(self, model: torch.nn.Module, temperature: float) -> None:
        super().__init__()
        self.model = model
        self.temperature = temperature

    def forward(self, image: torch.Tensor) -> torch.Tensor:
        return self.model(image) / self.temperature


def export(run_dir: Path, version: str, split: Path | None, n_check: int = 64, temperature: float = 1.0,
           test_eval: Path | None = None, ood_eval: Path | None = None, variants: bool = False) -> Path:
    model, meta = load_checkpoint(run_dir / "best.pt", "cpu")
    size = meta["img_size"]
    mean, std = tuple(meta.get("mean", IMAGENET_MEAN)), tuple(meta.get("std", IMAGENET_STD))
    graph = WithTemperature(model, temperature).eval() if temperature != 1.0 else model
    out_dir = MODELS_DIR / version
    out_dir.mkdir(parents=True, exist_ok=True)
    onnx_path = out_dir / "model.onnx"

    dummy = torch.randn(1, 3, size, size)
    torch.onnx.export(graph, (dummy,), str(onnx_path), input_names=["image"], output_names=["logits"],
                      dynamic_axes={"image": {0: "batch"}, "logits": {0: "batch"}},
                      opset_version=OPSET, dynamo=False)
    onnx.checker.check_model(str(onnx_path))

    batch, source = check_batch(split, size, mean, std, n_check)
    check = verify(graph, onnx_path, batch, source)

    output = {"name": "logits", "apply": "softmax"}
    if temperature != 1.0:
        output["temperature"] = {"value": temperature, "applied_in_graph": True,
                                 "note": "os logits ja saem divididos pela temperatura; nao dividir de novo"}
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
        "output": output,
        "preprocessing": {
            "resize": [size, size],
            "resize_mode": "imagem inteira para o quadrado, sem recorte; PIL Image.resize BILINEAR (com antialias)",
            "exif_orientation": "aplicar a rotacao do EXIF antes (ImageOps.exif_transpose)",
            "scale": "pixel / 255",
            "mean": list(mean),
            "std": list(std),
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
    if test_eval is not None:
        metrics["test"] = json.loads(test_eval.read_text(encoding="utf-8"))
    if ood_eval is not None:
        metrics["out_of_domain"] = out_of_domain(ood_eval)
    files = {"model.onnx": onnx_path}
    if variants:
        files.update(write_variants(onnx_path))
    metrics["files"] = {name: {"size_mb": round(path.stat().st_size / 1e6, 1), **cpu_timing(path, size)}
                        for name, path in files.items()}
    if variants:
        for name, path in files.items():
            if name != "model.onnx":
                metrics["files"][name]["agreement_vs_fp32"] = agreement(onnx_path, path, batch, source)
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2, ensure_ascii=False), encoding="utf-8")
    shutil.copy(run_dir / "config.json", out_dir / "train_config.json")
    print(json.dumps({"onnx_check": check, "files": metrics["files"]}, indent=2))
    return onnx_path


def out_of_domain(eval_path: Path, holdout: str = "craze") -> dict:
    """Resultado de vision.evaluate no formato do out_of_domain do 0.1.0."""
    result = json.loads(eval_path.read_text(encoding="utf-8"))
    run_dir = Path(result["checkpoint"]).parent
    val_path = run_dir / "eval_val.json"
    return {
        "holdout": holdout,
        "run": run_dir.name,
        "split_file": result["split_file"],
        "part": result["part"],
        **{k: result[k] for k in ("n", "accuracy", "macro_f1", "recall_cercosporiose", "per_class",
                                  "confusion_matrix")},
        "calibration": {"ece": result["calibration"]["ece"]},
        "val_macro_f1": json.loads(val_path.read_text(encoding="utf-8"))["macro_f1"] if val_path.exists() else None,
        "tta": result.get("tta", "none"),
        "temperature": result.get("temperature", 1.0),
    }


def check_batch(split: Path | None, size: int, mean, std, n_check: int) -> tuple[np.ndarray, str]:
    if split is None:
        return np.random.default_rng(0).standard_normal((n_check, 3, size, size), dtype=np.float32), \
            f"{n_check} tensores aleatorios"
    frame = load_split(split)
    frame = frame[frame["split"] == "test"]
    if n_check:
        frame = frame.sample(n=min(n_check, len(frame)), random_state=0)
    dataset = LeafDataset(frame, eval_transform(size, mean, std))
    batch = torch.stack([dataset[i][0] for i in range(len(dataset))]).numpy()
    return batch, f"{len(batch)} imagens do teste ({split.name})"


def _softmax(logits: np.ndarray) -> np.ndarray:
    z = np.exp(logits - logits.max(axis=1, keepdims=True))
    return z / z.sum(axis=1, keepdims=True)


def _run(path: Path, batch: np.ndarray, chunk: int = 32) -> np.ndarray:
    session = ort.InferenceSession(str(path), providers=["CPUExecutionProvider"])
    return np.concatenate([session.run(["logits"], {"image": batch[i:i + chunk]})[0]
                           for i in range(0, len(batch), chunk)])


def verify(model: torch.nn.Module, onnx_path: Path, batch: np.ndarray, source: str) -> dict:
    """Compara PyTorch e onnxruntime em imagens reais do teste (ou ruido, sem divisao)."""
    with torch.no_grad():
        expected = torch.softmax(model(torch.from_numpy(batch)), dim=1).numpy()
    actual = _softmax(_run(onnx_path, batch))
    diff = float(np.abs(actual - expected).max())
    agree = float((actual.argmax(1) == expected.argmax(1)).mean())
    result = {"checked_on": source, "max_abs_prob_diff": diff, "argmax_agreement": agree}
    if diff > 1e-3 or agree < 1.0:
        raise AssertionError(f"ONNX diverge do PyTorch: {result}")
    return result


def write_variants(onnx_path: Path) -> dict[str, Path]:
    from onnxruntime.quantization import QuantType, quantize_dynamic
    from onnxruntime.transformers.float16 import convert_float_to_float16

    fp16_path = onnx_path.with_name("model.fp16.onnx")
    onnx.save(convert_float_to_float16(onnx.load(str(onnx_path)), keep_io_types=True), str(fp16_path))
    int8_path = onnx_path.with_name("model.int8.onnx")
    quantize_dynamic(str(onnx_path), str(int8_path), weight_type=QuantType.QInt8)
    return {"model.fp16.onnx": fp16_path, "model.int8.onnx": int8_path}


def agreement(reference: Path, candidate: Path, batch: np.ndarray, source: str) -> dict:
    expected = _softmax(_run(reference, batch))
    actual = _softmax(_run(candidate, batch))
    diff = np.abs(actual - expected)
    return {"checked_on": source, "argmax_agreement": round(float((actual.argmax(1) == expected.argmax(1)).mean()), 4),
            "mean_abs_prob_diff": round(float(diff.mean()), 5), "max_abs_prob_diff": round(float(diff.max()), 5)}


def cpu_timing(path: Path, size: int, repeats: int = 5) -> dict:
    """Mediana do tempo de CPU (onnxruntime, threads padrao) para 1 imagem e para um lote de 64 (o
    mapa de oclusao 8x8 do servico)."""
    session = ort.InferenceSession(str(path), providers=["CPUExecutionProvider"])
    result = {}
    for n in (1, 64):
        x = np.random.default_rng(0).standard_normal((n, 3, size, size), dtype=np.float32)
        session.run(["logits"], {"image": x})
        times = []
        for _ in range(repeats if n == 1 else 2):
            started = time.perf_counter()
            session.run(["logits"], {"image": x})
            times.append(time.perf_counter() - started)
        result[f"cpu_ms_batch{n}"] = round(float(np.median(times)) * 1000, 1)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--run", type=Path, required=True, help="pasta da execucao (com best.pt)")
    parser.add_argument("--version", required=True)
    parser.add_argument("--split", type=Path, help="divisao para conferir em imagens reais")
    parser.add_argument("--n-check", type=int, default=64, help="imagens do teste na conferencia (0 = todas)")
    parser.add_argument("--temperature", type=float, default=1.0)
    parser.add_argument("--test-eval", type=Path)
    parser.add_argument("--ood-eval", type=Path)
    parser.add_argument("--variants", action="store_true")
    args = parser.parse_args()
    export(args.run, args.version, args.split, args.n_check, args.temperature, args.test_eval,
           args.ood_eval, args.variants)


if __name__ == "__main__":
    main()
