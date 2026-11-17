import { useMemo } from 'react';
import { Box, Stack, Typography } from '@mui/material';
import { MapContainer, Marker, Polygon, TileLayer } from 'react-leaflet';
import { divIcon, latLngBounds } from 'leaflet';
import type { AnomalyZoneViewModel } from '@/api/generated/model/anomalyZoneViewModel';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { toLeafletPositions } from '@/features/fields/geo/geoJson';

/** A mesma cor das areas no painel do satelite: quem viu la reconhece aqui. */
const zoneColor = '#f2c200';

/**
 * As paradas da vistoria sobre a imagem do talhao: vermelho falta, verde ja foi.
 *
 * Quando a malha foi dirigida pelo satelite, as areas que destoavam naquela passagem aparecem por
 * baixo das paradas. Sem elas, quem esta no campo ve 10 numeros soltos e nao sabe por que tem que
 * andar ate o canto do talhao; com elas, ve que as paradas estao onde a lavoura estava diferente.
 */
export function StopsMap({ field, points, visited, zones, zoneDate }: {
  field: FieldViewModel;
  points: SamplingPointViewModel[];
  visited: Set<number>;
  zones?: AnomalyZoneViewModel[];
  zoneDate?: string | null;
}) {
  const bounds = useMemo(() => latLngBounds(toLeafletPositions(field.boundary)), [field]);
  const hasZones = (zones?.length ?? 0) > 0;

  return (
    <Stack spacing={0.75}>
      <Box sx={{ height: 320, borderRadius: 1, overflow: 'hidden' }}>
        <MapContainer bounds={bounds} boundsOptions={{ padding: [20, 20] }} style={{ height: '100%' }}>
          <TileLayer attribution="Imagem &copy; Esri"
            url="https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}" maxZoom={19} />
          <Polygon positions={toLeafletPositions(field.boundary)} pathOptions={{ color: '#ffffff', weight: 2, fillOpacity: 0 }} />
          {zones?.map((zone, index) => (
            <Polygon key={index} positions={toLeafletPositions(zone.geometry)}
              pathOptions={{ color: zoneColor, weight: 2, fillColor: zoneColor, fillOpacity: 0.3 }} />
          ))}
          {points.map(point => (
            <Marker key={point.id} position={[point.location.coordinates[1], point.location.coordinates[0]]}
              icon={divIcon({
                className: '', iconSize: [24, 24], iconAnchor: [12, 12],
                html: `<div class="stop-marker${visited.has(point.sequence) ? ' visited' : ''}">${point.sequence}</div>`,
              })} />
          ))}
        </MapContainer>
      </Box>
      <Stack direction="row" useFlexGap spacing={2} sx={{ flexWrap: 'wrap' }}>
        <Legend color="#d32f2f" round label="Parada que falta" />
        <Legend color="#2e7d32" round label="Parada feita" />
        {hasZones && <Legend color={zoneColor} label={`Área que o satélite marcou${zoneDate ? ` (${cycleDate(zoneDate)})` : ''}`} />}
      </Stack>
      {!zoneDate && (
        <Typography variant="caption" color="text.secondary">
          O satélite não tinha área marcada quando a vistoria foi criada, então as paradas estão espalhadas pelo talhão.
        </Typography>
      )}
    </Stack>
  );
}

function Legend({ color, label, round }: { color: string; label: string; round?: boolean }) {
  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Box sx={{ width: 12, height: 12, bgcolor: color, opacity: round ? 1 : 0.6, borderRadius: round ? '50%' : '2px' }} />
      <Typography variant="caption" color="text.secondary">{label}</Typography>
    </Stack>
  );
}
