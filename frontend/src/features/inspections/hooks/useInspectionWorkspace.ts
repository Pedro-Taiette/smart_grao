import { useState } from 'react';
import { useGetInspectionById } from '@/api/generated/inspections/inspections';
import { useGetProtocolById } from '@/api/generated/protocols/protocols';
import { useGetSamplingPlanById } from '@/api/generated/sampling-plans/sampling-plans';
import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import { useInspectionActions } from './useInspectionActions';

type Dialog = { kind: 'observation' | 'cancel' | 'reschedule' } | null;

export function useInspectionWorkspace(inspectionId: string) {
  const query = useGetInspectionById(inspectionId, { query: { enabled: Boolean(inspectionId) } });
  const inspection = query.data;
  const actions = useInspectionActions(inspectionId);
  const [dialog, setDialog] = useState<Dialog>(null);

  // O protocolo diz o que se conta em cada parada; a malha, onde sao as paradas. As duas consultas
  // so partem depois que a vistoria e conhecida.
  const protocolQuery = useGetProtocolById(inspection?.protocolId ?? '', {
    query: { enabled: inspection !== undefined },
  });
  const planQuery = useGetSamplingPlanById(inspection?.samplingPlanId ?? '', {
    query: { enabled: inspection !== undefined },
  });

  const visited = new Set(
    inspection?.observations
      .map(observation => observation.samplingPointId)
      .filter((id): id is string => id !== null) ?? []);

  return {
    ...actions,
    inspection,
    protocol: protocolQuery.data,
    /** Paradas da malha que ainda não foram registradas nesta visita. */
    remainingPoints: (planQuery.data?.points ?? []).filter(point => !visited.has(point.id)),
    isScheduled: inspection?.status === InspectionStatus.Scheduled,
    isInProgress: inspection?.status === InspectionStatus.InProgress,
    isFinished: inspection?.status === InspectionStatus.Completed
      || inspection?.status === InspectionStatus.Cancelled,
    isLoading: query.isPending,
    error: query.error ?? protocolQuery.error ?? planQuery.error,
    refetch: () => { void query.refetch(); void protocolQuery.refetch(); void planQuery.refetch(); },
    dialog, openDialog: setDialog, closeDialog: () => setDialog(null),
  };
}
