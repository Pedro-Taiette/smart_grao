"""Dataset do PyTorch e transformacoes. O aumento de dados existe so aqui, no treino, e e aplicado
na hora de carregar: nenhuma imagem aumentada e gravada em disco nem entra na divisao."""

from __future__ import annotations

from pathlib import Path

import numpy as np
import pandas as pd
import torch
from PIL import Image
from torch.utils.data import Dataset, WeightedRandomSampler
from torchvision import transforms

from vision.config import CLASS_TO_INDEX, DATA_DIR, IMAGENET_MEAN, IMAGENET_STD


def train_transform(size: int) -> transforms.Compose:
    return transforms.Compose([
        transforms.RandomResizedCrop(size, scale=(0.35, 1.0), ratio=(0.6, 1.66)),
        transforms.RandomHorizontalFlip(),
        transforms.RandomVerticalFlip(),
        transforms.RandomRotation(20),
        # Luz de campo varia muito; o jitter de cor ajuda a nao depender do fundo de laboratorio
        transforms.ColorJitter(brightness=0.3, contrast=0.3, saturation=0.25, hue=0.03),
        transforms.RandomApply([transforms.GaussianBlur(5, sigma=(0.1, 2.0))], p=0.2),
        transforms.ToTensor(),
        transforms.Normalize(IMAGENET_MEAN, IMAGENET_STD),
    ])


def eval_transform(size: int) -> transforms.Compose:
    # Redimensiona a imagem inteira para o quadrado, sem recorte central: a lesao pode estar na
    # borda da foto. E o mesmo pre-processamento que o servico de inferencia deve aplicar.
    return transforms.Compose([
        transforms.Resize((size, size), interpolation=transforms.InterpolationMode.BILINEAR),
        transforms.ToTensor(),
        transforms.Normalize(IMAGENET_MEAN, IMAGENET_STD),
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


def balanced_sampler(frame: pd.DataFrame, mode: str = "label_dataset_sqrt") -> WeightedRandomSampler:
    """Amostragem com reposicao.

    - "label": cada classe pesa o mesmo por epoca.
    - "label_dataset_sqrt": cada classe pesa o mesmo e, dentro da classe, cada conjunto pesa
      proporcional a raiz do seu tamanho. Evita que um conjunto grande (ex.: recortes do Digipathos,
      de laboratorio) domine a classe sem repetir demais um conjunto pequeno.
    """
    labels = frame["label"]
    n_classes = labels.nunique()
    if mode == "label":
        weights = 1.0 / (n_classes * labels.map(labels.value_counts()))
    elif mode == "label_dataset_sqrt":
        counts = frame.groupby(["label", "dataset"]).size()
        share = np.sqrt(counts) / np.sqrt(counts).groupby(level="label").transform("sum")
        keys = list(zip(frame["label"], frame["dataset"]))
        weights = pd.Series([share[k] / (n_classes * counts[k]) for k in keys], index=frame.index)
    else:
        raise ValueError(f"modo de balanceamento desconhecido: {mode}")
    return WeightedRandomSampler(torch.tensor(weights.to_numpy(), dtype=torch.double),
                                 num_samples=len(frame), replacement=True)


def load_split(path: Path) -> pd.DataFrame:
    return pd.read_csv(path, dtype=str, keep_default_na=False)
