"""Divisao treino/validacao/teste agrupada, sem vazamento: nenhum grupo aparece em dois lados.

    python -m vision.split grouped                 # data/splits/grouped.csv
    python -m vision.split lodo --holdout craze    # data/splits/lodo_craze.csv
    python -m vision.split lodo --holdout craze --halves              # lodo_craze_halves.csv
    python -m vision.split lodo --holdout cds --exclude craze         # lodo_cds_sem_craze.csv
    python -m vision.split all                     # data/splits/all.csv (tudo treino: producao)

- grouped: cada conjunto e dividido por grupo (estratificado pela classe) em ~70/15/15, e as partes
  sao unidas. O teste mistura os conjuntos de campo e de laboratorio; os numeros por conjunto e por
  dominio saem separados na avaliacao.
- lodo (leave-one-dataset-out): o conjunto escolhido vira o teste inteiro; os demais sao divididos
  por grupo em treino e validacao (~85/15). Mede a troca de dominio (outro pais, outra camera).
  Com --halves, o conjunto de fora e dividido por grupo, estratificado, em duas metades: "test"
  (craze_dev, usado para escolher receita) e "final" (craze_final, avaliado uma vez so, no fim). A
  atribuicao fica em data/splits/<conjunto>_dev_final.csv, versionado: se o arquivo existe, ele manda
  (mesmas metades em qualquer maquina); se falta imagem nele, a divisao para. O treino e a validacao
  sao os mesmos do lodo sem --halves. --exclude tira conjuntos inteiros (folds
  auxiliares sem o Craze, para nenhuma foto dele entrar em treino de selecao).
- all: tudo e treino, para o modelo de producao (numero de epocas fixado antes, sem validacao).

Conjuntos marcados como so-treino (Digipathos: laboratorio, so "outras_manchas" e grupo grosseiro)
nunca entram na validacao nem no teste.

O aumento de dados nao acontece aqui: ele e aplicado so no carregamento do treino (data.py).
"""

from __future__ import annotations

import argparse
import json

import numpy as np
import pandas as pd
from sklearn.model_selection import StratifiedGroupKFold

from vision.config import MANIFEST_PATH, SPLITS_DIR

TRAIN_ONLY_DATASETS = {"digipathos"}
COLUMNS = ["path", "processed_path", "label", "original_label", "dataset", "group"]


def _group_folds(frame: pd.DataFrame, n_splits: int, seed: int) -> np.ndarray:
    """Atribui a cada linha um indice de dobra; todas as linhas de um grupo caem na mesma dobra."""
    folds = np.full(len(frame), -1)
    n_groups = frame["group"].nunique()
    if n_groups < n_splits:
        # Poucos grupos: cada grupo vira a propria dobra, em ordem aleatoria fixa
        rng = np.random.default_rng(seed)
        groups = frame["group"].unique()
        rng.shuffle(groups)
        mapping = {g: i % n_splits for i, g in enumerate(groups)}
        return frame["group"].map(mapping).to_numpy()
    splitter = StratifiedGroupKFold(n_splits=n_splits, shuffle=True, random_state=seed)
    for fold, (_, index) in enumerate(splitter.split(frame, frame["label"], frame["group"])):
        folds[index] = fold
    return folds


def grouped_split(manifest: pd.DataFrame, seed: int) -> pd.DataFrame:
    parts = []
    for dataset, frame in manifest.groupby("dataset"):
        frame = frame.copy()
        if dataset in TRAIN_ONLY_DATASETS:
            frame["split"] = "train"
        else:
            # 7 dobras: 1 teste, 1 validacao, 5 treino (~14/14/72)
            folds = _group_folds(frame, n_splits=7, seed=seed)
            frame["split"] = np.select([folds == 0, folds == 1], ["test", "val"], "train")
        parts.append(frame)
    return pd.concat(parts, ignore_index=True)


def _halves(frame: pd.DataFrame, holdout: str, seed: int) -> np.ndarray:
    reference = SPLITS_DIR / f"{holdout}_dev_final.csv"
    if reference.exists():
        known = pd.read_csv(reference, dtype=str, keep_default_na=False).set_index("path")["split"]
        missing = sorted(set(frame["path"]) - set(known.index))
        if missing:
            raise SystemExit(f"{len(missing)} imagens fora de {reference.name} (ex.: {missing[:3]}); "
                             "o manifesto difere do usado para fixar as metades")
        return frame["path"].map(known).to_numpy()
    folds = _group_folds(frame, n_splits=2, seed=seed)
    sides = np.where(folds == 0, "test", "final")
    SPLITS_DIR.mkdir(parents=True, exist_ok=True)
    frame.assign(split=sides)[["path", "group", "label", "split"]].to_csv(reference, index=False)
    return sides


def lodo_split(manifest: pd.DataFrame, holdout: str, seed: int, halves: bool = False) -> pd.DataFrame:
    if holdout not in set(manifest["dataset"]):
        raise SystemExit(f"Conjunto {holdout!r} nao esta no manifesto")
    if holdout in TRAIN_ONLY_DATASETS:
        raise SystemExit(f"{holdout} e so-treino e nao serve de teste")
    test = manifest[manifest["dataset"] == holdout].copy()
    test["split"] = "test"
    if halves:
        test["split"] = _halves(test, holdout, seed)
    parts = [test]
    for dataset, frame in manifest[manifest["dataset"] != holdout].groupby("dataset"):
        frame = frame.copy()
        if dataset in TRAIN_ONLY_DATASETS:
            frame["split"] = "train"
        else:
            folds = _group_folds(frame, n_splits=7, seed=seed)
            frame["split"] = np.where(folds == 0, "val", "train")
        parts.append(frame)
    return pd.concat(parts, ignore_index=True)


def check_no_leak(split: pd.DataFrame) -> None:
    sides = split.groupby("group")["split"].nunique()
    leaking = sides[sides > 1]
    if len(leaking):
        raise AssertionError(f"{len(leaking)} grupos em mais de uma parte: {list(leaking.index[:5])}")


def describe(split: pd.DataFrame) -> dict:
    table = split.groupby(["split", "dataset", "label"]).size().unstack(fill_value=0)
    return {
        "rows": int(len(split)),
        "groups": int(split["group"].nunique()),
        "by_split": split["split"].value_counts().to_dict(),
        "by_split_dataset_label": {f"{s}|{d}": row.to_dict() for (s, d), row in table.iterrows()},
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("mode", choices=["grouped", "lodo", "all"])
    parser.add_argument("--holdout", default="craze")
    parser.add_argument("--halves", action="store_true", help="divide o conjunto de fora em test/final")
    parser.add_argument("--exclude", nargs="*", default=[], help="conjuntos tirados por inteiro")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    manifest = pd.read_csv(MANIFEST_PATH, dtype=str, keep_default_na=False)
    manifest = manifest[manifest["excluded"] == ""][COLUMNS].reset_index(drop=True)
    manifest = manifest[~manifest["dataset"].isin(args.exclude)].reset_index(drop=True)
    if args.mode == "grouped":
        split, name = grouped_split(manifest, args.seed), "grouped"
    elif args.mode == "all":
        split, name = manifest.assign(split="train"), "all"
    else:
        split = lodo_split(manifest, args.holdout, args.seed, args.halves)
        name = f"lodo_{args.holdout}" + ("_halves" if args.halves else "")
    if args.exclude:
        name += "_sem_" + "_".join(sorted(args.exclude))
    check_no_leak(split)
    SPLITS_DIR.mkdir(parents=True, exist_ok=True)
    split.to_csv(SPLITS_DIR / f"{name}.csv", index=False)
    summary = describe(split)
    (SPLITS_DIR / f"{name}.summary.json").write_text(json.dumps(summary, indent=2, ensure_ascii=False),
                                                    encoding="utf-8")
    print(json.dumps({k: summary[k] for k in ("rows", "groups", "by_split")}, indent=2))
    print(split.groupby(["split", "label"]).size().unstack(fill_value=0))


if __name__ == "__main__":
    main()
