import { useState } from 'react';
import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate, useParams } from 'react-router-dom';
import { useGetInspections } from '@/api/generated/inspections/inspections';
import { useGetProtocols } from '@/api/generated/protocols/protocols';
// O histórico de planos é uma rota do talhão, e por isso o hook sai do módulo de talhões.
import { useGetFieldById, useGetSamplingPlansByField } from '@/api/generated/fields/fields';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import type { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import { PageContainer } from '@/shared/components/AppLayout';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { useCultivations } from '@/features/cultivations/hooks/useCultivations';
import { ScheduleInspectionDialog } from '../components/InspectionDialogs';
import {
  describeProgress, formatDay, inspectionStatusColor, inspectionStatusLabel, inspectionStatusOptions,
} from '../inspectionLabels';
import { useInspectionActions } from '../hooks/useInspectionActions';
import { usePeople } from '../hooks/usePeople';

/**
 * As vistorias de um cultivo.
 *
 * Mostrar a lista inteira e o ponto: a mesma malha caminhada muitas vezes, cada visita com o seu
 * proprio registro. E o que o criterio da fase 3 pede.
 */
export function InspectionsPage() {
  const { farmId = '', fieldId = '' } = useParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<InspectionStatus | ''>('');
  const [isScheduling, setScheduling] = useState(false);

  const fieldQuery = useGetFieldById(fieldId, { query: { enabled: Boolean(fieldId) } });
  const { cultivations, isLoading: cyclesLoading } = useCultivations(farmId, fieldId);
  const open = cultivations.find(cycle => cycle.endedOn === null) ?? cultivations[0];

  const inspectionsQuery = useGetInspections(
    { cultivationId: open?.id ?? '', ...(status ? { status } : {}) },
    { query: { enabled: open !== undefined } });

  const { people } = usePeople(farmId, true);
  const plansQuery = useGetSamplingPlansByField(fieldId, { query: { enabled: Boolean(fieldId) } });
  const protocolsQuery = useGetProtocols(
    { crop: open?.crop ?? 'Corn', status: ProtocolStatus.Published },
    { query: { enabled: open !== undefined } });

  const actions = useInspectionActions();
  const inspections = inspectionsQuery.data ?? [];
  const plans = (plansQuery.data ?? []).filter(plan => plan.cultivationId === open?.id);

  return (
    <PageContainer>
      <Button sx={{ mb: 2 }} onClick={() => navigate(`/farms/${farmId}/fields`)}>Voltar aos talhões</Button>

      <Stack direction="row" sx={{ mb: 3, justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="h5" component="h1">Vistorias — {fieldQuery.data?.name ?? 'Talhão'}</Typography>
          <Typography variant="body2" color="text.secondary">
            Cada visita ao talhão, com o que foi encontrado em cada parada.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} disabled={actions.isSaving || open === undefined}
          onClick={() => setScheduling(true)}>Agendar vistoria</Button>
      </Stack>

      <QueryBoundary
        isLoading={cyclesLoading || inspectionsQuery.isPending}
        error={inspectionsQuery.error}
        onRetry={() => void inspectionsQuery.refetch()}
      >
        {open === undefined ? (
          <Alert severity="info">
            Cadastre um cultivo neste talhão antes de agendar vistorias — é ele que diz qual é a
            cultura e qual ciclo está sendo acompanhado.
          </Alert>
        ) : <>
          <TextField select size="small" label="Situação" sx={{ mb: 3, minWidth: 200 }}
            value={status} onChange={event => setStatus(event.target.value as InspectionStatus | '')}>
            <MenuItem value="">Todas</MenuItem>
            {inspectionStatusOptions.map(option => (
              <MenuItem key={option} value={option}>{inspectionStatusLabel[option]}</MenuItem>
            ))}
          </TextField>

          {inspections.length === 0 ? (
            <EmptyState
              title="Nenhuma vistoria para este cultivo"
              description="Agende a primeira visita: escolha quem vai, qual caminhada e o que coletar em cada parada."
              action={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setScheduling(true)}>Agendar vistoria</Button>}
            />
          ) : (
            <Stack spacing={2}>
              {inspections.map(inspection => (
                <Paper key={inspection.id} variant="outlined" sx={{ p: 2.5 }}>
                  <Stack spacing={1}>
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                      <Typography variant="subtitle1">{formatDay(inspection.scheduledFor)}</Typography>
                      <Chip size="small" color={inspectionStatusColor[inspection.status]}
                        label={inspectionStatusLabel[inspection.status]} />
                      <Typography variant="body2" color="text.secondary">{inspection.responsibleName}</Typography>
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      {describeProgress(inspection.visitedPointCount, inspection.pointCount)}
                      {inspection.observationCount > inspection.visitedPointCount
                        && ` · ${inspection.observationCount - inspection.visitedPointCount} fora da malha`}
                    </Typography>
                    {inspection.cancellationReason && <Typography variant="body2" color="text.secondary">
                      {inspection.cancellationReason}
                    </Typography>}
                    <Button size="small" sx={{ alignSelf: 'flex-start' }}
                      onClick={() => navigate(`/inspections/${inspection.id}`)}>Abrir</Button>
                  </Stack>
                </Paper>
              ))}
            </Stack>
          )}
        </>}
      </QueryBoundary>

      {isScheduling && open && <ScheduleInspectionDialog
        isSaving={actions.isSaving}
        people={people}
        plans={plans}
        protocols={protocolsQuery.data ?? []}
        onClose={() => setScheduling(false)}
        onSave={async values => {
          const created = await actions.scheduleInspection({ ...values, cultivationId: open.id });
          if (created) navigate(`/inspections/${created.id}`);
          return created !== null;
        }} />}
    </PageContainer>
  );
}
