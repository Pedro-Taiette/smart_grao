"""Digipathos (Embrapa; Redape, doi:10.48432/XA1OVL): so milho, e so para "outras_manchas".

Laboratorio, folha sobre placa com color checker, Brasil, CC BY-NC 4.0. Nao tem cercosporiose de
milho nem folha saudavel. Baixa o zip das imagens originais ("- 1.zip") e o dos recortes
("- Cropped.zip"); o recorte e agrupado com a original de onde saiu (ver manifest.py).
"""

from __future__ import annotations

import zipfile

from vision.download.common import download_file, fetch_json, raw_dir, run_guarded

SERVER = "https://www.redape.dados.embrapa.br"
DOI = "doi:10.48432/XA1OVL"

# Doencas de milho do Digipathos que entram em "outras_manchas". As demais (antracnose, enfezamento,
# lixa, diplodia, physoderma) ficam de fora por nao estarem no recorte definido na pesquisa.
KEEP_DISEASES = {
    "Mancha_Turcicum": "helmintosporiose",
    "Mancha_Branca": "mancha_branca",
    "Ferrugem_Polissora": "polissora",
    "Mancha_Bipolaris": "bipolaris",
    "Ferrugem_Branca": "ferrugem_branca",
}


def _disease_of(filename: str) -> str | None:
    # Ex.: "Milho (Corn) - Mancha_Turcicum (Northern Leaf Blight) - 1.zip"
    if not filename.startswith("Milho (Corn) - "):
        return None
    token = filename.split(" - ")[1].split(" (")[0].strip()
    return token if token in KEEP_DISEASES else None


def download() -> dict:
    root = raw_dir("digipathos")
    meta = fetch_json(f"{SERVER}/api/datasets/:persistentId/?persistentId={DOI}")
    files = meta["data"]["latestVersion"]["files"]
    extracted = {}
    for entry in files:
        data_file = entry["dataFile"]
        name = data_file["filename"]
        disease = _disease_of(name)
        if disease is None or not name.endswith(".zip"):
            continue
        variant = "cropped" if "Cropped" in name else "original"
        archive_path = root / "_zips" / name
        download_file(f"{SERVER}/api/access/datafile/{data_file['id']}", archive_path,
                      expected_size=data_file.get("filesize"))
        out_dir = root / disease / variant
        out_dir.mkdir(parents=True, exist_ok=True)
        count = 0
        with zipfile.ZipFile(archive_path) as archive:
            for info in archive.infolist():
                if info.is_dir():
                    continue
                # Achata a estrutura interna do zip, mantendo o nome do arquivo
                target = out_dir / info.filename.replace("\\", "/").split("/")[-1]
                if not target.exists():
                    target.write_bytes(archive.read(info))
                count += 1
        extracted[f"{disease}/{variant}"] = count
        print(f"[digipathos] {disease}/{variant}: {count} arquivos", flush=True)
    return {"source": f"{SERVER}/dataset.xhtml?persistentId={DOI}", "extracted": extracted}


def main() -> None:
    run_guarded("digipathos", download)


if __name__ == "__main__":
    main()
