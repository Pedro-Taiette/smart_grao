"""Maize Leaf Dataset (figshare 29298275, v1, Shaik 2025): CC BY 4.0, mas e um recorte do PlantVillage.

Os nomes ("<uuid>___RS_GLSp 1234.JPG"), as pastas ("Corn_(maize)___...") e o tamanho 256x256 sao os
do PlantVillage: fotos de laboratorio, folha destacada sobre fundo uniforme, apesar da descricao
falar em campo. O zip (35 MB) e baixado inteiro; da extracao saem so as quatro pastas de milho, com
os nomes originais. As pastas de pimentao ficam de fora.
"""

from __future__ import annotations

import shutil
import zipfile
from pathlib import Path

from vision.download.common import download_file, raw_dir, run_guarded

ZIP_URL = "https://ndownloader.figshare.com/files/55317404"
ZIP_SIZE = 35_189_395
KEEP_CLASSES = {
    "Corn_(maize)___Cercospora_leaf_spot Gray_leaf_spot",
    "Corn_(maize)___healthy",
    "Corn_(maize)___Common_rust_",
    "Corn_(maize)___Northern_Leaf_Blight",
}


def download() -> dict:
    root = raw_dir("figmaize")
    archive_path = download_file(ZIP_URL, root / "_zip" / "Maize_Dataset.zip", expected_size=ZIP_SIZE)
    archive = zipfile.ZipFile(archive_path)
    kept, skipped_class = 0, 0
    for info in archive.infolist():
        if info.is_dir():
            continue
        parts = Path(info.filename).parts
        if len(parts) < 3:
            continue
        cls, name = parts[1], parts[-1]
        if cls not in KEEP_CLASSES:
            skipped_class += 1
            continue
        target = root / cls / name
        if not (target.exists() and target.stat().st_size == info.file_size):
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(info) as src, open(target, "wb") as dst:
                shutil.copyfileobj(src, dst, length=1 << 20)
        kept += 1
    return {
        "source": "https://doi.org/10.6084/m9.figshare.29298275.v1",
        "images": kept,
        "skipped_other_classes": skipped_class,
    }


def main() -> None:
    run_guarded("figmaize", download)


if __name__ == "__main__":
    main()
