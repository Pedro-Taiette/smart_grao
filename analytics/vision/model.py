"""Criacao do modelo e leitura/gravacao de checkpoint.

`arch` e um nome do timm ou uma chave de BACKBONES. Os do BioCLIP vem no formato do open_clip; so a
torre de imagem e carregada num ViT CLIP do timm, que converte as chaves (sem depender do open_clip).
"""

from __future__ import annotations

from pathlib import Path

import timm
import torch
from huggingface_hub import hf_hub_download

from vision.config import CLASSES, IMAGENET_MEAN, IMAGENET_STD

CLIP_MEAN = (0.48145466, 0.4578275, 0.40821073)
CLIP_STD = (0.26862954, 0.26130258, 0.27577711)

BACKBONES = {
    "efficientnet_b0": {"timm": "efficientnet_b0"},
    "dinov2_s": {"timm": "vit_small_patch14_dinov2.lvd142m", "vit": True},
    "dinov2_b_reg4": {"timm": "vit_base_patch14_reg4_dinov2.lvd142m", "vit": True},
    "plantclef_b": {"timm": "hf_hub:PlantNet/vit_base_patch14_reg4_dinov2_lvd142m.pc24_all", "vit": True},
    "bioclip": {"timm": "vit_base_patch16_clip_224", "vit": True, "clip": True,
                "hf": ("imageomics/bioclip", "open_clip_pytorch_model.bin")},
    "bioclip2": {"timm": "vit_large_patch14_clip_224", "vit": True, "clip": True,
                 "hf": ("imageomics/bioclip-2", "open_clip_model.safetensors")},
}


def normalization(arch: str) -> tuple[tuple[float, ...], tuple[float, ...]]:
    if BACKBONES.get(arch, {}).get("clip"):
        return CLIP_MEAN, CLIP_STD
    return IMAGENET_MEAN, IMAGENET_STD


def create_model(arch: str = "efficientnet_b0", pretrained: bool = True, num_classes: int = len(CLASSES),
                 img_size: int = 224, **kwargs) -> torch.nn.Module:
    spec = BACKBONES.get(arch, {"timm": arch})
    kwargs["num_classes"] = num_classes
    if spec.get("vit"):
        kwargs["img_size"] = img_size
    if spec.get("hf") and pretrained:
        kwargs["pretrained_cfg_overlay"] = {"file": hf_hub_download(*spec["hf"])}
    return timm.create_model(spec["timm"], pretrained=pretrained, **kwargs)


def save_checkpoint(path: Path, model: torch.nn.Module, meta: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    torch.save({"state_dict": model.state_dict(), "meta": meta}, path)


def load_checkpoint(path: Path, device: str = "cpu") -> tuple[torch.nn.Module, dict]:
    payload = torch.load(path, map_location=device, weights_only=False)
    meta = payload["meta"]
    model = create_model(meta["arch"], pretrained=False, img_size=meta.get("img_size", 224))
    model.load_state_dict(payload["state_dict"])
    model.to(device).eval()
    return model, meta
