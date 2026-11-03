import { useMemo, useState } from 'react';
import { Alert, Box, Button, Chip, Paper, Stack, Tooltip, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { MapContainer, Polygon, TileLayer } from 'react-leaflet';
import { latLngBounds } from 'leaflet';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import type { SatellitePassViewModel } from '@/api/generated/model/satellitePassViewModel';
import { SatellitePassStatus } from '@/api/generated/model/satellitePassStatus';
import { useGetFieldById } from '@/api/generated/fields/fields';
import { getGetSatellitePassesQueryKey, useGetSatellitePasses, useRefreshSatellitePasses } from '@/api/generated/risk/risk';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { toLeafletPositions } from '@/features/fields/geo/geoJson';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { useNotifier } from '@/shared/notifications/useNotifier';

const zoneColor = '#f2c200';
const lineColor = '#2a78d6';

function NdreSeries({ passes, selected, onSelect }: {
  passes: SatellitePassViewModel[]; selected: string | null; onSelect: (date: string) => void;
}) {
  const width = 640;
  const height = 120;
  const pad = 8;
  const times = passes.map(p => new Date(p.date).getTime());
  const min = Math.min(...times);
  const span = Math.max(...times) - min || 1;
  const readings = passes.filter(p => p.ndre);
  const values = readings.map(p => p.ndre!.median);
  const low = Math.min(...values, 0);
  const high = Math.max(...values, 0.1);
  const x = (date: string) => pad + ((new Date(date).getTime() - min) / span) * (width - 2 * pad);
  const y = (value: number) => height - 22 - ((value - low) / (high - low || 1)) * (height - 34);
  const path = readings.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(p.date)},${y(p.ndre!.median)}`).join(' ');

  return (
    <svg width="100%" viewBox={`0 0 ${width} ${height}`} style={{ maxWidth: width, display: 'block' }} role="img" aria-label="NDRE mediano do talhão por passagem">
      <line x1={pad} x2={width - pad} y1={height - 22} y2={height - 22} stroke="#d6d4cf" />
      {path && <path d={path} fill="none" stroke={lineColor} strokeWidth={2} />}
      {passes.map(p => p.ndre ? (
        <Tooltip key={p.date} title={`${cycleDate(p.date)} · NDRE mediano ${p.ndre.median.toFixed(2)} · ${p.zones.length} área(s) que destoam`} arrow>
          <circle cx={x(p.date)} cy={y(p.ndre.median)} r={p.date === selected ? 6 : 4}
            fill={p.date === selected ? lineColor : '#fff'} stroke={lineColor} strokeWidth={2}
            style={{ cursor: 'pointer' }} onClick={() => onSelect(p.date)} />
        </Tooltip>
      ) : (
        <Tooltip key={p.date} title={`${cycleDate(p.date)} · sem leitura (nuvem)`} arrow>
          <rect x={x(p.date) - 1.5} y={height - 18} width={3} height={8} fill="#a8a6a0" />
        </Tooltip>
      ))}
    </svg>
  );
}

export function SatellitePanel({ cultivation }: { cultivation: CultivationViewModel }) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const fieldQuery = useGetFieldById(cultivation.fieldId);
  const query = useGetSatellitePasses(cultivation.id);
  const refresh = useRefreshSatellitePasses();
  const passes = useMemo(() => query.data?.passes ?? [], [query.data]);
  const readings = passes.filter(p => p.status === SatellitePassStatus.Ok);
  const [chosen, setChosen] = useState<string | null>(null);
  const selected = readings.find(p => p.date === chosen) ?? readings[readings.length - 1] ?? null;
  const field = fieldQuery.data;
  const bounds = useMemo(() => field ? latLngBounds(toLeafletPositions(field.boundary)) : null, [field]);

  async function update() {
    try {
      const result = await refresh.mutateAsync({ id: cultivation.id });
      cache.setQueryData(getGetSatellitePassesQueryKey(cultivation.id), result);
      notifier.notifySuccess('Imagens de satélite atualizadas.');
    } catch (failure) {
      notifier.notifyError(failure);
    }
  }

  const cloudy = passes.length - readings.length;

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack direction="row" useFlexGap spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <Box>
            <Typography variant="subtitle2">Onde o talhão destoa</Typography>
            <Typography variant="body2" color="text.secondary">
              Sentinel-2, a cada poucos dias. Mostra a área da lavoura que está diferente do resto — não diz que é cercosporiose.
            </Typography>
          </Box>
          <Box sx={{ flexGrow: 1 }} />
          <Button variant="outlined" disabled={refresh.isPending} onClick={() => void update()}>
            {refresh.isPending ? 'Buscando imagens… (pode levar alguns minutos)' : 'Buscar imagens de satélite'}
          </Button>
        </Stack>

        <QueryBoundary isLoading={query.isPending || fieldQuery.isPending} error={query.error ?? fieldQuery.error}
          onRetry={() => { void query.refetch(); void fieldQuery.refetch(); }}>
          {passes.length === 0 ? (
            <Typography color="text.secondary">Nenhuma passagem buscada ainda.</Typography>
          ) : (
            <Stack spacing={2}>
              <Typography variant="body2">
                {passes.length} passagens: {readings.length} com leitura e {cloudy} sem leitura por nuvem ou sombra.
              </Typography>
              <Box>
                <Typography variant="caption" color="text.secondary">
                  NDRE mediano do talhão em cada passagem (traços cinza: sem leitura). Clique num ponto para ver a passagem no mapa.
                </Typography>
                <NdreSeries passes={passes} selected={selected?.date ?? null} onSelect={setChosen} />
              </Box>
              {selected && field && bounds && (
                <>
                  <Stack direction="row" useFlexGap spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                    <Chip label={`Passagem de ${cycleDate(selected.date)}`} />
                    <Typography variant="body2" color="text.secondary">
                      {selected.zones.length === 0
                        ? 'Nenhuma área destoa nesta passagem.'
                        : `${selected.zones.length} área(s) que destoam, ${selected.zones.reduce((sum, z) => sum + z.areaHectares, 0).toFixed(1)} ha no total`}
                      {selected.zones.some(z => z.droppedSincePrevious) ? ' · parte caiu desde a passagem anterior' : ''}
                    </Typography>
                  </Stack>
                  <Box sx={{ height: 360, borderRadius: 1, overflow: 'hidden' }}>
                    <MapContainer key={selected.date} bounds={bounds} boundsOptions={{ padding: [20, 20] }} style={{ height: '100%' }}>
                      <TileLayer
                        attribution="Imagem &copy; Esri"
                        url="https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}"
                        maxZoom={19}
                      />
                      <Polygon positions={toLeafletPositions(field.boundary)} pathOptions={{ color: '#ffffff', weight: 2, fillOpacity: 0 }} />
                      {selected.zones.map((zone, index) => (
                        <Polygon key={index} positions={toLeafletPositions(zone.geometry)}
                          pathOptions={{ color: zoneColor, weight: 2, fillColor: zoneColor, fillOpacity: zone.droppedSincePrevious ? 0.55 : 0.3 }} />
                      ))}
                    </MapContainer>
                  </Box>
                  <Stack direction="row" useFlexGap spacing={2} sx={{ flexWrap: 'wrap' }}>
                    <Typography variant="caption" color="text.secondary">
                      <Box component="span" sx={{ display: 'inline-block', width: 12, height: 12, mr: 0.75, verticalAlign: 'middle', bgcolor: zoneColor, opacity: 0.6 }} />
                      Área com NDRE pelo menos 15% abaixo da mediana do talhão (mais forte: também caiu desde a passagem anterior)
                    </Typography>
                  </Stack>
                </>
              )}
              {readings.length === 0 && <Alert severity="info">Todas as passagens deste período vieram cobertas. É comum na estação chuvosa.</Alert>}
            </Stack>
          )}
        </QueryBoundary>
      </Stack>
    </Paper>
  );
}
