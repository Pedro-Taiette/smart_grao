import { Alert, Box, Button, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { cropLabels } from '@/features/fields/cropLabels';
import { useSyncFarmFromRecord } from '@/features/farms/useFarmContext';
import { PageContainer } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { CancelInspectionDialog, RescheduleDialog } from '../components/InspectionDialogs';
import { ObservationDialog } from '../components/ObservationDialog';
import {
  describeCount, describeProgress, formatDay, formatMoment, inspectionStatusColor,
  inspectionStatusExplanation, inspectionStatusLabel,
} from '../inspectionLabels';
import { useInspectionWorkspace } from '../hooks/useInspectionWorkspace';
import { usePeople } from '../hooks/usePeople';
import { useGetFieldById } from '@/api/generated/fields/fields';

export function InspectionDetailPage() {
  const { inspectionId = '' } = useParams();
  // Uma URL diferente monta o estado dos diálogos do zero, para que uma parada em digitação não
  // acabe gravando na visita errada.
  return <InspectionWorkspace key={inspectionId} inspectionId={inspectionId} />;
}

function InspectionWorkspace({ inspectionId }: { inspectionId: string }) {
  const navigate = useNavigate();
  const w = useInspectionWorkspace(inspectionId);
  const inspection = w.inspection;
  const fieldQuery = useGetFieldById(inspection?.fieldId ?? '', {
    query: { enabled: inspection !== undefined },
  });
  const { people } = usePeople(fieldQuery.data?.farmId ?? '', true);

  // Esta é a tela que chega por link no celular, sem passar pela navegação: alinhar o contexto aqui
  // é o que faz o resto do sistema abrir na propriedade certa depois.
  useSyncFarmFromRecord(fieldQuery.data?.farmId);

  return (
    <PageContainer>
      <Stack spacing={2.5}>
        {/* Volta para o talhão, e não para a lista geral: quem abriu esta visita veio de lá, e é
            onde estão as outras visitas da mesma malha. */}
        <Button startIcon={<ArrowBackIcon />} sx={{ alignSelf: 'flex-start', ml: -1 }}
          onClick={() => inspection
            ? navigate(`/talhoes/${inspection.fieldId}?aba=vistorias`)
            : navigate('/vistorias')}>
          Voltar
        </Button>

        <QueryBoundary isLoading={w.isLoading} error={w.error} onRetry={w.refetch}>
          {inspection === undefined ? null : <>
            <Box>
              <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                <Typography variant="h5" component="h1">
                  Vistoria de {formatDay(inspection.scheduledFor)}
                </Typography>
                <Chip size="small" color={inspectionStatusColor[inspection.status]}
                  label={inspectionStatusLabel[inspection.status]} />
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {inspection.responsibleName} · {cropLabels[inspection.crop]} ·{' '}
                {inspection.protocolName} v{inspection.protocolVersion}
              </Typography>
            </Box>

            <Alert severity={w.isInProgress ? 'warning' : 'info'}>
              {inspectionStatusExplanation[inspection.status]}
            </Alert>

            {inspection.cancellationReason && <Alert severity="info">
              Motivo: {inspection.cancellationReason}
            </Alert>}

            <Typography variant="body2">
              {describeProgress(
                inspection.observations.filter(o => !o.isOffPlan).length, inspection.pointCount)}
              {inspection.startedAt && ` · iniciada em ${formatMoment(inspection.startedAt)}`}
              {inspection.completedAt && ` · concluída em ${formatMoment(inspection.completedAt)}`}
            </Typography>

            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              {w.isScheduled && <>
                <Button variant="contained" disabled={w.isSaving}
                  onClick={() => w.startInspection(inspection.id)}>Iniciar vistoria</Button>
                <Button disabled={w.isSaving}
                  onClick={() => w.openDialog({ kind: 'reschedule' })}>Remarcar</Button>
              </>}
              {w.isInProgress && <>
                <Button variant="contained" startIcon={<AddIcon />} disabled={w.isSaving || !w.protocol}
                  onClick={() => w.openDialog({ kind: 'observation' })}>Registrar parada</Button>
                <Button disabled={w.isSaving || inspection.observations.length === 0}
                  onClick={() => w.completeInspection(inspection.id)}>Concluir</Button>
              </>}
              {!w.isFinished && <Button color="error" disabled={w.isSaving}
                onClick={() => w.openDialog({ kind: 'cancel' })}>Não aconteceu</Button>}
            </Stack>

            <Divider />
            <Typography variant="h6">Paradas registradas</Typography>

            {inspection.observations.length === 0
              ? <Alert severity="info">
                  Nenhuma parada ainda. {w.isScheduled && 'Inicie a vistoria para começar a registrar.'}
                </Alert>
              : inspection.observations.map(observation => (
                <Paper key={observation.id} variant="outlined" sx={{ p: 2.5 }}>
                  <Stack spacing={1}>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                      <Typography variant="subtitle1">
                        {observation.isOffPlan ? 'Fora da malha' : `Parada ${observation.pointSequence ?? '—'}`}
                      </Typography>
                      {observation.growthStage && <Chip size="small" label={observation.growthStage} />}
                      {observation.hasPoorAccuracy && <Chip size="small" color="warning" label="GPS impreciso" />}
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      {formatMoment(observation.recordedAt)} ·{' '}
                      {observation.location.latitude.toFixed(5)}, {observation.location.longitude.toFixed(5)}
                      {observation.accuracyMeters !== null && ` · ${Math.round(observation.accuracyMeters)} m`}
                    </Typography>
                    {observation.notes && <Typography variant="body2"
                      sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{observation.notes}</Typography>}
                    <Stack spacing={0.25}>
                      {observation.counts.map(count => (
                        <Typography key={count.id} variant="body2"
                          color={count.detected ? 'text.primary' : 'text.secondary'}>
                          {count.targetCommonName}: {describeCount(count.value, count.detected, count.unit)}
                        </Typography>
                      ))}
                    </Stack>
                    {/* A lista acima e o registro do que foi avaliado. Alvo do protocolo que nao
                        aparece aqui nao foi olhado — e essa ausencia e deliberadamente visivel. */}
                    {w.protocol && observation.counts.length < w.protocol.items.length && (
                      <Typography variant="caption" color="text.secondary">
                        {w.protocol.items.length - observation.counts.length} alvo(s) do protocolo não
                        foram avaliados nesta parada.
                      </Typography>
                    )}
                  </Stack>
                </Paper>
              ))}
          </>}
        </QueryBoundary>
      </Stack>

      {inspection && w.protocol && w.dialog?.kind === 'observation' && <ObservationDialog
        crop={inspection.crop}
        protocol={w.protocol}
        remainingPoints={w.remainingPoints}
        isSaving={w.isSaving}
        onClose={w.closeDialog}
        onSave={async data => await w.recordObservation(inspection.id, data) !== null} />}

      {inspection && w.dialog?.kind === 'reschedule' && <RescheduleDialog
        isSaving={w.isSaving}
        people={people}
        responsibleId={inspection.responsibleId}
        scheduledFor={inspection.scheduledFor}
        onClose={w.closeDialog}
        onSave={async values =>
          await w.rescheduleInspection(inspection.id, values.responsibleId, values.scheduledFor) !== null} />}

      {inspection && w.dialog?.kind === 'cancel' && <CancelInspectionDialog
        isSaving={w.isSaving}
        onClose={w.closeDialog}
        onSave={async values => await w.cancelInspection(inspection.id, values.reason) !== null} />}
    </PageContainer>
  );
}
