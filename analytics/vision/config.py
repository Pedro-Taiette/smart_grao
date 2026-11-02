"""Caminhos e constantes compartilhados por todo o pacote."""

from __future__ import annotations

from pathlib import Path

ANALYTICS_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = ANALYTICS_DIR / "data"
RAW_DIR = DATA_DIR / "raw"
PROCESSED_DIR = DATA_DIR / "processed"
SPLITS_DIR = DATA_DIR / "splits"
MANIFEST_PATH = DATA_DIR / "manifest.csv"
DOWNLOAD_LOG_PATH = DATA_DIR / "download_log.json"
RUNS_DIR = ANALYTICS_DIR / "runs"
MODELS_DIR = ANALYTICS_DIR / "models"

# A ordem e o contrato com o servico de inferencia: o indice da saida do modelo e a posicao aqui
CLASSES: tuple[str, ...] = ("cercosporiose", "saudavel", "outras_manchas")
CLASS_TO_INDEX = {name: index for index, name in enumerate(CLASSES)}

# Nomes curtos dos conjuntos; sao tambem as pastas em data/raw
DATASETS: tuple[str, ...] = ("plantvillage", "cds", "seasonal", "craze", "digipathos")

# Dominio de captura de cada conjunto. Acuracia em "lab" nao e acuracia em campo.
DATASET_DOMAIN = {
    "plantvillage": "lab",
    "cds": "campo",
    "seasonal": "campo",
    "craze": "campo",
    "digipathos": "lab",
}

IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp"}

# Normalizacao do ImageNet, a mesma do pre-treino do EfficientNet do timm
IMAGENET_MEAN = (0.485, 0.456, 0.406)
IMAGENET_STD = (0.229, 0.224, 0.225)

# Lado maior das copias reduzidas em data/processed. Folga para o recorte aleatorio de 224 px
PROCESSED_MAX_SIDE = 512
