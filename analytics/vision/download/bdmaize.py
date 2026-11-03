"""Multi-Format Maize Leaf Image Dataset (Mendeley sb4wwjs8bz, v1): Bangladesh, CC BY 4.0.

Apesar da descricao ("real field conditions"), sao folhas destacadas fotografadas sobre um banner
plastico claro: fundo uniforme, mais perto de laboratorio que de campo. Sem EXIF. Muitos arquivos de
RAW_Image sao a mesma foto salva de novo (diferenca media < 0,02 por pixel); o dHash do manifesto
os junta no mesmo grupo.

O conjunto publica as mesmas fotos em quatro pastas: RAW_Image (originais do celular), Annotation
(copias identicas das RAW com CSV de caixas), "Converted raw Images" (as RAW recomprimidas) e
Augmented_Images (reduzidas "org_*" e aumentadas "aug_*"). Baixa so RAW_Image, arquivo por arquivo:
- das classes que entram no projeto (Gray_Leaf_Spot, Healthy_Leaf, Common_Rust, Corn_Leaf_Blight,
  Eyespot); Bacterial_Leaf_Streak e Maize_Streak_Disease sao estrias, nao manchas;
- sem os .HEIC (o pipeline so le jpg/png) e sem copias exatas (mesmo sha256 com outro nome).
"""

from __future__ import annotations

from vision.download.common import download_many, fetch_json, raw_dir, run_guarded

API = "https://data.mendeley.com/public-api/datasets/sb4wwjs8bz"
KEEP_CLASSES = {"Gray_Leaf_Spot", "Healthy_Leaf", "Common_Rust", "Corn_Leaf_Blight", "Eyespot"}
SOURCE_FOLDER = "RAW_Image"


def _folder_paths(folders: list[dict]) -> dict[str, tuple[str, ...]]:
    by_id = {folder["id"]: folder for folder in folders}

    def walk(folder_id: str) -> tuple[str, ...]:
        parts = []
        while folder_id in by_id:
            parts.append(by_id[folder_id]["name"])
            folder_id = by_id[folder_id].get("parent_id")
        return tuple(reversed(parts))

    return {folder_id: walk(folder_id) for folder_id in by_id}


def download() -> dict:
    root = raw_dir("bdmaize")
    meta = fetch_json(API)
    paths = _folder_paths(fetch_json(f"{API}/folders/{meta['version']}"))
    jobs, seen, skipped_dup, skipped_heic, skipped_class = [], set(), [], [], 0
    for item in sorted(meta["files"], key=lambda f: paths.get(f["folder_id"], ()) + (f["filename"],)):
        parts = paths.get(item["folder_id"], ())
        if SOURCE_FOLDER not in parts:
            continue
        cls, name = parts[-1], item["filename"]
        if cls not in KEEP_CLASSES:
            skipped_class += 1
            continue
        if name.lower().endswith(".heic"):
            skipped_heic.append(f"{cls}/{name}")
            continue
        digest = item["content_details"]["sha256_hash"]
        if digest in seen:
            skipped_dup.append(f"{cls}/{name}")
            continue
        seen.add(digest)
        jobs.append((item["content_details"]["download_url"], root / cls / name, item["size"]))
    done, errors = download_many(jobs, label="bdmaize")
    return {
        "status": "ok" if not errors else "parcial",
        "source": "https://data.mendeley.com/datasets/sb4wwjs8bz/1",
        "images": done,
        "errors": errors[:10],
        "skipped_other_classes": skipped_class,
        "skipped_heic": skipped_heic,
        "skipped_exact_duplicates": skipped_dup,
    }


def main() -> None:
    run_guarded("bdmaize", download)


if __name__ == "__main__":
    main()
