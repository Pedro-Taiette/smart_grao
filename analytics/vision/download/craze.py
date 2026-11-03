"""Maize In-Field (Craze et al., 2022; Kaggle hamishcrazeai/maize-in-field-dataset): campo, Africa
do Sul, CC BY-NC-SA 4.0, multirrotulo. E o eixo do treino e o conjunto de teste separado.

Usa o kagglehub. Em 2026-11-02 o download anonimo funcionou para este conjunto publico. Se o Kaggle
passar a exigir login, o kagglehub levanta erro de autenticacao: o status vira "manual" e o passo e
criar a credencial em ~/.kaggle/kaggle.json (ou KAGGLE_USERNAME/KAGGLE_KEY) e rodar de novo. Nenhuma
credencial e criada por este script.
"""

from __future__ import annotations

from pathlib import Path

from vision.config import IMAGE_EXTENSIONS
from vision.download.common import raw_dir, run_guarded

HANDLE = "hamishcrazeai/maize-in-field-dataset"


def download() -> dict:
    import kagglehub  # import local: o pacote so e necessario para este conjunto

    target = raw_dir("craze") / "kaggle"
    try:
        path = kagglehub.dataset_download(HANDLE, output_dir=str(target))
    except Exception as exc:  # noqa: BLE001
        text = f"{type(exc).__name__}: {exc}"
        if any(word in text.lower() for word in ("401", "403", "credential", "unauthenticated", "login")):
            return {
                "status": "manual",
                "error": text,
                "manual_step": "Criar ~/.kaggle/kaggle.json com a chave da conta Kaggle e rodar "
                               "python -m vision.download.craze de novo.",
            }
        raise
    images = [p for p in Path(path).rglob("*") if p.suffix.lower() in IMAGE_EXTENSIONS]
    return {"source": f"https://www.kaggle.com/datasets/{HANDLE}", "path": str(path),
            "images": len(images)}


def main() -> None:
    run_guarded("craze", download)


if __name__ == "__main__":
    main()
