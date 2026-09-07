import { useEffect } from 'react';
import { useMap } from 'react-leaflet';
import type { Layer } from 'leaflet';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import { layerToGeoJsonPolygon } from '../geo/geoJson';

interface DrawControlProps {
  onPolygonDrawn: (boundary: GeoJsonPolygon) => void;
}

/**
 * A barra de desenho do Leaflet-Geoman, reduzida ao que o Pilar 1 usa.
 *
 * So a ferramenta de poligono fica visivel: circulo, retangulo e linha desenham geometrias que a
 * API recusaria, e oferecer um botao que sempre falha e pior do que nao ter o botao.
 *
 * `allowSelfIntersection: false` impede o "nó de gravata" durante o traçado. O backend continua
 * recusando (`geo.invalid_polygon`) — este e apenas o aviso que chega antes, enquanto o dedo ainda
 * esta no mapa.
 */
export function DrawControl({ onPolygonDrawn }: DrawControlProps) {
  const map = useMap();

  useEffect(() => {
    map.pm.addControls({
      position: 'topright',
      drawPolygon: true,
      drawMarker: false,
      drawCircle: false,
      drawCircleMarker: false,
      drawPolyline: false,
      drawRectangle: false,
      drawText: false,
      editMode: false,
      dragMode: false,
      cutPolygon: false,
      removalMode: false,
      rotateMode: false,
    });

    map.pm.setGlobalOptions({ allowSelfIntersection: false });

    const handleCreate = ({ layer }: { layer: Layer }) => {
      const boundary = layerToGeoJsonPolygon(layer);

      // A camada temporaria e removida sempre: o talhao so aparece no mapa depois que a API o
      // aceitou e a lista foi recarregada. Mante-la desenhada anunciaria um sucesso que ainda nao
      // aconteceu — e deixaria um contorno fantasma na tela se o cadastro fosse recusado.
      map.removeLayer(layer);

      if (boundary) onPolygonDrawn(boundary);
    };

    map.on('pm:create', handleCreate);

    return () => {
      map.off('pm:create', handleCreate);
      map.pm.removeControls();
    };
  }, [map, onPolygonDrawn]);

  return null;
}
