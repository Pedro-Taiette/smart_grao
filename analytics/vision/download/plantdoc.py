"""PlantDoc (github.com/pratikkayal/PlantDoc-Dataset, Singh et al. 2020): imagens da web, CC BY 4.0.

Fotos coletadas por busca na internet e rotuladas pelos autores: fundo variado (muitas em campo), mas
com rotulo pouco confiavel. Ha figuras com legenda, colagens e slides; "Corn leaf blight" traz 16
arquivos com "gray leaf spot" no nome e 3 imagens identicas a outras da pasta de GLS. Baixa so as tres pastas de milho de train/ e
test/, arquivo por arquivo pela arvore do repositorio, mantendo split/classe/arquivo (com
caracteres invalidos no Windows trocados por "_").
"""

from __future__ import annotations

import re
import urllib.parse

from vision.download.common import download_many, fetch_json, raw_dir, run_guarded

REPO = "pratikkayal/PlantDoc-Dataset"
BRANCH = "master"
KEEP_CLASSES = {"Corn Gray leaf spot", "Corn leaf blight", "Corn rust leaf"}
# Varios nomes sao URLs ("show_picture.asp?id=...jpg"); o Windows nao aceita ?:*"<>| no nome
UNSAFE = re.compile(r'[<>:"|?*]')


def download() -> dict:
    root = raw_dir("plantdoc")
    tree = fetch_json(f"https://api.github.com/repos/{REPO}/git/trees/{BRANCH}?recursive=1")
    if tree.get("truncated"):
        raise RuntimeError("arvore do repositorio veio truncada")
    jobs = []
    for item in tree["tree"]:
        parts = item["path"].split("/")
        if item["type"] != "blob" or len(parts) != 3 or parts[1] not in KEEP_CLASSES:
            continue
        url = f"https://raw.githubusercontent.com/{REPO}/{BRANCH}/{urllib.parse.quote(item['path'])}"
        jobs.append((url, root / parts[0] / parts[1] / UNSAFE.sub("_", parts[2]), item["size"]))
    done, errors = download_many(jobs, label="plantdoc")
    return {
        "status": "ok" if not errors else "parcial",
        "source": f"https://github.com/{REPO}",
        "images": done,
        "errors": errors[:10],
    }


def main() -> None:
    run_guarded("plantdoc", download)


if __name__ == "__main__":
    main()
