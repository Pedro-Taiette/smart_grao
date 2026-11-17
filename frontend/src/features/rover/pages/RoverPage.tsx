import { useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Button, Chip, FormControlLabel, LinearProgress, Paper, Stack, Switch, Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import ReplayIcon from '@mui/icons-material/Replay';
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera';
import { useNavigate, useParams } from 'react-router-dom';
import { MapContainer, Marker, Polygon, Polyline, TileLayer } from 'react-leaflet';
import { divIcon, latLngBounds } from 'leaflet';
import { useGetFieldById } from '@/api/generated/fields/fields';
import { useGetPhotos } from '@/api/generated/photos/photos';
import { useGetSatellitePasses } from '@/api/generated/risk/risk';
import { LeafLabel } from '@/api/generated/model/leafLabel';
import type { PhotoViewModel } from '@/api/generated/model/photoViewModel';
import type { AnomalyZoneViewModel } from '@/api/generated/model/anomalyZoneViewModel';
import { apiBaseUrl } from '@/api/httpClient';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { useSyncFarmFromRecord } from '@/features/farms/useFarmContext';
import { toLeafletPositions } from '@/features/fields/geo/geoJson';
import { today } from '@/features/cultivations/cultivationSchema';
import { latestModel, leafLabelName, modelVerdict, percent } from '@/features/analysis/analysisLabels';
import { satelliteFacts } from '@/features/risk/axes';
import { PageContainer } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { planRoute, positionAt, type LatLng, type RoverRoute } from '../route';
import { useRoverSimulation, type Capture, type RoverPhase, type Sample } from '../useRoverSimulation';

const zoneColor = '#f2c200';

const datasetToLeaf: Record<Sample['datasetLabel'], LeafLabel> = {
  cercosporiose: LeafLabel.Cercosporiose,
  saudavel: LeafLabel.Healthy,
  outras_manchas: LeafLabel.OtherSpots,
};

const stopClass: Record<LeafLabel, string> = {
  [LeafLabel.Cercosporiose]: 'cercosporiose',
  [LeafLabel.OtherSpots]: 'other',
  [LeafLabel.Healthy]: 'healthy',
};

function useSamples() {
  const [samples, setSamples] = useState<Sample[]>([]);
  useEffect(() => {
    let alive = true;
    fetch('/rover-samples/samples.json')
      .then(response => response.json() as Promise<Sample[]>)
      .then(items => { if (alive) setSamples(items); })
      .catch(() => { if (alive) setSamples([]); });
    return () => { alive = false; };
  }, []);
  return samples;
}

/**
 * O rover autonomo, simulado: anda pelo talhao ate as areas que o satelite marcou, fotografa as
 * folhas de baixo e manda para a IA — sem ninguem ir a campo.
 *
 * E um modulo a parte, aberto a partir do talhao. Mostra o pilar 3 no papel em que ele faz mais
 * sentido: a IA como o olho de uma maquina, e nao como segunda opiniao de quem ja esta olhando a
 * folha. Ver `docs/modulo-coleta-autonoma.md`.
 */
export function RoverPage() {
  const { fieldId = '' } = useParams();
  const navigate = useNavigate();
  const fieldQuery = useGetFieldById(fieldId, { query: { enabled: Boolean(fieldId) } });
  const field = fieldQuery.data;
  useSyncFarmFromRecord(field?.farmId);
  const cultivations = useFarmCultivations(field?.farmId ?? '');
  const cultivation = cultivations.openFor(fieldId);
  const satelliteQuery = useGetSatellitePasses(cultivation?.id ?? '', { query: { enabled: Boolean(cultivation) } });
  const samples = useSamples();

  const zonesPass = useMemo(
    () => satelliteQuery.data ? satelliteFacts(satelliteQuery.data.passes, today()).recentZonesPass : null,
    [satelliteQuery.data]);
  const route = useMemo(
    () => field && satelliteQuery.data ? planRoute(field.boundary, zonesPass?.zones ?? []) : null,
    [field, satelliteQuery.data, zonesPass]);

  return (
    <PageContainer>
      <Button startIcon={<ArrowBackIcon />} sx={{ mb: 1, ml: -1 }} onClick={() => navigate(`/talhoes/${fieldId}`)}>
        {field?.name ?? 'Talhão'}
      </Button>

      <Stack direction="row" sx={{ alignItems: 'center', gap: 1, flexWrap: 'wrap', mb: 0.5 }}>
        <Typography variant="h5" component="h1">Rover autônomo</Typography>
        <Chip size="small" color="warning" label="Simulação" />
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        O rover sai da base, vai até as áreas que o satélite marcou, fotografa as folhas de baixo — onde a
        cercosporiose começa — e manda as fotos para a IA. Ninguém precisa ir a campo.
      </Typography>

      <Alert severity="info" sx={{ mb: 2 }}>
        <strong>Sem hardware e sem lavoura neste semestre</strong>, o trajeto é simulado e as fotos são exemplos do
        conjunto público de teste (milho de Bangladesh, CC BY 4.0) — não desta lavoura. A análise é a do modelo de IA de
        verdade, pela mesma fila das fotos de campo. As fotos ficam no talhão marcadas como <em>rover · simulada</em>.
      </Alert>

      <QueryBoundary isLoading={fieldQuery.isPending || cultivations.isLoading || (Boolean(cultivation) && satelliteQuery.isPending)}
        error={fieldQuery.error ?? cultivations.error ?? satelliteQuery.error}
        onRetry={() => { void fieldQuery.refetch(); cultivations.refetch(); void satelliteQuery.refetch(); }}>
        {!cultivation && <Alert severity="warning">Registre o plantio deste talhão para o rover ter o que vistoriar.</Alert>}
        {cultivation && field && route && (
          <Simulation key={route.totalMeters} route={route} cultivationId={cultivation.id} fieldId={fieldId}
            boundary={toLeafletPositions(field.boundary) as LatLng[]} zones={zonesPass?.zones ?? []} samples={samples} />
        )}
      </QueryBoundary>
    </PageContainer>
  );
}

function Simulation({ route, cultivationId, fieldId, boundary, zones, samples }: {
  route: RoverRoute;
  cultivationId: string;
  fieldId: string;
  boundary: LatLng[];
  zones: AnomalyZoneViewModel[];
  samples: Sample[];
}) {
  const navigate = useNavigate();
  const sim = useRoverSimulation(route, cultivationId, samples);
  // Consulta as fotos a cada 2 s enquanto alguma ainda espera o envio ou o resultado da IA.
  const photosQuery = useGetPhotos({ cultivationId }, {
    query: {
      refetchInterval: query => {
        const data = query.state.data ?? [];
        return sim.captures.some(c => {
          if (c.status === 'sending') return true;
          if (c.status === 'failed') return false;
          const photo = data.find(p => p.id === c.photoId);
          return !photo || (photo.analysisStatus !== 'Failed' && !latestModel(photo.assessments));
        }) ? 2000 : false;
      },
    },
  });
  const photos = useMemo(() => new Map((photosQuery.data ?? []).map(p => [p.id, p])), [photosQuery.data]);
  const bounds = useMemo(() => latLngBounds(boundary), [boundary]);

  const resultFor = (capture: Capture | undefined) => capture ? latestModel(photos.get(capture.photoId)?.assessments) : null;
  const last = sim.captures[sim.captures.length - 1];
  const traveledPath = useMemo(() => {
    const at = positionAt(route, sim.traveled);
    return [...route.path.slice(0, at.segment), at.position];
  }, [route, sim.traveled]);

  const analyzed = sim.captures.filter(c => resultFor(c));
  const suspicious = analyzed.filter(c => resultFor(c)?.label === LeafLabel.Cercosporiose).length;
  const agreed = analyzed.filter(c => resultFor(c)?.label === datasetToLeaf[c.sample.datasetLabel]).length;

  return (
    <Stack spacing={2}>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: '3fr 2fr' } }}>
        <Box sx={{ height: { xs: 360, md: 520 }, borderRadius: 2, overflow: 'hidden', border: 1, borderColor: 'divider' }}>
          <MapContainer bounds={bounds} boundsOptions={{ padding: [30, 30] }} style={{ height: '100%' }}>
            <TileLayer attribution="Imagem &copy; Esri"
              url="https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}" maxZoom={19} />
            <Polygon positions={boundary} pathOptions={{ color: '#ffffff', weight: 2, fillOpacity: 0 }} />
            {zones.map((zone, index) => (
              <Polygon key={index} positions={toLeafletPositions(zone.geometry)}
                pathOptions={{ color: zoneColor, weight: 2, fillColor: zoneColor, fillOpacity: 0.3 }} />
            ))}
            <Polyline positions={route.path} pathOptions={{ color: '#ffffff', weight: 3, opacity: 0.7, dashArray: '6 8' }} />
            {sim.phase !== 'ready' && <Polyline positions={traveledPath} pathOptions={{ color: '#2e7d32', weight: 5 }} />}
            <Marker position={route.base} icon={divIcon({ className: '', iconSize: [18, 18], iconAnchor: [9, 9], html: '<div class="rover-base"></div>' })} />
            {route.stops.map(stop => {
              const capture = sim.captures.find(c => c.stopSequence === stop.sequence);
              const result = resultFor(capture);
              const css = result ? stopClass[result.label] : capture ? 'pending' : '';
              return (
                <Marker key={stop.sequence} position={stop.position}
                  icon={divIcon({ className: '', iconSize: [26, 26], iconAnchor: [13, 13], html: `<div class="rover-stop ${css}">${stop.sequence}</div>` })} />
              );
            })}
            {sim.position && (
              <Marker position={sim.position} zIndexOffset={1000}
                icon={divIcon({
                  className: '', iconSize: [40, 40], iconAnchor: [20, 20],
                  html: `<div class="rover-marker${sim.phase === 'capturing' ? ' capturing' : ''}"><div class="rover-pulse"></div>${roverSvg(sim.heading)}</div>`,
                })} />
            )}
          </MapContainer>
        </Box>

        <Stack spacing={2}>
          <StatusCard sim={sim} route={route} onReview={() => navigate(`/talhoes/${fieldId}#fotos`)}
            suspicious={suspicious} analyzed={analyzed.length} agreed={agreed} />
          <CameraCard capture={last} photo={last ? photos.get(last.photoId) : undefined} capturing={sim.phase === 'capturing'} />
        </Stack>
      </Box>

      <Legend />
      <HowItWorks phase={sim.phase} hasResult={analyzed.length > 0} />

      {sim.captures.length > 0 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>Paradas do rover</Typography>
          <Stack spacing={1}>
            {sim.captures.map(capture => (
              <CaptureRow key={capture.photoId} capture={capture} photo={photos.get(capture.photoId)}
                fromZone={route.stops[capture.stopSequence - 1]?.fromZone ?? false} />
            ))}
          </Stack>
        </Paper>
      )}
    </Stack>
  );
}

/** Um rover visto de cima: corpo, quatro rodas e a camera na frente. Gira com o rumo. */
function roverSvg(heading: number) {
  return `<svg width="28" height="28" viewBox="0 0 28 28" style="transform: rotate(${heading}deg)">
    <rect x="3" y="5" width="5" height="7" rx="1.5" fill="#263238"/><rect x="20" y="5" width="5" height="7" rx="1.5" fill="#263238"/>
    <rect x="3" y="16" width="5" height="7" rx="1.5" fill="#263238"/><rect x="20" y="16" width="5" height="7" rx="1.5" fill="#263238"/>
    <rect x="7" y="4" width="14" height="20" rx="4" fill="#2e7d32" stroke="#fff" stroke-width="1.5"/>
    <circle cx="14" cy="8" r="2.6" fill="#fff"/><circle cx="14" cy="8" r="1.2" fill="#263238"/>
  </svg>`;
}

function phaseText(phase: RoverPhase, target: RoverRoute['stops'][number] | null, hasZones: boolean) {
  switch (phase) {
    case 'ready': return { title: 'Pronto para sair da base', detail: 'A rota já está traçada até as paradas.' };
    case 'driving': return {
      title: `A caminho da parada ${target?.sequence ?? ''}`,
      detail: target?.fromZone ? 'Indo para uma área que o satélite marcou.'
        : hasZones ? 'Indo para um ponto extra, para cobrir o talhão além das áreas marcadas.'
        : 'Indo para um ponto espalhado pelo talhão (o satélite não marcou área recente).',
    };
    case 'capturing': return { title: `Parado na parada ${target?.sequence ?? ''}`, detail: 'Fotografando as folhas de baixo…' };
    case 'returning': return { title: 'Voltando para a base', detail: 'Todas as paradas feitas.' };
    case 'done': return { title: 'Rota concluída', detail: 'O rover voltou para a base.' };
  }
}

function StatusCard({ sim, route, onReview, suspicious, analyzed, agreed }: {
  sim: ReturnType<typeof useRoverSimulation>;
  route: RoverRoute;
  onReview: () => void;
  suspicious: number;
  analyzed: number;
  agreed: number;
}) {
  const text = phaseText(sim.phase, sim.target, route.stops.some(stop => stop.fromZone));
  const moving = sim.phase === 'driving' || sim.phase === 'returning';
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="overline" color="text.secondary">Agora</Typography>
      <Typography variant="h6" sx={{ lineHeight: 1.25 }}>{text.title}</Typography>
      <Typography variant="body2" color="text.secondary">{text.detail}</Typography>
      {moving && (
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {Math.round(sim.remainingToTarget)} m · chega em {Math.max(1, Math.round(sim.secondsToTarget))} s
          <Typography component="span" variant="caption" color="text.secondary"> (simulação acelerada)</Typography>
        </Typography>
      )}
      <LinearProgress variant="determinate" value={(sim.traveled / route.totalMeters) * 100} sx={{ my: 1.5, height: 6, borderRadius: 3 }} />
      <Typography variant="body2" color="text.secondary">
        Paradas: {sim.stopsDone} de {route.stops.length} · rota de {Math.round(route.totalMeters)} m
      </Typography>

      {sim.phase === 'done' && (
        <Alert severity={suspicious > 0 ? 'warning' : 'success'} sx={{ mt: 1.5 }}>
          {analyzed < sim.captures.length
            ? `A IA ainda está analisando ${sim.captures.length - analyzed} foto(s)…`
            : suspicious > 0
              ? `A IA viu sinal de cercosporiose em ${suspicious} de ${analyzed} foto(s). Elas já estão no talhão para revisar.`
              : `Nenhuma das ${analyzed} foto(s) teve sinal de cercosporiose.`}
          {analyzed > 0 && ` A IA concordou com o gabarito do conjunto em ${agreed} de ${analyzed}.`}
        </Alert>
      )}

      <Stack direction="row" useFlexGap spacing={1} sx={{ mt: 1.5, flexWrap: 'wrap' }}>
        {(sim.phase === 'ready' || sim.phase === 'done') && (
          <Button variant="contained" startIcon={sim.phase === 'done' ? <ReplayIcon /> : <PlayArrowIcon />} onClick={sim.start}>
            {sim.phase === 'done' ? 'Rodar de novo' : 'Iniciar rota'}
          </Button>
        )}
        {sim.phase !== 'ready' && sim.phase !== 'done' && (
          <Button variant="outlined" startIcon={sim.paused ? <PlayArrowIcon /> : <PauseIcon />} onClick={sim.togglePause}>
            {sim.paused ? 'Continuar' : 'Pausar'}
          </Button>
        )}
        {sim.phase === 'done' && analyzed > 0 && <Button variant="outlined" onClick={onReview}>Revisar fotos no talhão</Button>}
      </Stack>
    </Paper>
  );
}

function CameraCard({ capture, photo, capturing }: { capture?: Capture; photo?: PhotoViewModel; capturing: boolean }) {
  const [heatmap, setHeatmap] = useState(false);
  const model = latestModel(photo?.assessments);
  const src = heatmap && model?.hasHeatmap ? `${apiBaseUrl}/api/assessments/${model.id}/heatmap` : capture ? `/rover-samples/${capture.sample.file}` : null;

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
        <PhotoCameraIcon fontSize="small" color="action" />
        <Typography variant="subtitle2">O que o rover vê</Typography>
      </Stack>
      <Box sx={{ position: 'relative', height: 200, borderRadius: 1, overflow: 'hidden', bgcolor: 'grey.900', display: 'grid', placeItems: 'center' }}>
        {src
          ? <Box component="img" src={src} alt="Folha fotografada pelo rover" sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
          : <Typography variant="body2" sx={{ color: 'grey.500' }}>A câmera liga na primeira parada.</Typography>}
        {capturing && <Box key={capture?.photoId} sx={{ position: 'absolute', inset: 0, bgcolor: '#fff', animation: 'rover-flash 0.6s ease-out forwards' }} />}
      </Box>
      {capture && (
        <Stack spacing={0.5} sx={{ mt: 1 }}>
          <Typography variant="body2">
            <strong>IA:</strong>{' '}
            {capture.status === 'failed' ? 'não deu para enviar a foto'
              : model ? `${modelVerdict[model.label]} (${percent(model.confidence)})`
              : capture.status === 'sending' ? 'enviando a foto…' : 'analisando…'}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Gabarito do conjunto: {leafLabelName[datasetToLeaf[capture.sample.datasetLabel]]}
            {model && (model.label === datasetToLeaf[capture.sample.datasetLabel] ? ' · a IA acertou' : ' · a IA errou')}
          </Typography>
          {model?.hasHeatmap && (
            <FormControlLabel control={<Switch size="small" checked={heatmap} onChange={(_, v) => setHeatmap(v)} />}
              label={<Typography variant="body2">Mostrar onde a IA olhou</Typography>} />
          )}
        </Stack>
      )}
    </Paper>
  );
}

function CaptureRow({ capture, photo, fromZone }: { capture: Capture; photo?: PhotoViewModel; fromZone: boolean }) {
  const model = latestModel(photo?.assessments);
  return (
    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
      <Box component="img" src={`/rover-samples/${capture.sample.file}`} alt="" sx={{ width: 56, height: 42, objectFit: 'cover', borderRadius: 0.5 }} />
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography variant="body2"><strong>Parada {capture.stopSequence}</strong>{fromZone ? ' · área do satélite' : ' · ponto extra'}</Typography>
        <Typography variant="caption" color="text.secondary">
          Gabarito: {leafLabelName[datasetToLeaf[capture.sample.datasetLabel]]}
        </Typography>
      </Box>
      {model
        ? <Chip size="small" color={model.label === LeafLabel.Cercosporiose ? 'error' : model.label === LeafLabel.OtherSpots ? 'warning' : 'success'}
            label={`${leafLabelName[model.label]} ${percent(model.confidence)}`} />
        : <Chip size="small" label={capture.status === 'failed' ? 'Falhou' : 'Analisando…'} />}
    </Stack>
  );
}

function Legend() {
  const item = (color: string, label: string, square = false) => (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Box sx={{ width: 12, height: 12, bgcolor: color, borderRadius: square ? '2px' : '50%', opacity: square ? 0.7 : 1 }} />
      <Typography variant="caption" color="text.secondary">{label}</Typography>
    </Stack>
  );
  return (
    <Stack direction="row" useFlexGap spacing={2} sx={{ flexWrap: 'wrap' }}>
      {item(zoneColor, 'Área que o satélite marcou', true)}
      {item('#757575', 'Parada a fazer')}
      {item('#1565c0', 'Foto na fila da IA')}
      {item('#c62828', 'IA: suspeita de cercosporiose')}
      {item('#ef6c00', 'IA: outra mancha')}
      {item('#2e7d32', 'IA: folha sadia')}
    </Stack>
  );
}

const steps = [
  { title: 'Anda até a parada', detail: 'Pela entrelinha, até as áreas que o satélite marcou.' },
  { title: 'Fotografa as folhas de baixo', detail: 'Onde a cercosporiose começa, sob o dossel.' },
  { title: 'Envia a foto', detail: 'Pela mesma rota da estação, com posição e origem.' },
  { title: 'A IA classifica', detail: 'Cercosporiose, outra mancha ou sadia, com o mapa de onde olhou.' },
  { title: 'Alguém revisa', detail: 'De longe, no eixo 3 do talhão. A revisão calibra o sistema.' },
];

function HowItWorks({ phase, hasResult }: { phase: RoverPhase; hasResult: boolean }) {
  const active = phase === 'driving' || phase === 'returning' ? 0 : phase === 'capturing' ? 1 : hasResult ? 4 : -1;
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="subtitle2" sx={{ mb: 1.5 }}>Como o rover analisa a lavoura</Typography>
      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: 'repeat(5, 1fr)' } }}>
        {steps.map((step, index) => (
          <Box key={step.title} sx={{
            p: 1.5, borderRadius: 1, border: 1,
            borderColor: index === active ? 'primary.main' : 'divider',
            bgcolor: index === active ? 'rgba(46, 125, 50, 0.08)' : 'transparent',
          }}>
            <Typography variant="caption" color="text.secondary">{index + 1}</Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>{step.title}</Typography>
            <Typography variant="caption" color="text.secondary">{step.detail}</Typography>
          </Box>
        ))}
      </Box>
    </Paper>
  );
}
