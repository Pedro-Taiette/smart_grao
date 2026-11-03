"""Seasonal Corn Leaf Disease (Mendeley vy629dngm8, v1): campo, Bangladesh, CC BY 4.0.

O zip publicado tem 4,6 GB e e baixado inteiro (ler so os membros por HTTP Range ficou lento demais
no S3 do Mendeley, ~0,4 MB/s). Da extracao saem so:
- as classes que entram no projeto (Gray_leaf_spot, Healthy, Common_rust);
- as imagens originais. Saem as geradas pelo Augmentor ("*_original_<arquivo>_<uuid>") e as
  copias reduzidas ("resized_*", "rsz_*"), que repetiriam uma original e vazariam na divisao.
Bacterial Leaf Streak e Maize Chlorotic Mottle Virus nao fazem parte das tres classes.
"""

from __future__ import annotations

import re
import shutil
import zipfile
from pathlib import Path

from vision.download.common import download_file, raw_dir, run_guarded

ZIP_URL = ("https://data.mendeley.com/public-files/datasets/vy629dngm8/files/"
           "e086f779-470a-4c7c-ba81-97734e9f8dd6/file_downloaded")
ZIP_SIZE = 4_599_036_050
KEEP_CLASSES = {"Gray_leaf_spot", "Healthy", "Common_rust"}
UUID_RE = re.compile(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", re.I)


def is_derived(filename: str) -> bool:
    """Arquivo derivado de outro (aumentado ou reduzido), que nao deve entrar no manifesto."""
    lower = filename.lower()
    return ("_original_" in lower or UUID_RE.search(lower) is not None
            or lower.startswith(("resized_", "rsz_", "recheck")))


def download() -> dict:
    root = raw_dir("seasonal")
    archive_path = download_file(ZIP_URL, root / "_zip" / "corn_leaf_disease_dataset.zip",
                                 expected_size=ZIP_SIZE)
    archive = zipfile.ZipFile(archive_path)
    kept, skipped_derived, skipped_class = 0, [], 0
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
        if is_derived(name):
            skipped_derived.append(f"{cls}/{name}")
            continue
        target = root / cls / name
        if target.exists() and target.stat().st_size == info.file_size:
            kept += 1
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        with archive.open(info) as src, open(target, "wb") as dst:
            shutil.copyfileobj(src, dst, length=1 << 20)
        kept += 1
        if kept % 100 == 0:
            print(f"[seasonal] {kept} imagens extraidas", flush=True)
    return {
        "source": "https://data.mendeley.com/datasets/vy629dngm8/1",
        "images": kept,
        "skipped_other_classes": skipped_class,
        "skipped_derived": len(skipped_derived),
        "skipped_derived_examples": skipped_derived[:10],
    }


def main() -> None:
    run_guarded("seasonal", download)


if __name__ == "__main__":
    main()
