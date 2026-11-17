import { useEffect, useState, type ReactNode } from 'react';
import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useLocation, useSearchParams } from 'react-router-dom';
import { Crop } from '@/api/generated/model/crop';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import type { CercosporaRiskViewModel } from '@/api/generated/model/cercosporaRiskViewModel';
import {
  getGetCercosporaAlertsQueryKey, getGetCercosporaRiskQueryKey, useRefreshWeather,
} from '@/api/generated/risk/risk';
import { useCultivations } from '@/features/cultivations/hooks/useCultivations';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { describeRisk, riskChipSx, riskLevelLabel, stageWarning } from '../riskLabels';
import { useCultivationAxes } from '../hooks/useCultivationAxes';
import { AxesOverview, axes, type AxisId } from './AxesOverview';
import { RiskTimeline } from './RiskTimeline';
import { SatellitePanel } from './SatellitePanel';
import { PhotosPanel } from './PhotosPanel';
import { AutonomousWip } from './AutonomousWip';

function label(c: CultivationViewModel) {
  return `${c.cultivar} · plantio em ${cycleDate(c.plantedOn)}${c.endedOn ? '' : ' (em andamento)'}`;
}

/**
 * A situacao do talhao: os tres eixos da deteccao de cercosporiose e o que fazer com eles.
 *
 * E a aba que abre primeiro. O topo junta clima, satelite e fotos numa decisao com o botao da acao
 * ("Vistoriar agora", "Revisar fotos"). Embaixo, uma secao por eixo, numerada na mesma ordem dos
 * cartoes, para quem quer ver o porque.
 */
export function RiskSection({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  const [, setParams] = useSearchParams();
  const { cultivations, isLoading, error, refetch } = useCultivations(farmId, fieldId);
  const corn = cultivations.filter(c => c.crop === Crop.Corn);
  const [chosen, setChosen] = useState<string>('');
  const selected = corn.find(c => c.id === chosen) ?? corn.find(c => c.endedOn === null) ?? corn[0];

  return (
    <QueryBoundary isLoading={isLoading} error={error} onRetry={refetch}>
      {!selected ? (
        <Alert severity="info"
          action={<Button color="inherit" size="small"
            onClick={() => setParams({ aba: 'plantio', registrar: 'plantio' })}>Registrar plantio</Button>}>
          Registre o plantio de milho deste talhão para o sistema acompanhar o risco de cercosporiose.
        </Alert>
      ) : (
        <Stack spacing={2}>
          {corn.length > 1 && (
            <TextField select size="small" label="Ver outro plantio" sx={{ maxWidth: 420 }}
              value={selected.id} onChange={event => setChosen(event.target.value)}>
              {corn.map(c => <MenuItem key={c.id} value={c.id}>{label(c)}</MenuItem>)}
            </TextField>
          )}
          <CultivationSituation key={selected.id} farmId={farmId} cultivation={selected} />
        </Stack>
      )}
    </QueryBoundary>
  );
}

function scrollTo(axis: AxisId) {
  document.getElementById(`eixo-${axis}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function CultivationSituation({ farmId, cultivation }: { farmId: string; cultivation: CultivationViewModel }) {
  const [, setParams] = useSearchParams();
  const { hash } = useLocation();
  const data = useCultivationAxes(cultivation.id, { endedOn: cultivation.endedOn });
  const closed = cultivation.endedOn !== null;
  const [openedPhoto, setOpenedPhoto] = useState<string | null>(null);

  // Link vindo da tela Hoje ("Revisar fotos", ou um eixo): rola ate a secao certa depois de carregar.
  const target = hash.replace('#', '') as AxisId | '';
  useEffect(() => {
    if (!target || data.isLoading) return;
    // A segunda rolagem corrige o salto do mapa do satelite, que termina de montar depois.
    const timers = [50, 900].map(delay => window.setTimeout(() => scrollTo(target), delay));
    return () => timers.forEach(timer => window.clearTimeout(timer));
  }, [target, data.isLoading]);

  function review() {
    const next = data.photos.awaitingReview[0];
    scrollTo('fotos');
    if (next) setOpenedPhoto(next.id);
  }

  return (
    <QueryBoundary isLoading={data.isLoading} error={data.error} onRetry={data.refetch}>
      <Stack spacing={3}>
        <AxesOverview
          farmId={farmId}
          cultivationId={cultivation.id}
          verdict={data.verdict}
          closed={closed}
          summaries={{ clima: data.climate, satelite: data.satelliteSummary, fotos: data.photoSummary }}
          onAxis={scrollTo}
          onReview={review}
          onStage={() => setParams({ aba: 'plantio', registrar: 'estagio' })}
          reviewCount={data.photos.awaitingReview.length}
        />

        <AxisSection id="clima">
          {data.riskFull && <ClimateDetails cultivation={cultivation} risk={data.riskFull} />}
        </AxisSection>

        <AxisSection id="satelite">
          <SatellitePanel cultivation={cultivation} />
        </AxisSection>

        <AxisSection id="fotos">
          <PhotosPanel photos={data.photoList} facts={data.photos}
            openedId={openedPhoto} onOpen={setOpenedPhoto} onClose={() => setOpenedPhoto(null)} />
        </AxisSection>

        {!closed && <AutonomousWip />}
      </Stack>
    </QueryBoundary>
  );
}

const sectionTitles: Record<AxisId, string> = {
  clima: 'O clima favorece a doença?',
  satelite: 'Onde a lavoura destoa do resto do talhão?',
  fotos: 'O que a IA viu nas fotos das folhas?',
};

function AxisSection({ id, children }: { id: AxisId; children: ReactNode }) {
  const axis = axes.find(a => a.id === id)!;
  return (
    <Paper id={`eixo-${id}`} variant="outlined" sx={{ p: 2.5, scrollMarginTop: 16 }}>
      <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', mb: 1.5 }}>
        <Box sx={{
          width: 28, height: 28, borderRadius: '50%', bgcolor: 'primary.main', color: 'primary.contrastText',
          display: 'grid', placeItems: 'center', fontWeight: 700, fontSize: 14, flexShrink: 0,
        }}>{axis.number}</Box>
        <Box>
          <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.2, display: 'block' }}>
            {axis.label} — {axis.question.toLowerCase()}
          </Typography>
          <Typography variant="h6" component="h2" sx={{ lineHeight: 1.25 }}>{sectionTitles[id]}</Typography>
        </Box>
      </Stack>
      {children}
    </Paper>
  );
}

function ClimateDetails({ cultivation, risk }: { cultivation: CultivationViewModel; risk: CercosporaRiskViewModel }) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const [, setParams] = useSearchParams();
  const refresh = useRefreshWeather();
  const closed = cultivation.endedOn !== null;

  async function update() {
    try {
      const result = await refresh.mutateAsync({ id: cultivation.id });
      cache.setQueryData(getGetCercosporaRiskQueryKey(cultivation.id), result);
      await cache.invalidateQueries({ queryKey: getGetCercosporaAlertsQueryKey() });
      notifier.notifySuccess('Clima atualizado.');
    } catch (failure) {
      notifier.notifyError(failure);
    }
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" useFlexGap spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        {risk.current?.level
          ? <Chip label={riskLevelLabel[risk.current.level]} sx={riskChipSx(risk.current.level)} />
          : <Chip label="Sem clima suficiente" />}
        <Typography variant="body2" color="text.secondary">
          {closed ? 'Último dia da safra' : 'Hoje'}{risk.current ? ` · ${cycleDate(risk.current.date)}` : ''}
        </Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button size="small" disabled={refresh.isPending} onClick={() => void update()}>
          {refresh.isPending ? 'Buscando clima…' : closed ? 'Buscar o clima da safra' : 'Atualizar clima'}
        </Button>
      </Stack>

      {risk.current?.level
        ? <Typography variant="body2">{describeRisk(risk.current, risk.windowDays, risk.riskFactors)}</Typography>
        : <Typography color="text.secondary">
            {closed
              ? 'Busque o clima da safra para ver como o risco evoluiu, dia a dia.'
              : 'O clima deste talhão ainda não foi buscado. O sistema atualiza sozinho a cada poucas horas; para ver agora, atualize.'}
          </Typography>}

      {!closed && stageWarning(risk.stage) && <Alert severity="warning"
        action={<Button color="inherit" size="small" onClick={() => setParams({ aba: 'plantio', registrar: 'estagio' })}>Informar estágio</Button>}>
        {stageWarning(risk.stage)}
      </Alert>}

      {risk.days.some(day => day.level) && (
        <Box>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>Risco dia a dia</Typography>
          <RiskTimeline days={risk.days} windowDays={risk.windowDays} />
        </Box>
      )}

      <Typography variant="caption" color="text.secondary">
        Índice de favorabilidade do clima, ainda não calibrado para o Brasil: diz quando as condições favorecem
        a doença, não que ela está na lavoura. Clima do Open-Meteo no centro do talhão
        {risk.weatherRetrievedAt ? `, atualizado em ${new Date(risk.weatherRetrievedAt).toLocaleString('pt-BR')}` : ''}.
      </Typography>
    </Stack>
  );
}
