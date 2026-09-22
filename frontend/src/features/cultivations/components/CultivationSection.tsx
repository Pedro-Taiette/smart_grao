import { Alert, Box, Button, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import { cropLabels } from '@/features/fields/cropLabels';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { CultivationDialog, SeasonDialog, StageDialog, ClosureDialog } from './CultivationDialogs';
import { cycleDate } from '../cultivationSchema';
import { useCultivationWorkspace } from '../hooks/useCultivationWorkspace';

/**
 * Safras, ciclos e estagios de um talhao.
 *
 * Era uma tela propria, alcancada por um botao dentro da tela de talhoes. Virou uma aba da pagina do
 * talhao: "o que esta plantado aqui" e a mesma pergunta que "que talhao e este", e separa-las cobrava
 * uma navegacao para juntar de volta o que sempre se le junto.
 */
export function CultivationSection({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  // Uma chave por talhao monta o estado dos formularios do zero: um dialogo aberto nao pode
  // sobreviver a uma troca de talhao e salvar no lugar errado.
  return <Workspace key={fieldId} farmId={farmId} fieldId={fieldId} />;
}

function Workspace({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  const w = useCultivationWorkspace(farmId, fieldId);
  const common = { isSaving: w.isSaving, onClose: w.closeDialog };
  const hasOpenCycle = w.cultivations.some(c => c.endedOn === null);

  return <>
    <QueryBoundary isLoading={w.isLoading} error={w.error} onRetry={w.refetch}>
      <Stack spacing={2.5}>
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={1.5}>
            <Box>
              <Typography variant="h6">Safras da fazenda</Typography>
              <Typography variant="body2" color="text.secondary">
                O período agrícola em que os cultivos acontecem. Vale para todos os talhões.
              </Typography>
            </Box>
            <Stack direction="row" useFlexGap spacing={1} sx={{ flexWrap: 'wrap' }}>
              {w.seasons.map(s => <Chip key={s.id} label={s.name} />)}
              {w.seasons.length === 0 && <Typography color="text.secondary">
                Cadastre uma safra para iniciar o primeiro cultivo.
              </Typography>}
            </Stack>
            <Button sx={{ alignSelf: 'flex-start' }} disabled={w.isSaving}
              onClick={() => w.openDialog({ kind: 'season' })}>Nova safra</Button>
          </Stack>
        </Paper>

        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
          <Typography variant="h6">Cultivos deste talhão</Typography>
          <Button variant="contained"
            disabled={w.isSaving || !w.field?.active || hasOpenCycle || w.seasons.length === 0}
            onClick={() => w.openDialog({ kind: 'cultivation' })}>Novo cultivo</Button>
        </Stack>

        {!w.field?.active && <Alert severity="info">Reative o talhão para iniciar um cultivo.</Alert>}
        {hasOpenCycle && <Typography variant="body2" color="text.secondary">
          Encerre o cultivo em andamento antes de iniciar o próximo.
        </Typography>}
        {w.cultivations.length === 0 && <Alert severity="info">
          Nenhum cultivo registrado neste talhão. É o cultivo que diz o que está plantado e desde
          quando — as vistorias dependem dele.
        </Alert>}

        {w.cultivations.map(c => <Paper key={c.id} variant="outlined" sx={{ p: 2.5 }}>
          <Stack spacing={1.5}>
            <Stack direction="row" useFlexGap spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
              <Typography variant="h6">{cropLabels[c.crop]} · {c.cultivar}</Typography>
              <Chip size="small" color={c.endedOn ? 'default' : 'success'}
                label={c.endedOn ? 'Encerrado' : 'Em andamento'} />
            </Stack>
            <Typography variant="body2">
              Safra {w.seasons.find(s => s.id === c.seasonId)?.name ?? '—'} · Plantio em {cycleDate(c.plantedOn)}
              {c.endedOn ? ` · Encerrado em ${cycleDate(c.endedOn)}` : ''}
            </Typography>
            <Divider />
            <Typography variant="subtitle2">Estágios observados</Typography>
            {c.stages.length === 0 && <Typography variant="body2" color="text.secondary">
              Nenhum estágio registrado.
            </Typography>}
            {c.stages.map(s => <Box key={s.id}>
              <Typography variant="body2"><strong>{cycleDate(s.observedOn)} · {s.stage}</strong></Typography>
              {s.notes && <Typography variant="body2" color="text.secondary"
                sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{s.notes}</Typography>}
            </Box>)}
            <Stack direction="row" spacing={1}>
              <Button disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'stage', cultivation: c })}>
                Registrar estágio
              </Button>
              {!c.endedOn && <Button disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'closure', cultivation: c })}>
                Encerrar cultivo
              </Button>}
            </Stack>
          </Stack>
        </Paper>)}
      </Stack>
    </QueryBoundary>

    {w.dialog?.kind === 'season' && <SeasonDialog {...common} onSave={w.createSeason} />}
    {w.dialog?.kind === 'cultivation' && <CultivationDialog {...common} seasons={w.seasons} onSave={w.createCultivation} />}
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
