import { Alert, Box, Button, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { cropLabels } from '@/features/fields/cropLabels';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { CultivationDialog, SeasonDialog, StageDialog, ClosureDialog } from '../components/CultivationDialogs';
import { cycleDate } from '../cultivationSchema';
import { useCultivationWorkspace } from '../hooks/useCultivationWorkspace';

export function CultivationsPage() {
  const { farmId = '', fieldId = '' } = useParams();
  // A different URL mounts fresh form state, so an old dialog cannot target another field.
  return <CultivationWorkspace key={`${farmId}/${fieldId}`} farmId={farmId} fieldId={fieldId} />;
}

function CultivationWorkspace({ farmId, fieldId }: { farmId: string; fieldId: string }) {
  const navigate = useNavigate();
  const w = useCultivationWorkspace(farmId, fieldId);
  const common = { isSaving: w.isSaving, onClose: w.closeDialog };
  const hasOpenCycle = w.cultivations.some(c => c.endedOn === null);

  return <Box sx={{ overflow: 'auto', p: { xs: 2, md: 3 }, height: '100%' }}>
    <Stack spacing={2.5} sx={{ maxWidth: 1000, mx: 'auto' }}>
      <Button sx={{ alignSelf: 'flex-start' }} onClick={() => navigate(`/farms/${farmId}/fields`)}>Voltar aos talhões</Button>
      <Box>
        <Typography variant="h5" component="h1">Cultivos — {w.field?.name ?? 'Talhão'}</Typography>
        <Typography color="text.secondary">Safras, plantios e desenvolvimento ao longo dos ciclos.</Typography>
      </Box>
      <QueryBoundary isLoading={w.isLoading} error={w.error} onRetry={w.refetch}>
        {w.wrongFarm ? <Alert severity="error">Este talhão não pertence à fazenda informada.</Alert> : <>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Stack spacing={1.5}>
              <Typography variant="h6">Safras da fazenda</Typography>
              <Stack direction="row" useFlexGap spacing={1} sx={{ flexWrap: 'wrap' }}>
                {w.seasons.map(s => <Chip key={s.id} label={s.name} />)}
                {w.seasons.length === 0 && <Typography color="text.secondary">Cadastre uma safra para iniciar o primeiro cultivo.</Typography>}
              </Stack>
              <Button sx={{ alignSelf: 'flex-start' }} disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'season' })}>Nova safra</Button>
            </Stack>
          </Paper>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="h6">Histórico do talhão</Typography>
            <Button variant="contained" disabled={w.isSaving || !w.field?.active || hasOpenCycle || w.seasons.length === 0}
              onClick={() => w.openDialog({ kind: 'cultivation' })}>Novo cultivo</Button>
          </Stack>
          {!w.field?.active && <Alert severity="info">Reative o talhão para iniciar um cultivo.</Alert>}
          {hasOpenCycle && <Typography variant="body2" color="text.secondary">Encerre o cultivo em andamento antes de iniciar o próximo.</Typography>}
          {w.cultivations.length === 0 && <Alert severity="info">Nenhum cultivo registrado neste talhão.</Alert>}
          {w.cultivations.map(c => <Paper key={c.id} variant="outlined" sx={{ p: 2.5 }}>
            <Stack spacing={1.5}>
              <Stack direction="row" useFlexGap spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                <Typography variant="h6">{cropLabels[c.crop]} · {c.cultivar}</Typography>
                <Chip size="small" color={c.endedOn ? 'default' : 'success'} label={c.endedOn ? 'Encerrado' : 'Em andamento'} />
              </Stack>
              <Typography variant="body2">Safra {w.seasons.find(s => s.id === c.seasonId)?.name ?? '—'} · Plantio em {cycleDate(c.plantedOn)}{c.endedOn ? ` · Encerrado em ${cycleDate(c.endedOn)}` : ''}</Typography>
              <Divider />
              <Typography variant="subtitle2">Estágios observados</Typography>
              {c.stages.length === 0 && <Typography variant="body2" color="text.secondary">Nenhum estágio registrado.</Typography>}
              {c.stages.map(s => <Box key={s.id}>
                <Typography variant="body2"><strong>{cycleDate(s.observedOn)} · {s.stage}</strong></Typography>
                {s.notes && <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{s.notes}</Typography>}
              </Box>)}
              <Stack direction="row" spacing={1}>
                <Button disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'stage', cultivation: c })}>Registrar estágio</Button>
                {!c.endedOn && <Button disabled={w.isSaving} onClick={() => w.openDialog({ kind: 'closure', cultivation: c })}>Encerrar cultivo</Button>}
              </Stack>
            </Stack>
          </Paper>)}
        </>}
      </QueryBoundary>
    </Stack>
    {w.dialog?.kind === 'season' && <SeasonDialog {...common} onSave={w.createSeason} />}
    {w.dialog?.kind === 'cultivation' && <CultivationDialog {...common} seasons={w.seasons} onSave={w.createCultivation} />}
    {w.dialog?.kind === 'stage' && <StageDialog {...common} cultivation={w.dialog.cultivation}
      onSave={values => w.dialog?.kind === 'stage' ? w.recordStage(w.dialog.cultivation.id, { ...values, notes: values.notes || null }) : Promise.resolve(false)} />}
    {w.dialog?.kind === 'closure' && <ClosureDialog {...common} cultivation={w.dialog.cultivation}
      onSave={date => w.dialog?.kind === 'closure' ? w.closeCultivation(w.dialog.cultivation.id, date) : Promise.resolve(false)} />}
  </Box>;
}
