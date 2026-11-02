"""Utilitarios de download: requisicao com retentativa, baixa paralela e registro de status.

Cada conjunto grava o resultado em data/raw/<conjunto>/download_status.json, consolidado em
data/download_log.json. Uma falha num conjunto e registrada e
nao impede os demais.
"""

from __future__ import annotations

import json
import shutil
import time
import traceback
import urllib.request
from collections.abc import Callable, Iterable
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from pathlib import Path

from vision.config import DOWNLOAD_LOG_PATH, RAW_DIR

USER_AGENT = "smartgrao-analytics/0.1 (+https://github.com/Pedro-Taiette)"


def raw_dir(dataset: str) -> Path:
    path = RAW_DIR / dataset
    path.mkdir(parents=True, exist_ok=True)
    return path


def fetch_json(url: str, retries: int = 5) -> dict:
    last_error: Exception | None = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=60) as resp:
                return json.load(resp)
        except Exception as exc:  # noqa: BLE001
            last_error = exc
            time.sleep(2 ** attempt)
    raise RuntimeError(f"Falha ao ler {url}: {last_error}")


def download_file(url: str, target: Path, expected_size: int | None = None, retries: int = 5) -> Path:
    """Baixa para um .part e renomeia no fim; pula se o arquivo final ja existe com o tamanho certo."""
    if target.exists() and (expected_size is None or target.stat().st_size == expected_size):
        return target
    target.parent.mkdir(parents=True, exist_ok=True)
    partial = target.with_suffix(target.suffix + ".part")
    last_error: Exception | None = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=120) as resp, open(partial, "wb") as out:
                shutil.copyfileobj(resp, out, length=1 << 20)
            if expected_size is not None and partial.stat().st_size != expected_size:
                raise RuntimeError(f"tamanho {partial.stat().st_size} != {expected_size}")
            partial.replace(target)
            return target
        except Exception as exc:  # noqa: BLE001
            last_error = exc
            # 429: o OSF limita a taxa; espera bem mais antes de tentar de novo
            throttled = getattr(exc, "code", None) == 429
            time.sleep(30 * (attempt + 1) if throttled else 2 ** attempt)
    raise RuntimeError(f"Falha ao baixar {url}: {last_error}")


def download_many(jobs: Iterable[tuple[str, Path, int | None]], workers: int = 8,
                  label: str = "") -> tuple[int, list[str]]:
    """Baixa varios arquivos em paralelo. Devolve (quantos ok, lista de erros)."""
    jobs = list(jobs)
    errors: list[str] = []
    done = 0
    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures = {pool.submit(download_file, url, target, size): url for url, target, size in jobs}
        for index, future in enumerate(as_completed(futures), start=1):
            try:
                future.result()
                done += 1
            except Exception as exc:  # noqa: BLE001
                errors.append(str(exc))
            if index % 100 == 0 or index == len(jobs):
                print(f"[{label}] {index}/{len(jobs)} arquivos ({len(errors)} erros)", flush=True)
    return done, errors


def record_status(dataset: str, status: str, **details) -> None:
    """status: ok | parcial | falhou | manual. Um arquivo por conjunto, para os downloads poderem
    rodar em paralelo sem disputar o mesmo arquivo; collect_log() junta tudo."""
    entry = {
        "status": status,
        "updated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        **details,
    }
    path = raw_dir(dataset) / "download_status.json"
    path.write_text(json.dumps(entry, indent=2, ensure_ascii=False), encoding="utf-8")


def collect_log() -> dict:
    log = {}
    for path in sorted(RAW_DIR.glob("*/download_status.json")):
        log[path.parent.name] = json.loads(path.read_text(encoding="utf-8"))
    DOWNLOAD_LOG_PATH.write_text(json.dumps(log, indent=2, ensure_ascii=False), encoding="utf-8")
    return log


def run_guarded(dataset: str, func: Callable[[], dict]) -> bool:
    """Executa o download de um conjunto; qualquer excecao vira status 'falhou' no log."""
    try:
        details = func()
        status = details.pop("status", "ok")
        record_status(dataset, status, **details)
        print(f"[{dataset}] {status}: {details}", flush=True)
        return status in ("ok", "parcial")
    except Exception as exc:  # noqa: BLE001
        record_status(dataset, "falhou", error=f"{type(exc).__name__}: {exc}",
                      traceback=traceback.format_exc(limit=3))
        print(f"[{dataset}] falhou: {exc}", flush=True)
        return False
