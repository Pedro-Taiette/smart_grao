import type { Layer, Polygon as LeafletPolygon } from 'leaflet';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';

/** Par `[latitude, longitude]` — a ordem que o Leaflet usa. */
export type LatLngTuple = [number, number];

/** Centro aproximado do Brasil, para quando nao ha nada melhor sobre o que centralizar o mapa. */
export const BRAZIL_CENTER: LatLngTuple = [-15.78, -52.9];

/**
 * GeoJSON para posicoes do Leaflet.
 *
 * A troca de ordem acontece aqui e em `fromLeafletPositions`, e em nenhum outro lugar: GeoJSON usa
 * `[longitude, latitude]` e o Leaflet usa `[latitude, longitude]`. Inverter os dois por engano nao
 * quebra nada visivelmente — o talhao simplesmente aparece do outro lado do planeta.
 */
export function toLeafletPositions(polygon: GeoJsonPolygon): LatLngTuple[] {
  const ring = polygon.coordinates[0] ?? [];
  return ring.map(([longitude, latitude]) => [latitude, longitude] as LatLngTuple);
}

/** Posicoes do Leaflet para GeoJSON, fechando o anel como a RFC 7946 exige. */
export function fromLeafletPositions(positions: LatLngTuple[]): GeoJsonPolygon {
  const ring = positions.map(([latitude, longitude]) => [longitude, latitude]);

  const first = ring[0];
  const last = ring[ring.length - 1];
  if (first && last && (first[0] !== last[0] || first[1] !== last[1])) {
    ring.push([...first]);
  }

  return { type: 'Polygon', coordinates: [ring] };
}

/**
 * Extrai o contorno de uma camada do Leaflet.
 *
 * O `toGeoJSON` do Leaflet ja devolve `[longitude, latitude]` e ja fecha o anel, entao o resultado
 * vai direto para a API. Devolve `null` quando a camada nao e um poligono — o Geoman tambem sabe
 * desenhar linha, circulo e retangulo, e so o poligono vira talhao.
 */
export function layerToGeoJsonPolygon(layer: Layer): GeoJsonPolygon | null {
  const geoJson = (layer as LeafletPolygon).toGeoJSON?.();

  if (!geoJson || geoJson.type !== 'Feature' || geoJson.geometry?.type !== 'Polygon') {
    return null;
  }

  return {
    type: 'Polygon',
    coordinates: geoJson.geometry.coordinates as number[][][],
  };
}
