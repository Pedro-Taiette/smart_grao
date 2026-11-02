"""Grad-CAM: mapa de calor de onde o modelo olhou para chegar a uma classe.

    python -m vision.gradcam --checkpoint runs/<run>/best.pt --image foto.jpg --out mapa.png

Usa a ultima camada convolucional (conv_head no EfficientNet do timm). O mapa explica o modelo,
nao delimita a lesao: nao serve como medida de severidade.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
import torch
from PIL import Image, ImageOps

from vision.config import CLASSES
from vision.data import eval_transform
from vision.model import load_checkpoint


def _target_layer(model: torch.nn.Module) -> torch.nn.Module:
    if hasattr(model, "conv_head"):
        return model.conv_head
    convs = [m for m in model.modules() if isinstance(m, torch.nn.Conv2d)]
    return convs[-1]


def gradcam(model: torch.nn.Module, image: torch.Tensor, class_index: int | None = None
            ) -> tuple[np.ndarray, int, np.ndarray]:
    """image: tensor [3, H, W] normalizado. Devolve (mapa HxW em [0, 1], classe, probabilidades)."""
    activations: list[torch.Tensor] = []
    gradients: list[torch.Tensor] = []
    layer = _target_layer(model)
    forward_hook = layer.register_forward_hook(lambda _m, _i, out: activations.append(out))
    backward_hook = layer.register_full_backward_hook(lambda _m, _gi, go: gradients.append(go[0]))
    try:
        model.eval()
        device = next(model.parameters()).device
        batch = image.unsqueeze(0).to(device).requires_grad_(True)
        logits = model(batch)
        probs = torch.softmax(logits, dim=1)[0].detach().cpu().numpy()
        target = int(probs.argmax()) if class_index is None else class_index
        model.zero_grad(set_to_none=True)
        logits[0, target].backward()
        weights = gradients[0].mean(dim=(2, 3), keepdim=True)
        cam = torch.relu((weights * activations[0]).sum(dim=1, keepdim=True))
        cam = torch.nn.functional.interpolate(cam, size=image.shape[1:], mode="bilinear",
                                              align_corners=False)[0, 0]
        cam = cam - cam.min()
        cam = cam / cam.max() if cam.max() > 0 else cam
        return cam.detach().cpu().numpy(), target, probs
    finally:
        forward_hook.remove()
        backward_hook.remove()


def overlay(image: Image.Image, cam: np.ndarray, alpha: float = 0.45) -> Image.Image:
    import matplotlib

    heat = matplotlib.colormaps["jet"](cam)[..., :3]
    heat_image = Image.fromarray((heat * 255).astype(np.uint8)).resize(image.size, Image.BILINEAR)
    return Image.blend(image.convert("RGB"), heat_image, alpha)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--checkpoint", type=Path, required=True)
    parser.add_argument("--image", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--class-name", choices=CLASSES, help="classe explicada (padrao: a prevista)")
    args = parser.parse_args()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model, meta = load_checkpoint(args.checkpoint, device)
    with Image.open(args.image) as source:
        image = ImageOps.exif_transpose(source).convert("RGB")
    tensor = eval_transform(meta["img_size"])(image)
    index = CLASSES.index(args.class_name) if args.class_name else None
    cam, target, probs = gradcam(model, tensor, index)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    overlay(image, cam).save(args.out)
    print(f"classe explicada: {CLASSES[target]} | probabilidades: "
          + ", ".join(f"{c}={p:.3f}" for c, p in zip(CLASSES, probs)))
    print(f"gravado em {args.out}")


if __name__ == "__main__":
    main()
