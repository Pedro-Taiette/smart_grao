"""Leitura do Maize In-Field (Craze et al., 2022) para o manifesto.

O Database.csv tem uma linha por imagem e uma coluna 0/1 por rotulo (multirrotulo):
GLS, NCLB, PLS, CR, SR, NoFoliarSymptoms, Other, UnidentifiedDisease.

Regra das 3 classes, nesta ordem:
1. GLS presente -> cercosporiose, mesmo com outra doenca junto. E a classe que importa detectar: uma
   folha com cercosporiose e helmintosporiose ainda precisa disparar a suspeita.
2. NCLB, PLS (mancha-branca), CR (ferrugem comum) ou SR (polissora) -> outras_manchas.
3. So NoFoliarSymptoms -> saudavel.
4. O resto e excluido: "Other" (rasgo, inseto) e "UnidentifiedDisease" sem doenca conhecida, e
   NoFoliarSymptoms + Other, em que o "saudavel" fica duvidoso.

Grupo: as fotos CIMG trazem a planta no nome ("105_CIMG3840_1-140-1" e "..._1-140-2" sao a mesma
planta 140, lados 1 e 2). As demais (DSC, IMG, "maize_diverse", "Cedara") nao identificam a planta e
caem na sessao de captura (EXIF em janela de 5 minutos), definida no manifest.py.
"""

from __future__ import annotations

import re

import pandas as pd

from vision.config import DATA_DIR, RAW_DIR

ROOT = RAW_DIR / "craze" / "kaggle" / "Kaggle Dataset"
COLUMNS = ["GLS", "NCLB", "PLS", "CR", "SR", "NoFoliarSymptoms", "Other", "UnidentifiedDisease"]
OTHER_SPOTS = ["NCLB", "PLS", "CR", "SR"]
PLANT_RE = re.compile(r"^\d+[a-z]?_CIMG\d+_(\d+)-(\d+)-\d+$", re.IGNORECASE)


def label_of(row: pd.Series) -> tuple[str, str]:
    """Devolve (classe de 3 vias, motivo de exclusao)."""
    if row["GLS"] == 1:
        return "cercosporiose", ""
    if any(row[c] == 1 for c in OTHER_SPOTS):
        return "outras_manchas", ""
    if row["NoFoliarSymptoms"] == 1 and row["Other"] == 0 and row["UnidentifiedDisease"] == 0:
        return "saudavel", ""
    return "", "craze: sem rotulo que caiba nas 3 classes (Other/Unidentified)"


def scan() -> list[dict]:
    csv = ROOT / "Database.csv"
    if not csv.exists():
        return []
    table = pd.read_csv(csv)
    rows = []
    for _, row in table.iterrows():
        path = ROOT / "leaf_images" / row["filePath"]
        if not path.exists():
            continue
        label, excluded = label_of(row)
        original = "+".join(c for c in COLUMNS if row[c] == 1)
        match = PLANT_RE.match(path.stem)
        hint = f"craze:plant:{match.group(1)}-{match.group(2)}" if match else ""
        rows.append({
            "path": path.relative_to(DATA_DIR).as_posix(),
            "label": label or "excluida",
            "original_label": original,
            "dataset": "craze",
            "group_hint": hint,
            "excluded": excluded,
            "meta_multilabel": int(sum(row[c] for c in COLUMNS[:5]) > 1),
        })
    return rows
