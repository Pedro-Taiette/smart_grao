from dataclasses import asdict
from datetime import date

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from . import satellite

app = FastAPI(title="SmartGrao Analytics")


class SatelliteRequest(BaseModel):
    polygon: dict
    start: date
    end: date


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/satellite/passes")
def satellite_passes(request: SatelliteRequest):
    if request.polygon.get("type") != "Polygon":
        raise HTTPException(status_code=422, detail="polygon must be a GeoJSON Polygon")
    if request.end < request.start:
        raise HTTPException(status_code=422, detail="end must not precede start")
    passes = satellite.analyze(request.polygon, request.start, request.end)
    return {"collection": satellite.COLLECTION, "passes": [asdict(item) for item in passes]}
