"""Linha comparavel de cada execucao no craze_dev (parte "test" da lodo_craze_halves).

    python -m vision.report runs/s_lpft6 runs/b_lpft8 [--markdown]

Le eval_test.json (ou eval_lodo_craze_halves_test.json, de uma reavaliacao) e, se houver, wise.json.
Nunca le resultado da parte "final".
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

FILES = ("eval_lodo_craze_halves_test.json", "eval_test.json")
COLUMNS = ("execucao", "val F1", "dev F1", "recall cerco", "precisao cerco", "recall saudavel", "ECE")


def rows(run: Path) -> list[tuple]:
    out = []
    for name in FILES:
        path = run / name
        if path.exists():
            test = json.loads(path.read_text(encoding="utf-8"))
            if test.get("split_file") not in (None, "lodo_craze_halves.csv") or test.get("part") == "final":
                continue
            val_path = run / "eval_val.json"
            val = json.loads(val_path.read_text(encoding="utf-8"))["macro_f1"] if val_path.exists() else None
            per_class = test["per_class"]
            out.append((run.name, val, test["macro_f1"], per_class["cercosporiose"]["recall"],
                        per_class["cercosporiose"]["precision"], per_class["saudavel"]["recall"],
                        test["calibration"]["ece"]))
            break
    wise = run / "wise.json"
    if wise.exists():
        for alpha, entry in json.loads(wise.read_text(encoding="utf-8")).items():
            t = entry["test"]
            out.append((f"{run.name} WiSE a={alpha}", entry["val"]["macro_f1"], t["macro_f1"],
                        t["recall_cercosporiose"], t["precision_cercosporiose"], t["recall_saudavel"], t["ece"]))
    return out


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("runs", type=Path, nargs="+")
    parser.add_argument("--markdown", action="store_true")
    args = parser.parse_args()
    table = [row for run in args.runs for row in rows(run)]
    fmt = (lambda v: "-" if v is None else (f"{v:.3f}" if isinstance(v, float) else str(v)))
    if args.markdown:
        print("| " + " | ".join(COLUMNS) + " |")
        print("|" + "---|" * len(COLUMNS))
        for row in table:
            print("| " + " | ".join(fmt(v) for v in row) + " |")
        return
    for row in table:
        print(f"{row[0]:32s} " + " ".join(f"{fmt(v):>7s}" for v in row[1:]))


if __name__ == "__main__":
    main()
