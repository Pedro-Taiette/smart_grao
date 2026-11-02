"""Metricas de avaliacao. Recall de cercosporiose e F1 macro sao as metricas principais: em deteccao
precoce o falso negativo e o erro caro, e a acuracia esconde o desbalanceamento entre classes."""

from __future__ import annotations

import numpy as np

from vision.config import CLASSES


def confusion_matrix(y_true: np.ndarray, y_pred: np.ndarray, n_classes: int = len(CLASSES)) -> np.ndarray:
    matrix = np.zeros((n_classes, n_classes), dtype=np.int64)
    np.add.at(matrix, (y_true, y_pred), 1)
    return matrix


def per_class_report(matrix: np.ndarray) -> dict:
    report = {}
    for index, name in enumerate(CLASSES):
        tp = matrix[index, index]
        support = matrix[index].sum()
        predicted = matrix[:, index].sum()
        precision = tp / predicted if predicted else None
        recall = tp / support if support else None
        f1 = (2 * precision * recall / (precision + recall)
              if precision and recall else (0.0 if support else None))
        report[name] = {
            "precision": _round(precision),
            "recall": _round(recall),
            "f1": _round(f1),
            "support": int(support),
        }
    return report


def macro_f1(matrix: np.ndarray) -> float:
    """Media do F1 das classes presentes no conjunto (classe sem exemplo nao entra na media)."""
    values = [v["f1"] for v in per_class_report(matrix).values() if v["support"] > 0]
    return float(np.mean(values)) if values else 0.0


def expected_calibration_error(probs: np.ndarray, y_true: np.ndarray, n_bins: int = 15) -> dict:
    """ECE pela confianca da classe prevista, em faixas de largura igual."""
    confidence = probs.max(axis=1)
    correct = (probs.argmax(axis=1) == y_true).astype(np.float64)
    edges = np.linspace(0.0, 1.0, n_bins + 1)
    ece = 0.0
    bins = []
    for low, high in zip(edges[:-1], edges[1:]):
        mask = (confidence > low) & (confidence <= high)
        count = int(mask.sum())
        if count == 0:
            continue
        accuracy = float(correct[mask].mean())
        mean_conf = float(confidence[mask].mean())
        ece += count / len(y_true) * abs(accuracy - mean_conf)
        bins.append({"range": [round(float(low), 3), round(float(high), 3)], "count": count,
                     "accuracy": round(accuracy, 4), "confidence": round(mean_conf, 4)})
    return {"ece": round(float(ece), 4), "n_bins": n_bins, "bins": bins}


def summarize(y_true: np.ndarray, probs: np.ndarray) -> dict:
    y_pred = probs.argmax(axis=1)
    matrix = confusion_matrix(y_true, y_pred)
    report = per_class_report(matrix)
    return {
        "n": int(len(y_true)),
        "accuracy": _round(float((y_pred == y_true).mean()) if len(y_true) else None),
        "macro_f1": _round(macro_f1(matrix)),
        "recall_cercosporiose": report["cercosporiose"]["recall"],
        "per_class": report,
        "confusion_matrix": {"labels": list(CLASSES), "rows_true_cols_pred": matrix.tolist()},
        "calibration": expected_calibration_error(probs, y_true) if len(y_true) else None,
    }


def _round(value):
    return None if value is None else round(float(value), 4)
