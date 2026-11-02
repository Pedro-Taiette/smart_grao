from dataclasses import asdict
from datetime import date

from fastapi import FastAPI, File, HTTPException, UploadFile
from pydantic import BaseModel

from . import satellite, vision

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


@app.post("/vision/classify")
async def vision_classify(file: UploadFile = File(...), heatmap: bool = True):
    content = await file.read()
    if not content:
        raise HTTPException(status_code=422, detail="empty file")
    try:
        return vision.classify(content, heatmap)
    except FileNotFoundError as error:
        raise HTTPException(status_code=503, detail=str(error))


@app.get("/vision/model")
def vision_model():
    try:
        return vision.model_card()
    except FileNotFoundError as error:
        raise HTTPException(status_code=503, detail=str(error))
