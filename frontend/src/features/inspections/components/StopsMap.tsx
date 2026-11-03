import { useMemo } from 'react';
import { Box } from '@mui/material';
import { MapContainer, Marker, Polygon, TileLayer } from 'react-leaflet';
import { divIcon, latLngBounds } from 'leaflet';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';
import { toLeafletPositions } from '@/features/fields/geo/geoJson';

/** As paradas da vistoria sobre a imagem do talhão: vermelho falta, verde já foi. */
export function StopsMap({ field, points, visited }: {
  field: FieldViewModel; points: SamplingPointViewModel[]; visited: Set<number>;
}) {
  const bounds = useMemo(() => latLngBounds(toLeafletPositions(field.boundary)), [field]);
  return (
    <Box sx={{ height: 320, borderRadius: 1, overflow: 'hidden' }}>
      <MapContainer bounds={bounds} boundsOptions={{ padding: [20, 20] }} style={{ height: '100%' }}>
        <TileLayer attribution="Imagem &copy; Esri"
          url="https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}" maxZoom={19} />
        <Polygon positions={toLeafletPositions(field.boundary)} pathOptions={{ color: '#ffffff', weight: 2, fillOpacity: 0 }} />
        {points.map(point => (
          <Marker key={point.id} position={[point.location.coordinates[1], point.location.coordinates[0]]}
            icon={divIcon({
              className: '', iconSize: [24, 24], iconAnchor: [12, 12],
              html: `<div class="stop-marker${visited.has(point.sequence) ? ' visited' : ''}">${point.sequence}</div>`,
            })} />
        ))}
      </MapContainer>
    </Box>
  );
}
