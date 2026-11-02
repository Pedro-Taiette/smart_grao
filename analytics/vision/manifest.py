"""Manifesto unico das imagens: data/manifest.csv.

    python -m vision.manifest

Uma linha por imagem encontrada em data/raw, com:
- path            caminho relativo a data/ (imagem original, como baixada)
- processed_path  copia reduzida (lado maior 512 px) usada no treino; relativo a data/
- label           classe de 3 vias: cercosporiose | saudavel | outras_manchas
- original_label  rotulo do conjunto de origem
- dataset         conjunto de origem; domain: campo | lab
- group           unidade de divisao: tudo que tem o mesmo grupo cai no mesmo lado
- excluded        motivo da exclusao (vazio = entra). As excluidas ficam no arquivo para auditoria.
- md5, dhash, width, height, taken_at, camera

Regras de grupo por conjunto (o melhor metadado disponivel, nenhum perfeito):
- plantvillage: leaf-map.json do repositorio nao cobre o milho; grupo = prefixo do arquivo + faixa
  de 50 numeros sequenciais. Medido nas culturas que tem leaf-map, ~11% das folhas cruzam uma faixa.
- cds, seasonal: sessao de captura = data/hora do EXIF em janela de 5 minutos (e camera, quando ha).
  Sem EXIF: data/hora do nome IMG_AAAAMMDD_HHMMSS; sem isso, faixa de 20 numeros do nome.
- craze: planta pelo nome do arquivo nas fotos CIMG; as demais, sessao de captura (vision/craze.py).
- digipathos: os recortes nao dizem de qual foto original sairam, entao o grupo e a doenca inteira e
  o conjunto e so-treino (split.py).

Depois disso, duas protecoes contra vazamento:
- duplicata exata (md5): fica a primeira; rotulos conflitantes excluem todas as copias;
- quase duplicata (dHash de 256 bits, distancia <= 10): no mesmo conjunto, os grupos sao unidos; entre
  conjuntos diferentes, a copia do conjunto de menor prioridade e excluida.
"""

from __future__ import annotations

import hashlib
import json
import re
from concurrent.futures import ProcessPoolExecutor
from datetime import datetime
from pathlib import Path

import numpy as np
import pandas as pd
from PIL import Image, ImageOps

from vision.config import (DATA_DIR, DATASET_DOMAIN, IMAGE_EXTENSIONS, MANIFEST_PATH, PROCESSED_DIR,
                           PROCESSED_MAX_SIDE, RAW_DIR)

# Prioridade ao resolver quase duplicatas entre conjuntos: fica a copia do conjunto mais acima
DATASET_PRIORITY = ["craze", "cds", "seasonal", "plantvillage", "digipathos"]
DHASH_SIZE = 16
DHASH_MAX_DISTANCE = 10  # de 256 bits (~4%)
SESSION_MINUTES = 5
DIGIPATHOS_CROPS_PER_DISEASE = 300
CACHE_PATH = PROCESSED_DIR / "_image_cache.csv"


# ---------------------------------------------------------------------------------------------
# Varredura por conjunto: devolve linhas com path, label, original_label, dataset e pistas de grupo
# ---------------------------------------------------------------------------------------------

def _images(folder: Path) -> list[Path]:
    if not folder.exists():
        return []
    return sorted(p for p in folder.rglob("*") if p.is_file() and p.suffix.lower() in IMAGE_EXTENSIONS)


def _row(path: Path, label: str, original: str, dataset: str, **extra) -> dict:
    return {"path": path.relative_to(DATA_DIR).as_posix(), "label": label, "original_label": original,
            "dataset": dataset, "group_hint": "", "excluded": "", **extra}


PLANTVILLAGE_LABELS = {
    "Corn_(maize)___Cercospora_leaf_spot Gray_leaf_spot": "cercosporiose",
    "Corn_(maize)___healthy": "saudavel",
    "Corn_(maize)___Common_rust_": "outras_manchas",
    "Corn_(maize)___Northern_Leaf_Blight": "outras_manchas",
}


def _scan_plantvillage() -> list[dict]:
    rows = []
    root = RAW_DIR / "plantvillage" / "repo" / "raw" / "color"
    for folder, label in PLANTVILLAGE_LABELS.items():
        for path in _images(root / folder):
            stem = path.stem.split("___")[-1]
            match = re.match(r"^(.*?)[\s_]*(\d+)", stem)
            prefix, number = (match.group(1), int(match.group(2))) if match else (stem, 0)
            hint = f"{folder}:{prefix}:{number // 50}"
            rows.append(_row(path, label, folder.split("___")[-1], "plantvillage", group_hint=hint))
    return rows


CDS_LABELS = {"gls": "cercosporiose", "nlb": "outras_manchas", "nls": "outras_manchas"}


def _scan_cds() -> list[dict]:
    rows = []
    root = RAW_DIR / "cds" / "Dataset_Original"
    for path in _images(root):
        folder = path.parent.name
        if folder in CDS_LABELS:
            rows.append(_row(path, CDS_LABELS[folder], folder, "cds"))
    return rows


SEASONAL_LABELS = {"Gray_leaf_spot": "cercosporiose", "Healthy": "saudavel", "Common_rust": "outras_manchas"}


def _scan_seasonal() -> list[dict]:
    rows = []
    for folder, label in SEASONAL_LABELS.items():
        for path in _images(RAW_DIR / "seasonal" / folder):
            rows.append(_row(path, label, folder, "seasonal"))
    return rows


def _scan_digipathos() -> list[dict]:
    rows = []
    root = RAW_DIR / "digipathos"
    rng = np.random.default_rng(0)
    for disease_dir in sorted(p for p in root.iterdir() if p.is_dir() and not p.name.startswith("_")) \
            if root.exists() else []:
        hint = f"digipathos:{disease_dir.name}"
        for path in _images(disease_dir / "original"):
            rows.append(_row(path, "outras_manchas", disease_dir.name, "digipathos", group_hint=hint))
        crops = _images(disease_dir / "cropped")
        keep = set(rng.choice(len(crops), size=min(len(crops), DIGIPATHOS_CROPS_PER_DISEASE),
                              replace=False).tolist()) if crops else set()
        for index, path in enumerate(crops):
            row = _row(path, "outras_manchas", disease_dir.name, "digipathos", group_hint=hint)
            if index not in keep:
                row["excluded"] = f"limite de {DIGIPATHOS_CROPS_PER_DISEASE} recortes por doenca"
            rows.append(row)
    return rows


def _scan_craze() -> list[dict]:
    from vision.craze import scan

    return scan()


SCANNERS = {
    "plantvillage": _scan_plantvillage,
    "cds": _scan_cds,
    "seasonal": _scan_seasonal,
    "craze": _scan_craze,
    "digipathos": _scan_digipathos,
}


# ---------------------------------------------------------------------------------------------
# Leitura das imagens: metadado, hashes e copia reduzida (em paralelo, com cache)
# ---------------------------------------------------------------------------------------------

def processed_path_for(path: str, dataset: str) -> str:
    digest = hashlib.sha1(path.encode("utf-8")).hexdigest()[:20]
    return (PROCESSED_DIR / dataset / f"{digest}.jpg").relative_to(DATA_DIR).as_posix()


def _dhash(image: Image.Image) -> str:
    """dHash de 256 bits (gradiente horizontal numa grade 17x16), em hexadecimal.

    O dHash classico de 64 bits (9x8) dava falso positivo entre folhas diferentes: textura de folha
    reduzida a 9x8 fica parecida demais.
    """
    small = np.asarray(image.convert("L").resize((DHASH_SIZE + 1, DHASH_SIZE), Image.BILINEAR),
                       dtype=np.int16)
    bits = (small[:, 1:] > small[:, :-1]).flatten()
    return np.packbits(bits).tobytes().hex()


def _dhash_of_processed(processed: str) -> str:
    try:
        with Image.open(DATA_DIR / processed) as image:
            return _dhash(image)
    except Exception:  # noqa: BLE001
        return ""


def _exif_time_and_camera(image: Image.Image) -> tuple[str, str]:
    try:
        exif = image.getexif()
        sub = exif.get_ifd(0x8769)
        raw = sub.get(36867) or exif.get(306) or ""
        camera = str(exif.get(272) or "").strip()
        taken = datetime.strptime(str(raw).strip(), "%Y:%m:%d %H:%M:%S").isoformat() if raw else ""
        return taken, camera
    except Exception:  # noqa: BLE001 - EXIF malformado e comum; trata como ausente
        return "", ""


def _process_one(args: tuple[str, str]) -> dict:
    path, processed = args
    source = DATA_DIR / path
    target = DATA_DIR / processed
    data = source.read_bytes()
    result = {"path": path, "md5": hashlib.md5(data).hexdigest(), "error": ""}
    try:
        with Image.open(source) as image:
            taken, camera = _exif_time_and_camera(image)
            image = ImageOps.exif_transpose(image).convert("RGB")
            result.update(width=image.width, height=image.height, taken_at=taken, camera=camera)
            image.thumbnail((PROCESSED_MAX_SIDE, PROCESSED_MAX_SIDE), Image.BICUBIC)
            if not target.exists():
                target.parent.mkdir(parents=True, exist_ok=True)
                image.save(target, "JPEG", quality=92)
    except Exception as exc:  # noqa: BLE001
        result["error"] = f"{type(exc).__name__}: {exc}"
    return result


def _as_text(frame: pd.DataFrame) -> pd.DataFrame:
    """Tudo como texto, com ausente virando string vazia (o CSV e lido com dtype=str)."""
    return frame.astype(object).where(frame.notna(), "").astype(str)


def _load_images(rows: pd.DataFrame, workers: int) -> pd.DataFrame:
    cache = pd.DataFrame()
    if CACHE_PATH.exists():
        cache = pd.read_csv(CACHE_PATH, dtype=str, keep_default_na=False)
    known = set(cache["path"]) if len(cache) else set()
    active = rows[rows["excluded"] == ""]
    todo = [(p, q) for p, q in zip(active["path"], active["processed_path"])
            if p not in known or not (DATA_DIR / q).exists()]
    print(f"imagens: {len(rows)} | a processar: {len(todo)}", flush=True)
    fresh = []
    if todo:
        with ProcessPoolExecutor(max_workers=workers) as pool:
            for index, result in enumerate(pool.map(_process_one, todo, chunksize=16), start=1):
                fresh.append(result)
                if index % 500 == 0:
                    print(f"  {index}/{len(todo)}", flush=True)
    if fresh:
        fresh_frame = _as_text(pd.DataFrame(fresh))
        cache = pd.concat([cache[~cache["path"].isin(fresh_frame["path"])] if len(cache) else cache,
                           fresh_frame], ignore_index=True)
        CACHE_PATH.parent.mkdir(parents=True, exist_ok=True)
        cache.to_csv(CACHE_PATH, index=False)
    return cache.drop(columns=[c for c in ("dhash",) if c in cache.columns])


# ---------------------------------------------------------------------------------------------
# Grupos e protecoes contra vazamento
# ---------------------------------------------------------------------------------------------

def _filename_time(name: str) -> str:
    match = re.search(r"(20\d{2})(\d{2})(\d{2})[_-]?(\d{2})(\d{2})(\d{2})", name)
    if not match:
        return ""
    try:
        return datetime(*map(int, match.groups())).isoformat()
    except ValueError:
        return ""


def _session_group(row: pd.Series) -> str:
    """Sessao de captura: camera + janela de SESSION_MINUTES minutos. Sem horario, faixa do numero."""
    taken = row["taken_at"] or _filename_time(Path(row["path"]).name)
    if taken:
        moment = datetime.fromisoformat(taken)
        window = (moment.hour * 60 + moment.minute) // SESSION_MINUTES
        return f"{row['dataset']}:{row['camera'] or '-'}:{moment.date()}:{window}"
    stem = Path(row["path"]).stem
    match = re.match(r"^(.*?)[\s_]*(\d+)", stem)
    if match:
        return f"{row['dataset']}:{match.group(1)}:{int(match.group(2)) // 20}"
    return f"{row['dataset']}:{row['path']}"


def _assign_groups(frame: pd.DataFrame) -> pd.Series:
    groups = []
    for _, row in frame.iterrows():
        if row["group_hint"]:
            groups.append(row["group_hint"] if row["group_hint"].startswith(row["dataset"])
                          else f"{row['dataset']}:{row['group_hint']}")
        else:
            groups.append(_session_group(row))
    return pd.Series(groups, index=frame.index)


class _UnionFind:
    def __init__(self) -> None:
        self.parent: dict[str, str] = {}

    def find(self, key: str) -> str:
        self.parent.setdefault(key, key)
        while self.parent[key] != key:
            self.parent[key] = self.parent[self.parent[key]]
            key = self.parent[key]
        return key

    def union(self, a: str, b: str) -> None:
        ra, rb = self.find(a), self.find(b)
        if ra != rb:
            self.parent[max(ra, rb)] = min(ra, rb)


def _exclude_exact_duplicates(frame: pd.DataFrame) -> dict:
    stats = {"exact_duplicates": 0, "conflicting_labels": 0}
    active = frame[frame["excluded"] == ""]
    for _, index in active.groupby("md5").groups.items():
        if len(index) < 2:
            continue
        copies = frame.loc[index]
        if copies["label"].nunique() > 1:
            frame.loc[index, "excluded"] = "duplicata exata com rotulos conflitantes"
            stats["conflicting_labels"] += len(index)
            continue
        order = copies["dataset"].map(DATASET_PRIORITY.index).sort_values(kind="stable").index
        keeper = order[0]
        for other in order[1:]:
            frame.at[other, "excluded"] = f"duplicata exata de {frame.at[keeper, 'path']}"
            stats["exact_duplicates"] += 1
    return stats


def _handle_near_duplicates(frame: pd.DataFrame) -> dict:
    active = frame[(frame["excluded"] == "") & (frame["dhash"] != "")]
    hashes = np.frombuffer(b"".join(bytes.fromhex(h) for h in active["dhash"]),
                           dtype=np.uint64).reshape(len(active), -1)
    index = active.index.to_numpy()
    datasets = active["dataset"].to_numpy()
    union = _UnionFind()
    stats = {"near_duplicate_pairs": 0, "groups_merged": 0, "cross_dataset_excluded": 0}
    chunk = 256
    for start in range(0, len(hashes), chunk):
        block = hashes[start:start + chunk]
        distance = np.bitwise_count(block[:, None, :] ^ hashes[None, :, :]).sum(axis=2)
        rows, cols = np.nonzero(distance <= DHASH_MAX_DISTANCE)
        for r, c in zip(rows + start, cols):
            if c <= r:
                continue
            stats["near_duplicate_pairs"] += 1
            a, b = index[r], index[c]
            if datasets[r] == datasets[c]:
                ga, gb = frame.at[a, "group"], frame.at[b, "group"]
                if union.find(ga) != union.find(gb):
                    union.union(ga, gb)
                    stats["groups_merged"] += 1
            else:
                pa, pb = DATASET_PRIORITY.index(datasets[r]), DATASET_PRIORITY.index(datasets[c])
                loser, keeper = (b, a) if pa <= pb else (a, b)
                if frame.at[loser, "excluded"] == "":
                    frame.at[loser, "excluded"] = f"quase duplicata de {frame.at[keeper, 'path']}"
                    stats["cross_dataset_excluded"] += 1
    frame["group"] = frame["group"].map(union.find)
    return stats


def build(workers: int = 8) -> pd.DataFrame:
    rows = []
    found = {}
    for dataset, scanner in SCANNERS.items():
        scanned = scanner()
        found[dataset] = len(scanned)
        rows.extend(scanned)
    print("imagens por conjunto:", found, flush=True)
    frame = pd.DataFrame(rows)
    frame["processed_path"] = [processed_path_for(p, d) for p, d in zip(frame["path"], frame["dataset"])]
    images = _load_images(frame, workers)
    frame = _as_text(frame.merge(images, on="path", how="left"))
    frame.loc[(frame["error"] != "") & (frame["excluded"] == ""), "excluded"] = "imagem ilegivel"
    frame["dhash"] = ""
    active = frame["excluded"] == ""
    with ProcessPoolExecutor(max_workers=workers) as pool:
        frame.loc[active, "dhash"] = list(pool.map(_dhash_of_processed, frame.loc[active, "processed_path"],
                                                    chunksize=64))
    frame["domain"] = frame["dataset"].map(DATASET_DOMAIN)
    frame["group"] = _assign_groups(frame)

    stats = {"found": found}
    stats.update(_exclude_exact_duplicates(frame))
    stats.update(_handle_near_duplicates(frame))

    columns = ["path", "processed_path", "label", "original_label", "dataset", "domain", "group",
               "excluded", "md5", "dhash", "width", "height", "taken_at", "camera"]
    extra = [c for c in frame.columns if c.startswith("meta_")]
    frame = frame[columns + extra].sort_values(["dataset", "path"]).reset_index(drop=True)
    frame.to_csv(MANIFEST_PATH, index=False)

    kept = frame[frame["excluded"] == ""]
    stats["kept"] = int(len(kept))
    stats["excluded_by_reason"] = (frame.loc[frame["excluded"] != "", "excluded"]
                                   .str.replace(r" (de|com) .*", "", regex=True).value_counts().to_dict())
    stats["kept_by_dataset_label"] = {f"{d}|{l}": int(n) for (d, l), n
                                      in kept.groupby(["dataset", "label"]).size().items()}
    stats["groups_by_dataset"] = kept.groupby("dataset")["group"].nunique().to_dict()
    (DATA_DIR / "manifest.summary.json").write_text(json.dumps(stats, indent=2, ensure_ascii=False),
                                                   encoding="utf-8")
    print(json.dumps(stats, indent=2, ensure_ascii=False))
    return frame


if __name__ == "__main__":
    build()
