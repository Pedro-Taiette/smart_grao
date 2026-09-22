import { useState } from 'react';
import { Alert, Button, Stack } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import { useGetInspections } from '@/api/generated/inspections/inspections';
import { useGetProtocols } from '@/api/generated/protocols/protocols';
// O histórico de malhas é uma rota do talhão, e por isso o hook sai do módulo de talhões.
import { useGetSamplingPlansByField } from '@/api/generated/fields/fields';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { InspectionRow } from '@/features/today/components/InspectionRow';
import { ScheduleInspectionDialog } from './InspectionDialogs';
import { useInspectionActions } from '../hooks/useInspectionActions';
import { usePeople } from '../hooks/usePeople';

/**
 * As visitas ao ciclo aberto deste talhao.
 *
 * Mostrar a lista inteira e o ponto: a mesma malha caminhada muitas vezes, cada visita com o seu
 * proprio registro. E o que o criterio da fase 3 pede.
 */
export function FieldInspectionsSection({ farmId, fieldId, cultivation }: {
  farmId: string;
  fieldId: string;
  cultivation: CultivationViewModel | null;
}) {
  const navigate = useNavigate();
  const [isScheduling, setScheduling] = useState(false);
  const actions = useInspectionActions();

  const inspectionsQuery = useGetInspections(
    { cultivationId: cultivation?.id ?? '' },
    { query: { enabled: Boolean(cultivation) } },
  );
  const { people } = usePeople(farmId, true);
  const plansQuery = useGetSamplingPlansByField(fieldId, { query: { enabled: Boolean(fieldId) } });
  const protocolsQuery = useGetProtocols(
    { crop: cultivation?.crop ?? 'Corn', status: ProtocolStatus.Published },
    { query: { enabled: Boolean(cultivation) } },
  );

  const inspections = inspectionsQuery.data ?? [];
  const plans = (plansQuery.data ?? []).filter(plan => plan.cultivationId === cultivation?.id);

  if (!cultivation) {
    return (
      <Alert severity="info">
        Registre um cultivo neste talhão antes de agendar vistorias — é ele que diz qual é a cultura
        e qual ciclo está sendo acompanhado.
      </Alert>
    );
  }

  return (
    <Stack spacing={2}>
      <Button
        variant="contained"
        startIcon={<AddIcon />}
        sx={{ alignSelf: 'flex-start' }}
        disabled={actions.isSaving}
        onClick={() => setScheduling(true)}
      >
        Agendar vistoria
      </Button>

      <QueryBoundary
        isLoading={inspectionsQuery.isPending}
        error={inspectionsQuery.error}
        onRetry={() => void inspectionsQuery.refetch()}
      >
        {inspections.length === 0 ? (
          <EmptyState
            title="Nenhuma vistoria para este cultivo"
            description="Agende a primeira visita: escolha quem vai, qual caminhada e o que coletar em cada parada."
            action={
              <Button variant="contained" startIcon={<AddIcon />} onClick={() => setScheduling(true)}>
                Agendar vistoria
              </Button>
            }
          />
        ) : (
          <Stack spacing={1.5}>
            {inspections.map(inspection => (
              <InspectionRow key={inspection.id} inspection={inspection} showField={false} />
            ))}
          </Stack>
        )}
      </QueryBoundary>

      {isScheduling && (
        <ScheduleInspectionDialog
          isSaving={actions.isSaving}
          people={people}
          plans={plans}
          protocols={protocolsQuery.data ?? []}
          onClose={() => setScheduling(false)}
          onSave={async values => {
            const created = await actions.scheduleInspection({ ...values, cultivationId: cultivation.id });
            if (created) navigate(`/vistorias/${created.id}`);
            return created !== null;
          }}
        />
      )}
    </Stack>
  );
}
