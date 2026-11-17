import { useEffect, useRef } from 'react';
import { Alert, Box, Button, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useSearchParams } from 'react-router-dom';
import { Crop } from '@/api/generated/model/crop';
import { CropSequence } from '@/api/generated/model/cropSequence';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { cropLabels } from '@/features/fields/cropLabels';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { CultivationDialog, StageDialog, ClosureDialog, RiskFactorsDialog } from './CultivationDialogs';
import { riskFactorsSummary, unknownRiskFactors } from '../riskFactorLabels';
import { cycleDate } from '../cultivationSchema';
import { useCultivationWorkspace } from '../hooks/useCultivationWorkspace';

/**
 * O plantio de um talhao: o que esta plantado, desde quando e em que estagio.
 *
 * A safra deixou de ser um cadastro a parte (sai da data do plantio), entao a tela tem uma unica
 * pergunta por vez: "o que esta plantado aqui?" — e, se nada, um botao grande para responder.
 *
 * Outras telas abrem um formulario daqui pela URL (`?registrar=estagio`): o aviso "registre o
 * estagio" da aba de situacao leva direto ao formulario, sem o clique intermediario.
 */
export function CultivationSection({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  // Uma chave por talhao monta o estado dos formularios do zero: um dialogo aberto nao pode
  // sobreviver a uma troca de talhao e salvar no lugar errado.
  return <Workspace key={fieldId} farmId={farmId} fieldId={fieldId} />;
}

function Workspace({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  const w = useCultivationWorkspace(farmId, fieldId);
  const [params, setParams] = useSearchParams();
  const common = { isSaving: w.isSaving, onClose: w.closeDialog };
  const current = w.cultivations.find(c => c.endedOn === null);
  const past = w.cultivations.filter(c => c.endedOn !== null);
  const previous = w.cultivations.reduce<CultivationViewModel | undefined>(
    (latest, c) => (!latest || c.plantedOn > latest.plantedOn ? c : latest), undefined);
  const newCycleRisk = previous
    ? { ...unknownRiskFactors, cropSequence: previous.crop === Crop.Corn ? CropSequence.CornAfterCorn : CropSequence.Rotation }
    : unknownRiskFactors;
  const canPlant = !current && Boolean(w.field?.active);

  // Pedido vindo de outra tela: abre o formulario uma vez e limpa a URL, para o F5 nao reabrir.
  const handled = useRef(false);
  const request = params.get('registrar');
  const { openDialog } = w;
  useEffect(() => {
    if (handled.current || w.isLoading || !request) return;
    handled.current = true;
    if (request === 'estagio' && current) openDialog({ kind: 'stage', cultivation: current });
    if (request === 'plantio' && canPlant) openDialog({ kind: 'cultivation' });
    const next = new URLSearchParams(params);
    next.delete('registrar');
    setParams(next, { replace: true });
  }, [request, w.isLoading, current, canPlant, openDialog, params, setParams]);

  return <>
    <QueryBoundary isLoading={w.isLoading} error={w.error} onRetry={w.refetch}>
      <Stack spacing={2.5}>
        {!w.field?.active && <Alert severity="info">
          Este talhão está fora de operação. Reative-o no mapa de talhões para registrar um plantio.
        </Alert>}

        {current
          ? <CurrentPlanting cultivation={current} isSaving={w.isSaving} openDialog={w.openDialog} />
          : canPlant && <Paper variant="outlined">
              <EmptyState
                title="Nada plantado neste talhão"
                description="Registre o plantio de milho para o sistema começar a acompanhar o risco de cercosporiose."
                action={<Button variant="contained" size="large" startIcon={<AddIcon />} disabled={w.isSaving}
                  onClick={() => w.openDialog({ kind: 'cultivation' })}>Registrar plantio</Button>}
              />
            </Paper>}

        {past.length > 0 && <Box>
          <Typography variant="h6" sx={{ mb: 1 }}>Plantios anteriores</Typography>
          <Stack spacing={1}>
            {past.map(c => <Paper key={c.id} variant="outlined" sx={{ p: 2 }}>
              <Typography variant="subtitle1">{cropLabels[c.crop]} · {c.cultivar}</Typography>
              <Typography variant="body2" color="text.secondary">
                Safra {w.seasons.find(s => s.id === c.seasonId)?.name ?? '—'} · plantado em {cycleDate(c.plantedOn)}
                {c.endedOn ? ` · encerrado em ${cycleDate(c.endedOn)}` : ''}
              </Typography>
            </Paper>)}
          </Stack>
        </Box>}
      </Stack>
    </QueryBoundary>

    {w.dialog?.kind === 'cultivation' && <CultivationDialog {...common} riskFactors={newCycleRisk} onSave={w.createCultivation} />}
    {w.dialog?.kind === 'risk' && <RiskFactorsDialog {...common} riskFactors={w.dialog.cultivation.riskFactors}
      onSave={values => w.dialog?.kind === 'risk'
        ? w.updateRiskFactors(w.dialog.cultivation.id, values)
        : Promise.resolve(false)} />}
    {w.dialog?.kind === 'stage' && <StageDialog {...common} cultivation={w.dialog.cultivation}
      onSave={values => w.dialog?.kind === 'stage'
        ? w.recordStage(w.dialog.cultivation.id, { ...values, notes: values.notes || null })
        : Promise.resolve(false)} />}
    {w.dialog?.kind === 'closure' && <ClosureDialog {...common} cultivation={w.dialog.cultivation}
      onSave={date => w.dialog?.kind === 'closure'
        ? w.closeCultivation(w.dialog.cultivation.id, date)
        : Promise.resolve(false)} />}
  </>;
}

type OpenDialog = ReturnType<typeof useCultivationWorkspace>['openDialog'];

function CurrentPlanting({ cultivation: c, isSaving, openDialog }: {
  cultivation: CultivationViewModel; isSaving: boolean; openDialog: OpenDialog;
}) {
  const latest = c.stages[c.stages.length - 1];
  const summary = riskFactorsSummary(c.riskFactors);

  return <Paper variant="outlined" sx={{ p: 2.5 }}>
    <Stack spacing={1.5}>
      <Stack direction="row" useFlexGap spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="h6">{cropLabels[c.crop]} · {c.cultivar}</Typography>
        <Chip size="small" color="success" label="Plantado" />
      </Stack>
      <Typography variant="body2">Plantado em {cycleDate(c.plantedOn)}</Typography>

      <Divider />

      <Stack direction="row" sx={{ gap: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="subtitle2">Estágio da lavoura</Typography>
          <Typography variant="body2" color={latest ? 'text.primary' : 'text.secondary'}>
            {latest ? `${latest.stage}, visto em ${cycleDate(latest.observedOn)}` : 'Ainda não informado'}
          </Typography>
        </Box>
        <Button variant="contained" disabled={isSaving}
          onClick={() => openDialog({ kind: 'stage', cultivation: c })}>Informar estágio</Button>
      </Stack>

      {c.crop === Crop.Corn && <Stack direction="row" sx={{ gap: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="subtitle2">Sobre o plantio</Typography>
          <Typography variant="body2" color={summary ? 'text.primary' : 'text.secondary'}>
            {summary ?? 'Não informado (opcional)'}
          </Typography>
        </Box>
        <Button disabled={isSaving} onClick={() => openDialog({ kind: 'risk', cultivation: c })}>
          {summary ? 'Alterar' : 'Responder'}
        </Button>
      </Stack>}

      {c.stages.length > 1 && <Box>
        <Typography variant="subtitle2">Histórico de estágios</Typography>
        {[...c.stages].reverse().slice(1).map(s => (
          <Typography key={s.id} variant="body2" color="text.secondary">
            {cycleDate(s.observedOn)} · {s.stage}{s.notes ? ` — ${s.notes}` : ''}
          </Typography>
        ))}
      </Box>}

      <Divider />
      <Button color="inherit" sx={{ alignSelf: 'flex-start' }} disabled={isSaving}
        onClick={() => openDialog({ kind: 'closure', cultivation: c })}>Encerrar plantio (colheita)</Button>
    </Stack>
  </Paper>;
}
