import { useEffect } from 'react';
import { useMap } from 'react-leaflet';
import type { Layer } from 'leaflet';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import { layerToGeoJsonPolygon } from '../geo/geoJson';

interface DrawControlProps {
  /** Ligado: o proximo clique no mapa comeca o contorno. Desligado: cancela o traçado em curso. */
  isDrawing: boolean;
  onDrawingChange: (isDrawing: boolean) => void;
  onPolygonDrawn: (boundary: GeoJsonPolygon) => void;
}

/**
 * O traçado de contorno do Leaflet-Geoman, comandado de fora do mapa.
 *
 * Antes era a barra do Geoman: um icone de poligono de 30 px no canto do mapa, sem texto, que
 * ninguem achava. Agora quem liga o traçado e um botao com nome na lateral ("Desenhar novo
 * talhao"), e a tela explica o que fazer enquanto ele esta ligado. A barra nao aparece mais.
 *
 * `allowSelfIntersection: false` impede o "nó de gravata" durante o traçado. O backend continua
 * recusando (`geo.invalid_polygon`) — este e apenas o aviso que chega antes, enquanto o dedo ainda
 * esta no mapa.
 */
export function DrawControl({ isDrawing, onDrawingChange, onPolygonDrawn }: DrawControlProps) {
  const map = useMap();

  useEffect(() => {
    map.pm.setGlobalOptions({ allowSelfIntersection: false });

    const handleCreate = ({ layer }: { layer: Layer }) => {
      const boundary = layerToGeoJsonPolygon(layer);

      // A camada temporaria e removida sempre: o talhao so aparece no mapa depois que a API o
      // aceitou e a lista foi recarregada. Mante-la desenhada anunciaria um sucesso que ainda nao
      // aconteceu — e deixaria um contorno fantasma na tela se o cadastro fosse recusado.
      map.removeLayer(layer);

      if (boundary) onPolygonDrawn(boundary);
    };
    const handleEnd = () => onDrawingChange(false);

    map.on('pm:create', handleCreate);
    map.on('pm:drawend', handleEnd);

    return () => {
      map.off('pm:create', handleCreate);
      map.off('pm:drawend', handleEnd);
    };
  }, [map, onPolygonDrawn, onDrawingChange]);

  useEffect(() => {
    const active = map.pm.globalDrawModeEnabled();
    if (isDrawing && !active) {
      map.pm.enableDraw('Polygon', { snappable: true, finishOn: 'dblclick' });
    } else if (!isDrawing && active) {
      map.pm.disableDraw();
    }
  }, [map, isDrawing]);

  // Sair da tela no meio do traçado nao pode deixar o modo ligado no mapa reaproveitado.
  useEffect(() => () => { map.pm.disableDraw(); }, [map]);

  return null;
}
