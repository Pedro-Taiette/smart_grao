"""Dataset do PyTorch e transformacoes. O aumento de dados existe so aqui, no treino, e e aplicado
na hora de carregar: nenhuma imagem aumentada e gravada em disco nem entra na divisao."""

from __future__ import annotations

import io
from pathlib import Path

import numpy as np
import pandas as pd
import torch
from PIL import Image
from torch.utils.data import Dataset, WeightedRandomSampler
from torchvision import transforms

from vision.config import CLASS_TO_INDEX, DATA_DIR, IMAGENET_MEAN, IMAGENET_STD


class RandomJpeg:
    def __init__(self, quality: tuple[int, int]) -> None:
        self.quality = quality

    def __call__(self, image: Image.Image) -> Image.Image:
        buffer = io.BytesIO()
        image.save(buffer, "JPEG", quality=int(np.random.randint(*self.quality)))
        buffer.seek(0)
        return Image.open(buffer).convert("RGB")


def train_transform(size: int, mean=IMAGENET_MEAN, std=IMAGENET_STD, strong: bool = False):
    if not strong:
        return transforms.Compose([
            transforms.RandomResizedCrop(size, scale=(0.35, 1.0), ratio=(0.6, 1.66)),
            transforms.RandomHorizontalFlip(),
            transforms.RandomVerticalFlip(),
            transforms.RandomRotation(20),
            # Luz de campo varia muito; o jitter de cor ajuda a nao depender do fundo de laboratorio
            transforms.ColorJitter(brightness=0.3, contrast=0.3, saturation=0.25, hue=0.03),
            transforms.RandomApply([transforms.GaussianBlur(5, sigma=(0.1, 2.0))], p=0.2),
            transforms.ToTensor(),
            transforms.Normalize(mean, std),
        ])
    # Forte: foto de celular em lavoura (sol, sombra, foco, compressao, enquadramento solto)
    return transforms.Compose([
        transforms.RandomResizedCrop(size, scale=(0.25, 1.0), ratio=(0.5, 2.0)),
        transforms.RandomHorizontalFlip(),
        transforms.RandomVerticalFlip(),
        transforms.RandomRotation(30),
        transforms.TrivialAugmentWide(),
        transforms.ColorJitter(brightness=0.4, contrast=0.4, saturation=0.35, hue=0.04),
        transforms.RandomGrayscale(p=0.05),
        transforms.RandomApply([transforms.GaussianBlur(7, sigma=(0.1, 3.0))], p=0.3),
        transforms.RandomApply([RandomJpeg((30, 90))], p=0.3),
        transforms.ToTensor(),
        transforms.Normalize(mean, std),
        transforms.RandomErasing(p=0.2, scale=(0.02, 0.15)),
    ])


def eval_transform(size: int, mean=IMAGENET_MEAN, std=IMAGENET_STD) -> transforms.Compose:
    # Redimensiona a imagem inteira para o quadrado, sem recorte central: a lesao pode estar na
    # borda da foto. E o mesmo pre-processamento que o servico de inferencia deve aplicar.
    return transforms.Compose([
        transforms.Resize((size, size), interpolation=transforms.InterpolationMode.BILINEAR),
        transforms.ToTensor(),
        transforms.Normalize(mean, std),
    ])


class LeafDataset(Dataset):
    def __init__(self, frame: pd.DataFrame, transform) -> None:
        self.frame = frame.reset_index(drop=True)
        self.transform = transform
        self.paths = [DATA_DIR / Path(p) for p in self.frame["processed_path"]]
        self.targets = [CLASS_TO_INDEX[c] for c in self.frame["label"]]

    def __len__(self) -> int:
        return len(self.frame)

    def __getitem__(self, index: int):
        with Image.open(self.paths[index]) as image:
            tensor = self.transform(image.convert("RGB"))
        return tensor, self.targets[index], index


def sample_weights(frame: pd.DataFrame, mode: str = "label_dataset_sqrt") -> np.ndarray:
    """Peso de cada linha para que a soma por classe seja igual.

    - "label": cada classe pesa o mesmo.
    - "label_dataset_sqrt": cada classe pesa o mesmo e, dentro da classe, cada conjunto pesa
      proporcional a raiz do seu tamanho. Evita que um conjunto grande (ex.: recortes do Digipathos,
      de laboratorio) domine a classe sem repetir demais um conjunto pequeno.
    """
    labels = frame["label"]
    n_classes = labels.nunique()
    if mode == "label":
        return (1.0 / (n_classes * labels.map(labels.value_counts()))).to_numpy()
    if mode == "label_dataset_sqrt":
        counts = frame.groupby(["label", "dataset"]).size()
        share = np.sqrt(counts) / np.sqrt(counts).groupby(level="label").transform("sum")
        keys = list(zip(frame["label"], frame["dataset"]))
        return np.array([share[k] / (n_classes * counts[k]) for k in keys])
    raise ValueError(f"modo de balanceamento desconhecido: {mode}")


def balanced_sampler(frame: pd.DataFrame, mode: str = "label_dataset_sqrt") -> WeightedRandomSampler:
    """Amostragem com reposicao pelos pesos de sample_weights."""
    return WeightedRandomSampler(torch.tensor(sample_weights(frame, mode), dtype=torch.double),
                                 num_samples=len(frame), replacement=True)


def load_split(path: Path) -> pd.DataFrame:
    return pd.read_csv(path, dtype=str, keep_default_na=False)
