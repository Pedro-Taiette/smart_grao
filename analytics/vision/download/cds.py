"""CD&S (Ahmad et al., 2021; OSF s6ru5): so Dataset_Original, sem as versoes aumentadas ou de
fundo removido. Campo, EUA, um local e um mes. Licenca nao declarada no OSF.

Mantem a divisao train/test do autor como subpasta, so como rotulo; a divisao do projeto e refeita
por grupo no split.py.
"""

from __future__ import annotations

from pathlib import Path

from vision.download.common import download_many, fetch_json, raw_dir, run_guarded

NODE = "s6ru5"
# Pasta Dataset_Original no osfstorage (listada em 2026-11-02)
ORIGINAL_FOLDER_ID = "60be295acb2a5e0501690f31"
API = "https://api.osf.io/v2"


def _list_folder(url: str, prefix: Path, out: list[tuple[str, Path, int | None]]) -> None:
    while url:
        page = fetch_json(url)
        for item in page["data"]:
            attrs = item["attributes"]
            if attrs["kind"] == "folder":
                _list_folder(item["relationships"]["files"]["links"]["related"]["href"],
                             prefix / attrs["name"], out)
            else:
                out.append((item["links"]["download"], prefix / attrs["name"], attrs.get("size")))
        url = page["links"].get("next")


def download() -> dict:
    root = raw_dir("cds") / "Dataset_Original"
    jobs: list[tuple[str, Path, int | None]] = []
    _list_folder(f"{API}/nodes/{NODE}/files/osfstorage/{ORIGINAL_FOLDER_ID}/", root, jobs)
    # Poucas conexoes: com 8 o OSF respondeu 429 (Too Many Requests) em ~5% dos arquivos
    done, errors = download_many(jobs, workers=3, label="cds")
    return {
        "status": "ok" if not errors else "parcial",
        "source": f"https://osf.io/{NODE}/ (Dataset_Original)",
        "files_listed": len(jobs),
        "files_ok": done,
        "errors": errors[:20],
    }


def main() -> None:
    run_guarded("cds", download)


if __name__ == "__main__":
    main()
