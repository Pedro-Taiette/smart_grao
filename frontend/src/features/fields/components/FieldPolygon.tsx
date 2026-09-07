import { useEffect, useRef } from 'react';
import { Polygon, Tooltip } from 'react-leaflet';
import type { Polygon as LeafletPolygon } from 'leaflet';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import { formatHectares } from '@/shared/format';
import { cropLabels } from '../cropLabels';
import { layerToGeoJsonPolygon, toLeafletPositions } from '../geo/geoJson';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';

interface FieldPolygonProps {
  field: FieldViewModel;
  isSelected: boolean;
  isEditing: boolean;
  onSelect: (fieldId: string) => void;
  onGeometryChange: (boundary: GeoJsonPolygon) => void;
}

const colors = {
  active: '#2e7d32',
  inactive: '#9e9e9e',
  editing: '#ed6c02',
};

/**
 * Um talhao desenhado no mapa.
 *
 * A edicao de vertices e ligada e desligada aqui, na propria camada, em vez de num controlador
 * central que precisaria manter um registro de refs por talhao. Cada poligono sabe se esta em
 * edicao e avisa o contorno novo a cada arrasto de vertice.
 */
export function FieldPolygon({
  field,
  isSelected,
  isEditing,
  onSelect,
  onGeometryChange,
}: FieldPolygonProps) {
  const layerRef = useRef<LeafletPolygon>(null);

  useEffect(() => {
    const layer = layerRef.current;
    if (!layer) return;

    if (!isEditing) {
      layer.pm.disable();
      return;
    }

    layer.pm.enable({ allowSelfIntersection: false });

    const emitGeometry = () => {
      const boundary = layerToGeoJsonPolygon(layer);
      if (boundary) onGeometryChange(boundary);
    };

    // `pm:edit` cobre o arrasto de vertice; `pm:markerdragend` garante o disparo ao soltar, que e
    // quando o contorno realmente parou de mudar.
    layer.on('pm:edit', emitGeometry);
    layer.on('pm:markerdragend', emitGeometry);

    return () => {
      layer.off('pm:edit', emitGeometry);
      layer.off('pm:markerdragend', emitGeometry);
      layer.pm.disable();
    };
  }, [isEditing, onGeometryChange]);

  const color = isEditing ? colors.editing : field.active ? colors.active : colors.inactive;

  return (
    <Polygon
      ref={layerRef}
      positions={toLeafletPositions(field.boundary)}
      pathOptions={{
        color,
        weight: isSelected ? 4 : 2,
        fillOpacity: isSelected ? 0.35 : 0.18,
        dashArray: field.active ? undefined : '6 6',
      }}
      eventHandlers={{ click: () => onSelect(field.id) }}
    >
      <Tooltip sticky>
        <strong>{field.name}</strong>
        <br />
        {cropLabels[field.crop]} — {formatHectares(field.areaHectares)}
      </Tooltip>
    </Polygon>
  );
}
