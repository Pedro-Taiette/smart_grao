"""Criacao do modelo e leitura/gravacao de checkpoint."""

from __future__ import annotations

from pathlib import Path

import timm
import torch

from vision.config import CLASSES


def create_model(arch: str = "efficientnet_b0", pretrained: bool = True) -> torch.nn.Module:
    return timm.create_model(arch, pretrained=pretrained, num_classes=len(CLASSES))


def save_checkpoint(path: Path, model: torch.nn.Module, meta: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    torch.save({"state_dict": model.state_dict(), "meta": meta}, path)


def load_checkpoint(path: Path, device: str = "cpu") -> tuple[torch.nn.Module, dict]:
    payload = torch.load(path, map_location=device, weights_only=False)
    meta = payload["meta"]
    model = create_model(meta["arch"], pretrained=False)
    model.load_state_dict(payload["state_dict"])
    model.to(device).eval()
    return model, meta
