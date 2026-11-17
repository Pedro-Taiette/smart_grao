"""Baixa os conjuntos em sequencia: python -m vision.download [plantvillage cds seasonal craze digipathos]

Sem argumentos, baixa os cinco do manifesto. bdmaize, figmaize e plantdoc (avaliados e rejeitados
para treino) so baixam se nomeados. Uma falha e registrada e o proximo conjunto segue.
"""

from __future__ import annotations

import importlib
import json
import sys

from vision.config import DATASETS, EXTRA_DATASETS
from vision.download.common import collect_log


def main(argv: list[str]) -> None:
    selected = argv or list(DATASETS)
    unknown = set(selected) - set(DATASETS) - set(EXTRA_DATASETS)
    if unknown:
        raise SystemExit(f"Conjuntos desconhecidos: {sorted(unknown)}. Validos: {DATASETS + EXTRA_DATASETS}")
    for name in selected:
        importlib.import_module(f"vision.download.{name}").main()
    log = collect_log()
    print(json.dumps({k: v["status"] for k, v in log.items()}, indent=2))


if __name__ == "__main__":
    main(sys.argv[1:])
