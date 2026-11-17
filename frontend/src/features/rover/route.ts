import type { AnomalyZoneViewModel } from '@/api/generated/model/anomalyZoneViewModel';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';

/** [latitude, longitude], a ordem do Leaflet. */
export type LatLng = [number, number];

export interface RoverStop {
  sequence: number;
  position: LatLng;
  /** A parada nasceu de uma area que o satelite marcou (ou foi espalhada pelo talhao). */
  fromZone: boolean;
}

export interface RoverRoute {
  base: LatLng;
  stops: RoverStop[];
  /** Base → paradas → base, como uma lista de pontos. */
  path: LatLng[];
  /** Distancia acumulada (m) ate cada ponto de `path`. */
  cumulative: number[];
  totalMeters: number;
}

/** Nesta simulacao o rover para em ate 6 pontos — o bastante para mostrar o ciclo sem cansar a banca. */
export const maxStops = 6;

const earthRadius = 6_371_000;

export function distanceMeters(a: LatLng, b: LatLng) {
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(b[0] - a[0]);
  const dLon = toRad(b[1] - a[1]);
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(a[0])) * Math.cos(toRad(b[0])) * Math.sin(dLon / 2) ** 2;
  return 2 * earthRadius * Math.asin(Math.sqrt(h));
}

/** Rumo em graus a partir do norte, para girar o icone do rover. */
export function bearing(a: LatLng, b: LatLng) {
  const dLon = (b[1] - a[1]) * Math.cos((a[0] * Math.PI) / 180);
  const dLat = b[0] - a[0];
  return (Math.atan2(dLon, dLat) * 180) / Math.PI;
}

function ring(polygon: GeoJsonPolygon): LatLng[] {
  return polygon.coordinates[0].map(([lon, lat]) => [lat, lon] as LatLng);
}

function inside(point: LatLng, polygon: LatLng[]) {
  let result = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const [yi, xi] = polygon[i];
    const [yj, xj] = polygon[j];
    if ((yi > point[0]) !== (yj > point[0]) && point[1] < ((xj - xi) * (point[0] - yi)) / (yj - yi) + xi) result = !result;
  }
  return result;
}

function centroid(points: LatLng[]): LatLng {
  const sum = points.reduce((acc, p) => [acc[0] + p[0], acc[1] + p[1]] as LatLng, [0, 0] as LatLng);
  return [sum[0] / points.length, sum[1] / points.length];
}

/**
 * Um ponto dentro da area. O centro geometrico serve quase sempre, mas numa area em "U" ele cai
 * fora dela — ai vale o ponto de uma grade interna mais perto do centro.
 */
function pointInside(area: LatLng[]): LatLng {
  const center = centroid(area);
  if (inside(center, area)) return center;
  const inner = gridInside(area, 12);
  if (inner.length === 0) return center;
  return inner.reduce((best, p) => distanceMeters(p, center) < distanceMeters(best, center) ? p : best);
}

function gridInside(area: LatLng[], steps: number): LatLng[] {
  const lats = area.map(p => p[0]);
  const lons = area.map(p => p[1]);
  const [minLat, maxLat, minLon, maxLon] = [Math.min(...lats), Math.max(...lats), Math.min(...lons), Math.max(...lons)];
  const points: LatLng[] = [];
  for (let i = 1; i < steps; i++)
    for (let j = 1; j < steps; j++) {
      const point: LatLng = [minLat + ((maxLat - minLat) * i) / steps, minLon + ((maxLon - minLon) * j) / steps];
      if (inside(point, area)) points.push(point);
    }
  return points;
}

/** Pontos espalhados numa grade dentro do talhao, quando o satelite nao marcou nada. */
function spread(boundary: LatLng[], count: number): LatLng[] {
  const candidates = gridInside(boundary, 8);
  if (candidates.length <= count) return candidates;
  const stride = candidates.length / count;
  return Array.from({ length: count }, (_, k) => candidates[Math.floor(k * stride + stride / 2)]);
}

/**
 * Onde o rover para e por onde anda.
 *
 * Mesma ideia da vistoria dirigida: primeiro as areas que o satelite marcou (as maiores antes), uma
 * parada no meio de cada; sem area marcada, paradas espalhadas pelo talhao. A ordem e a do vizinho
 * mais proximo a partir da base — o rover nao cruza o talhao a toa — e ele volta a base no fim.
 */
export function planRoute(boundary: GeoJsonPolygon, zones: AnomalyZoneViewModel[]): RoverRoute {
  const outline = ring(boundary);
  const fromZones = [...zones]
    .sort((a, b) => b.areaHectares - a.areaHectares)
    .slice(0, maxStops)
    .map(zone => pointInside(ring(zone.geometry).slice(0, -1)))
    .filter(point => inside(point, outline));
  const filler = fromZones.length >= 3 ? [] : spread(outline, Math.max(4 - fromZones.length, 0));
  const points = [...fromZones.map(p => ({ p, fromZone: true })), ...filler.map(p => ({ p, fromZone: false }))];

  const base = outline[0];
  const ordered: typeof points = [];
  let current = base;
  const pending = [...points];
  while (pending.length > 0) {
    let best = 0;
    for (let i = 1; i < pending.length; i++)
      if (distanceMeters(current, pending[i].p) < distanceMeters(current, pending[best].p)) best = i;
    const [next] = pending.splice(best, 1);
    ordered.push(next);
    current = next.p;
  }

  const stops = ordered.map((item, index) => ({ sequence: index + 1, position: item.p, fromZone: item.fromZone }));
  const path = [base, ...stops.map(s => s.position), base];
  const cumulative = [0];
  for (let i = 1; i < path.length; i++) cumulative.push(cumulative[i - 1] + distanceMeters(path[i - 1], path[i]));
  return { base, stops, path, cumulative, totalMeters: cumulative[cumulative.length - 1] };
}

/** Posicao e rumo do rover depois de andar `meters` pelo caminho. */
export function positionAt(route: RoverRoute, meters: number): { position: LatLng; heading: number; segment: number } {
  const d = Math.max(0, Math.min(meters, route.totalMeters));
  let segment = 1;
  while (segment < route.cumulative.length - 1 && route.cumulative[segment] < d) segment++;
  const from = route.path[segment - 1];
  const to = route.path[segment];
  const length = route.cumulative[segment] - route.cumulative[segment - 1] || 1;
  const t = (d - route.cumulative[segment - 1]) / length;
  return {
    position: [from[0] + (to[0] - from[0]) * t, from[1] + (to[1] - from[1]) * t],
    heading: bearing(from, to),
    segment,
  };
}
