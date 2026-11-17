import { useState } from 'react';
import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { Crop } from '@/api/generated/model/crop';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import {
  getGetCercosporaAlertsQueryKey, getGetCercosporaRiskQueryKey, useGetCercosporaRisk, useRefreshWeather,
} from '@/api/generated/risk/risk';
import { useCultivations } from '@/features/cultivations/hooks/useCultivations';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { describeRisk, riskAdvice, riskChipSx, riskLevelLabel, stageWarning } from '../riskLabels';
import { RiskTimeline } from './RiskTimeline';
import { SatellitePanel } from './SatellitePanel';
import { StationPanel } from './StationPanel';

function label(c: CultivationViewModel) {
  return `${c.cultivar} · plantio em ${cycleDate(c.plantedOn)}${c.endedOn ? '' : ' (em andamento)'}`;
}

/**
 * A situacao do talhao: o risco de cercosporiose hoje e o que fazer com ele.
 *
 * E a aba que abre primeiro. O topo responde em uma frase "preciso fazer alguma coisa?", com o
 * botao da acao ao lado; clima dia a dia, satelite e estacao vem embaixo, para quem quer o porque.
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
          <CultivationRisk key={selected.id} cultivation={selected} />
        </Stack>
      )}
    </QueryBoundary>
  );
}

function CultivationRisk({ cultivation }: { cultivation: CultivationViewModel }) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const [, setParams] = useSearchParams();
  const query = useGetCercosporaRisk(cultivation.id);
  const refresh = useRefreshWeather();
  const risk = query.data;
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

  const refreshButton = (
    <Button size="small" disabled={refresh.isPending} onClick={() => void update()}>
      {refresh.isPending ? 'Buscando clima…' : closed ? 'Buscar o clima da safra' : 'Atualizar clima'}
    </Button>
  );

  return (
    <QueryBoundary isLoading={query.isPending} error={query.error} onRetry={() => void query.refetch()}>
      {risk && (
        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack spacing={1.5}>
              <Stack direction="row" useFlexGap spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                {risk.current?.level ? (
                  <Chip label={riskLevelLabel[risk.current.level]}
                    sx={riskChipSx(risk.current.level)} />
                ) : (
                  <Chip label="Sem clima suficiente" />
                )}
                <Typography variant="body2" color="text.secondary">
                  {closed ? 'Último dia da safra' : 'Hoje'}{risk.current ? ` · ${cycleDate(risk.current.date)}` : ''}
                </Typography>
                <Box sx={{ flexGrow: 1 }} />
                {refreshButton}
              </Stack>

              {risk.current?.level && (
                <Box>
                  {!closed && <Typography variant="h6" component="p">
                    {riskAdvice(risk.current.level, risk.suggestsInspection)}
                  </Typography>}
                  <Typography variant="body2" color="text.secondary">
                    {describeRisk(risk.current, risk.windowDays, risk.riskFactors)}
                  </Typography>
                </Box>
              )}
              {!risk.current && (
                <Typography color="text.secondary">
                  {closed
                    ? 'Busque o clima da safra para ver como o risco evoluiu, dia a dia.'
                    : 'O clima deste talhão ainda não foi buscado. O sistema atualiza sozinho a cada poucas horas; para ver agora, atualize.'}
                </Typography>
              )}

              {risk.suggestsInspection && !closed && (
                <Alert severity="error"
                  action={<Button variant="contained" color="error" onClick={() => setParams({ aba: 'vistorias', agendar: 'cercosporiose' })}>Agendar vistoria</Button>}>
                  As condições das últimas duas semanas favorecem a infecção, e a mancha aparece de 14 a 21 dias
                  depois dela.
                </Alert>
              )}

              {!closed && stageWarning(risk.stage) && <Alert severity="warning"
                action={<Button color="inherit" size="small" onClick={() => setParams({ aba: 'plantio', registrar: 'estagio' })}>Informar estágio</Button>}>
                {stageWarning(risk.stage)}
              </Alert>}
            </Stack>
          </Paper>

          {risk.days.some(day => day.level) && (
            <Paper variant="outlined" sx={{ p: 2.5 }}>
              <Typography variant="subtitle2" sx={{ mb: 1.5 }}>Risco dia a dia</Typography>
              <RiskTimeline days={risk.days} windowDays={risk.windowDays} />
            </Paper>
          )}

          <SatellitePanel cultivation={cultivation} />

          <StationPanel cultivationId={cultivation.id} />

          <Typography variant="caption" color="text.secondary">
            Índice de favorabilidade do clima, ainda não calibrado para o Brasil: diz quando as condições favorecem
            a doença, não que ela está na lavoura. Clima do Open-Meteo no centro do talhão
            {risk.weatherRetrievedAt ? `, atualizado em ${new Date(risk.weatherRetrievedAt).toLocaleString('pt-BR')}` : ''}.
          </Typography>
        </Stack>
      )}
    </QueryBoundary>
  );
}
