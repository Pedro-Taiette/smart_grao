"""Classificacao da foto da folha com o modelo ONNX da trilha B, e o mapa de oclusao.

O mapa nao e Grad-CAM: o ONNX nao tem gradiente. Cada quadrado da imagem e coberto pela cor media e
a queda na probabilidade da classe escolhida diz o quanto o modelo dependia dele.
"""

from __future__ import annotations

import base64
import io
import json
import os
from functools import lru_cache
from pathlib import Path

import numpy as np
import onnxruntime as ort
from PIL import Image, ImageOps

MODELS_DIR = Path(os.environ.get("SMARTGRAO_MODELS_DIR", Path(__file__).resolve().parents[2] / "models"))
GRID = 8


def _latest_dir() -> Path:
    versions = sorted((p for p in MODELS_DIR.iterdir() if (p / "model.onnx").exists()),
                      key=lambda p: tuple(int(x) for x in p.name.split(".") if x.isdigit()))
    if not versions:
        raise FileNotFoundError(f"nenhum modelo em {MODELS_DIR}")
    return versions[-1]


def _model_dir() -> Path:
    return Path(os.environ["SMARTGRAO_MODEL_DIR"]) if "SMARTGRAO_MODEL_DIR" in os.environ else _latest_dir()


def _summary(part: dict) -> dict:
    cercosporiose = part.get("per_class", {}).get("cercosporiose", {})
    return {
        "n": part.get("n"), "accuracy": part.get("accuracy"), "macro_f1": part.get("macro_f1"),
        "recall_cercosporiose": part.get("recall_cercosporiose", cercosporiose.get("recall")),
        "precision_cercosporiose": cercosporiose.get("precision"),
    }


def model_card() -> dict:
    directory = _model_dir()
    labels = json.loads((directory / "labels.json").read_text(encoding="utf-8"))
    metrics = json.loads((directory / "metrics.json").read_text(encoding="utf-8"))
    test = metrics["test"]
    outside = metrics.get("out_of_domain")
    return {
        "model_version": f"{labels.get('arch', 'modelo')}-{labels['version']}",
        "split": metrics.get("split_file"),
        "test": _summary(test) | {"ece": test.get("calibration", {}).get("ece")},
        "confusion_labels": test["confusion_matrix"]["labels"],
        "confusion_matrix": test["confusion_matrix"]["rows_true_cols_pred"],
        "by_domain": [{"name": name} | _summary(part) for name, part in test.get("by_domain", {}).items()],
        "by_dataset": [{"name": name} | _summary(part) for name, part in test.get("by_dataset", {}).items()],
        "out_of_domain": None if outside is None else {
            "holdout": outside["holdout"],
            "test": {"name": outside["holdout"]} | _summary(outside) | {"ece": outside.get("calibration", {}).get("ece")},
            "validation_macro_f1": outside.get("val_macro_f1"),
        },
    }


@lru_cache(maxsize=1)
def load():
    directory = _model_dir()
    labels = json.loads((directory / "labels.json").read_text(encoding="utf-8"))
    session = ort.InferenceSession(str(directory / "model.onnx"), providers=["CPUExecutionProvider"])
    return session, labels


def _prepare(image: Image.Image, labels: dict) -> np.ndarray:
    size = tuple(labels["preprocessing"]["resize"])
    array = np.asarray(image.convert("RGB").resize(size, Image.BILINEAR), dtype=np.float32) / 255.0
    mean = np.array(labels["preprocessing"]["mean"], dtype=np.float32)
    std = np.array(labels["preprocessing"]["std"], dtype=np.float32)
    return ((array - mean) / std).transpose(2, 0, 1)


def _softmax(logits: np.ndarray) -> np.ndarray:
    shifted = np.exp(logits - logits.max(axis=-1, keepdims=True))
    return shifted / shifted.sum(axis=-1, keepdims=True)


def _occlusion(session, name: str, tensor: np.ndarray, target: int, base: float) -> np.ndarray:
    _, height, width = tensor.shape
    step_y, step_x = height // GRID, width // GRID
    batch = np.repeat(tensor[None], GRID * GRID, axis=0)
    for index in range(GRID * GRID):
        row, col = divmod(index, GRID)
        batch[index, :, row * step_y:(row + 1) * step_y, col * step_x:(col + 1) * step_x] = 0.0
    probabilities = _softmax(session.run(None, {name: batch})[0])[:, target]
    return np.clip(base - probabilities, 0, None).reshape(GRID, GRID)


def _overlay(image: Image.Image, heat: np.ndarray, size: int = 448) -> bytes:
    picture = image.convert("RGB").resize((size, size), Image.BILINEAR)
    peak = heat.max()
    normalized = heat / peak if peak > 0 else heat
    mask = Image.fromarray(np.uint8(normalized * 255)).resize((size, size), Image.BILINEAR)
    alpha = np.asarray(mask, dtype=np.float32)[..., None] / 255.0 * 0.6
    red = np.zeros((size, size, 3), dtype=np.float32)
    red[..., 0] = 255
    blended = np.asarray(picture, dtype=np.float32) * (1 - alpha) + red * alpha
    buffer = io.BytesIO()
    Image.fromarray(np.uint8(blended)).save(buffer, format="PNG")
    return buffer.getvalue()


def classify(content: bytes, with_heatmap: bool = True) -> dict:
    session, labels = load()
    image = ImageOps.exif_transpose(Image.open(io.BytesIO(content)))
    tensor = _prepare(image, labels)
    name = session.get_inputs()[0].name
    probabilities = _softmax(session.run(None, {name: tensor[None]})[0])[0]
    classes = labels["classes"]
    best = int(np.argmax(probabilities))
    result = {
        "model_version": f"{labels.get('arch', 'modelo')}-{labels['version']}",
        "label": classes[best],
        "probabilities": {cls: float(p) for cls, p in zip(classes, probabilities)},
        "heatmap_png": None,
    }
    if with_heatmap:
        heat = _occlusion(session, name, tensor, best, float(probabilities[best]))
        result["heatmap_png"] = base64.b64encode(_overlay(image, heat)).decode("ascii")
    return result
