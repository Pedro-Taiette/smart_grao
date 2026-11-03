"""PlantVillage (spMohanty/PlantVillage-Dataset, GitHub): so as pastas de milho, versao colorida.

Usa clone raso com sparse-checkout para nao baixar o repositorio inteiro (as outras culturas).
Laboratorio, fundo uniforme. O leaf-map.json do repositorio nao cobre o milho, entao o grupo da
divisao e aproximado pelo numero sequencial do arquivo (ver manifest.py).
"""

from __future__ import annotations

import subprocess

from vision.download.common import raw_dir, run_guarded

REPO_URL = "https://github.com/spMohanty/PlantVillage-Dataset.git"
CORN_PATTERN = "/raw/color/Corn_(maize)___*"


def _git(*args: str, cwd=None) -> None:
    subprocess.run(["git", *args], cwd=cwd, check=True)


def download() -> dict:
    target = raw_dir("plantvillage") / "repo"
    if not (target / ".git").exists():
        _git("clone", "--depth", "1", "--filter=blob:none", "--no-checkout", REPO_URL, str(target))
    _git("sparse-checkout", "set", "--no-cone", CORN_PATTERN, cwd=target)
    _git("checkout", cwd=target)
    color = target / "raw" / "color"
    counts = {d.name: sum(1 for _ in d.iterdir()) for d in sorted(color.iterdir()) if d.is_dir()}
    return {"source": REPO_URL, "images": sum(counts.values()), "per_folder": counts}


def main() -> None:
    run_guarded("plantvillage", download)


if __name__ == "__main__":
    main()
