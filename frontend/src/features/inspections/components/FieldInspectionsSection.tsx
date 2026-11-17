import { useState } from 'react';
import { Alert, Button, Stack } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useGetInspections } from '@/api/generated/inspections/inspections';
import { useGetProtocols } from '@/api/generated/protocols/protocols';
import { useCreateDirectedPlan } from '@/api/generated/photos/photos';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { InspectionRow } from '@/features/today/components/InspectionRow';
import { ScheduleInspectionDialog } from './InspectionDialogs';
import { InspectNowButton } from './InspectNowButton';
import { useInspectionActions } from '../hooks/useInspectionActions';
import { usePeople } from '../hooks/usePeople';

/**
 * As visitas ao ciclo aberto deste talhao.
 *
 * Mostrar a lista inteira e o ponto: a mesma malha caminhada muitas vezes, cada visita com o seu
 * proprio registro. E o que o criterio da fase 3 pede.
 */
export function FieldInspectionsSection({ farmId, cultivation }: {
  farmId: string;
  cultivation: CultivationViewModel | null;
}) {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const [isScheduling, setSchedulingState] = useState(params.has('agendar'));
  const setScheduling = (open: boolean) => {
    setSchedulingState(open);
    if (!open && params.has('agendar')) setParams({ aba: 'vistorias' }, { replace: true });
  };
  const actions = useInspectionActions();
  const notifier = useNotifier();
  const directedPlan = useCreateDirectedPlan();

  const inspectionsQuery = useGetInspections(
    { cultivationId: cultivation?.id ?? '' },
    { query: { enabled: Boolean(cultivation) } },
  );
  const { people } = usePeople(farmId, true);
  const protocolsQuery = useGetProtocols(
    { crop: cultivation?.crop ?? 'Corn', status: ProtocolStatus.Published },
    { query: { enabled: Boolean(cultivation) } },
  );

  const protocol = (protocolsQuery.data ?? []).find(p => p.code === 'cercosporiose_deteccao') ?? protocolsQuery.data?.[0];

  async function schedule(responsibleId: string, scheduledFor: string) {
    if (!cultivation || !protocol) {
      notifier.notifyError(new Error('Nenhum protocolo de cercosporiose publicado.'));
      return false;
    }
    try {
      const plan = await directedPlan.mutateAsync({ id: cultivation.id });
      const created = await actions.scheduleInspection({
        cultivationId: cultivation.id, samplingPlanId: plan.plan.id, protocolId: protocol.id, responsibleId, scheduledFor,
      });
      if (created) navigate(`/vistorias/${created.id}`);
      return created !== null;
    } catch (failure) {
      notifier.notifyError(failure);
      return false;
    }
  }

  const inspections = inspectionsQuery.data ?? [];

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
      {/* Vistoriar agora e o caminho normal: cria as paradas, inicia e abre a primeira. Agendar fica
          para quando quem vai a campo e outra pessoa, ou outro dia. */}
      {!cultivation.endedOn && (
        <Stack direction="row" useFlexGap spacing={1} sx={{ flexWrap: 'wrap' }}>
          <InspectNowButton farmId={farmId} cultivationId={cultivation.id} size="large" />
          <Button startIcon={<AddIcon />} disabled={actions.isSaving} onClick={() => setScheduling(true)}>
            Agendar para outro dia
          </Button>
        </Stack>
      )}

      <QueryBoundary
        isLoading={inspectionsQuery.isPending}
        error={inspectionsQuery.error}
        onRetry={() => void inspectionsQuery.refetch()}
      >
        {inspections.length === 0 ? (
          <EmptyState
            title="Nenhuma vistoria neste plantio ainda"
            description="O sistema escolhe as 10 paradas — onde o satélite viu a lavoura destoar, ou espalhadas pelo talhão. Em cada uma, você olha 10 plantas e fotografa as folhas."

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
          isSaving={actions.isSaving || directedPlan.isPending}
          people={people}
          onClose={() => setScheduling(false)}
          onSave={values => schedule(values.responsibleId, values.scheduledFor)}
        />
      )}
    </Stack>
  );
}
