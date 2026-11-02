"""Sentinel-2 L2A sobre o contorno de um talhao: mascara de nuvem, NDVI, NDRE e zonas que destoam.

Decisoes e fontes em docs/fase-6-satelite.md.
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from datetime import date

import numpy as np
import rasterio
from pyproj import Transformer
from pystac_client import Client
from rasterio.enums import Resampling
from rasterio.features import geometry_mask, shapes
from rasterio.windows import from_bounds
from scipy import ndimage
from shapely.geometry import Polygon, mapping, shape
from shapely.ops import transform as shapely_transform, unary_union

STAC_URL = "https://earth-search.aws.element84.com/v1"
COLLECTION = "sentinel-2-c1-l2a"
MASKED_SCL = (0, 1, 3, 8, 9, 10)
MINIMUM_VALID_FRACTION = 0.6
ANOMALY_DROP = 0.15
PREVIOUS_DROP = 0.03
MINIMUM_ZONE_HECTARES = 0.5
CANOPY_NDVI = 0.4
PIXEL_HECTARES = 0.04

os.environ.setdefault("AWS_NO_SIGN_REQUEST", "YES")
os.environ.setdefault("GDAL_DISABLE_READDIR_ON_OPEN", "EMPTY_DIR")
os.environ.setdefault("CPL_VSIL_CURL_ALLOWED_EXTENSIONS", ".tif")
os.environ.setdefault("GDAL_HTTP_MAX_RETRY", "3")


@dataclass
class IndexStats:
    mean: float
    median: float
    p10: float
    p90: float


@dataclass
class Zone:
    geometry: dict
    area_hectares: float
    mean_drop: float
    dropped_since_previous: bool


@dataclass
class Pass:
    date: date
    scene_id: str
    valid_fraction: float
    status: str
    ndvi: IndexStats | None = None
    ndre: IndexStats | None = None
    zones: list[Zone] = field(default_factory=list)


def search_items(polygon: dict, start: date, end: date):
    client = Client.open(STAC_URL)
    search = client.search(
        collections=[COLLECTION], intersects=polygon, datetime=f"{start.isoformat()}/{end.isoformat()}",
        max_items=500,
    )
    items = sorted(search.items(), key=lambda item: item.datetime)
    if not items:
        return []
    # Um talhao na divisa de dois tiles aparece duas vezes na mesma data; fica o tile mais frequente,
    # para que as passagens sucessivas caiam na mesma grade.
    tiles = [item.properties.get("grid:code") for item in items]
    tile = max(set(tiles), key=tiles.count)
    by_date: dict[date, object] = {}
    for item in items:
        if item.properties.get("grid:code") == tile:
            by_date.setdefault(item.datetime.date(), item)
    return list(by_date.values())


def _scale_offset(asset) -> tuple[float, float]:
    bands = asset.extra_fields.get("raster:bands") or [{}]
    return float(bands[0].get("scale", 0.0001)), float(bands[0].get("offset", -0.1))


def _read(asset, bounds, shape_hw=None, resampling=Resampling.nearest):
    with rasterio.open(asset.href) as src:
        window = from_bounds(*bounds, transform=src.transform).round_offsets().round_lengths()
        out_shape = shape_hw or (int(window.height), int(window.width))
        data = src.read(1, window=window, out_shape=out_shape, resampling=resampling, boundless=True, fill_value=0)
        transform = src.window_transform(window)
        if shape_hw is None:
            return data, transform, src.crs
        return data


def _stats(values: np.ndarray) -> IndexStats:
    return IndexStats(
        mean=float(np.mean(values)), median=float(np.median(values)),
        p10=float(np.percentile(values, 10)), p90=float(np.percentile(values, 90)),
    )


def anomaly_mask(ndre: np.ndarray, valid: np.ndarray, median: float) -> np.ndarray:
    """Pixels pelo menos ANOMALY_DROP abaixo da mediana do proprio talhao, sem os grupos pequenos."""
    if median <= 0.05:
        return np.zeros_like(valid)
    low = valid & ((ndre - median) / abs(median) <= -ANOMALY_DROP)
    labels, count = ndimage.label(low, structure=np.ones((3, 3)))
    if count == 0:
        return low
    sizes = ndimage.sum(low, labels, index=np.arange(1, count + 1))
    keep = np.isin(labels, np.flatnonzero(sizes * PIXEL_HECTARES >= MINIMUM_ZONE_HECTARES) + 1)
    return keep


def _zones(mask, ndre, median, previous, transform, to_wgs84, outline_utm) -> list[Zone]:
    labels, count = ndimage.label(mask, structure=np.ones((3, 3)))
    zones = []
    for index in range(1, count + 1):
        component = labels == index
        polygons = [shape(geom) for geom, value in shapes(component.astype(np.uint8), mask=component, transform=transform) if value == 1]
        # Meio metro para fora junta os pixels que se tocam so pela quina.
        merged = unary_union(polygons).buffer(0.5, join_style="mitre").intersection(outline_utm)
        drop = float(np.mean((ndre[component] - median) / abs(median)))
        dropped = previous is not None and bool(np.nanmean(ndre[component] - previous[component]) <= -PREVIOUS_DROP)
        parts = [g for g in getattr(merged, "geoms", [merged]) if g.geom_type == "Polygon"]
        for part in parts:
            area = float(part.area / 10_000)
            if area < MINIMUM_ZONE_HECTARES:
                continue
            part = shapely_transform(to_wgs84, part).simplify(0.00005)
            # Buracos sao preenchidos: a vistoria caminha a zona inteira.
            outline = Polygon(part.exterior).buffer(0)
            if outline.geom_type == "MultiPolygon":
                outline = max(outline.geoms, key=lambda candidate: candidate.area)
            if outline.is_empty or len(outline.exterior.coords) < 4:
                continue
            zones.append(Zone(mapping(outline), area, drop, dropped))
    return zones


def analyze(polygon: dict, start: date, end: date) -> list[Pass]:
    items = search_items(polygon, start, end)
    passes: list[Pass] = []
    previous_ndre = None
    previous_grid = None
    geometry = shape(polygon)
    for item in items:
        scl_asset = item.assets["scl"]
        with rasterio.open(scl_asset.href) as src:
            crs = src.crs
        to_utm = Transformer.from_crs("EPSG:4326", crs, always_xy=True).transform
        to_wgs84 = Transformer.from_crs(crs, "EPSG:4326", always_xy=True).transform
        field_utm = shapely_transform(to_utm, geometry)
        bounds = field_utm.buffer(40).bounds

        scl, grid, _ = _read(scl_asset, bounds)
        inside = geometry_mask([mapping(field_utm)], out_shape=scl.shape, transform=grid, invert=True, all_touched=False)
        clear = inside & ~np.isin(scl, MASKED_SCL)
        fraction = float(clear.sum() / max(inside.sum(), 1))
        if fraction < MINIMUM_VALID_FRACTION:
            passes.append(Pass(item.datetime.date(), item.id, fraction, "no_reading"))
            continue

        bands = {}
        for name in ("red", "nir", "rededge1", "nir08"):
            asset = item.assets[name]
            scale, offset = _scale_offset(asset)
            raw = _read(asset, bounds, shape_hw=scl.shape, resampling=Resampling.average).astype(np.float32)
            bands[name] = np.where(raw > 0, raw * scale + offset, np.nan)

        with np.errstate(divide="ignore", invalid="ignore"):
            ndvi = (bands["nir"] - bands["red"]) / (bands["nir"] + bands["red"])
            ndre = (bands["nir08"] - bands["rededge1"]) / (bands["nir08"] + bands["rededge1"])
        valid = clear & np.isfinite(ndvi) & np.isfinite(ndre)
        if valid.sum() == 0:
            passes.append(Pass(item.datetime.date(), item.id, 0.0, "no_reading"))
            continue

        ndre_stats = _stats(ndre[valid])
        same_grid = previous_grid is not None and previous_grid == (grid, scl.shape)
        previous = previous_ndre if same_grid else None
        ndvi_stats = _stats(ndvi[valid])
        # Antes de o dossel fechar, o que destoa e solo, nao lavoura.
        mask = anomaly_mask(ndre, valid, ndre_stats.median) if ndvi_stats.median >= CANOPY_NDVI else np.zeros_like(valid)
        zones = _zones(mask, ndre, ndre_stats.median, previous, grid, to_wgs84, field_utm)
        passes.append(Pass(item.datetime.date(), item.id, fraction, "ok", ndvi_stats, ndre_stats, zones))
        previous_ndre = np.where(valid, ndre, np.nan)
        previous_grid = (grid, scl.shape)
    return passes
