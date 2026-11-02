"""Estacao sentinela simulada: envia fotos de folha do terco inferior para um cultivo, como faria um
Raspberry Pi preso a uma planta. Sem hardware nem lavoura, as fotos vem do conjunto de teste da
trilha B e chegam marcadas como simuladas.

    .venv/Scripts/python simulate_station.py --cultivation <id> --count 6 --interval 0
"""

from __future__ import annotations

import argparse
import csv
import random
import time
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path

import httpx

DATA = Path(__file__).resolve().parents[1] / "data"


def test_images(label: str | None) -> list[Path]:
    split = DATA / "splits" / "grouped.csv"
    rows = list(csv.DictReader(split.open(encoding="utf-8")))
    field = {"craze", "cds", "seasonal"}
    picked = [r for r in rows if r["split"] == "test" and r["dataset"] in field and (label is None or r["label"] == label)]
    return [DATA / r["processed_path"] for r in picked if (DATA / r["processed_path"]).exists()]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--api", default="http://localhost:5148")
    parser.add_argument("--cultivation", required=True)
    parser.add_argument("--count", type=int, default=6)
    parser.add_argument("--interval", type=float, default=0, help="segundos entre fotos")
    parser.add_argument("--label", choices=["cercosporiose", "saudavel", "outras_manchas"], default=None)
    parser.add_argument("--latitude", type=float)
    parser.add_argument("--longitude", type=float)
    args = parser.parse_args()

    images = test_images(args.label)
    random.seed(42)
    start = datetime.now(timezone.utc) - timedelta(hours=4 * args.count)
    with httpx.Client(base_url=args.api, timeout=60) as client:
        for index, image in enumerate(random.sample(images, min(args.count, len(images)))):
            data = {
                "id": str(uuid.uuid4()), "cultivationId": args.cultivation, "source": "Station", "isSimulated": "true",
                "organ": "LowerLeaves", "takenAt": (start + timedelta(hours=4 * index)).isoformat(),
            }
            if args.latitude is not None and args.longitude is not None:
                data |= {"latitude": str(args.latitude), "longitude": str(args.longitude)}
            response = client.post("/api/photos/device", data=data,
                                   files={"file": (image.name, image.read_bytes(), "image/jpeg")})
            response.raise_for_status()
            print(f"{index + 1}/{args.count} {image.parent.name}/{image.name} -> {response.json()['id']}")
            if args.interval:
                time.sleep(args.interval)


if __name__ == "__main__":
    main()
