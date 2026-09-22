import { useQueryClient } from '@tanstack/react-query';
import {
  getGetInspectionsQueryKey, getGetInspectionByIdQueryKey,
  useScheduleInspection, useRescheduleInspection, useStartInspection,
  useRecordObservation, useCompleteInspection, useCancelInspection,
} from '@/api/generated/inspections/inspections';
import type { RecordObservationViewModel } from '@/api/generated/model/recordObservationViewModel';
import type { ScheduleInspectionViewModel } from '@/api/generated/model/scheduleInspectionViewModel';
import type { InspectionViewModel } from '@/api/generated/model/inspectionViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export function useInspectionActions(id?: string) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const schedule = useScheduleInspection();
  const reschedule = useRescheduleInspection();
  const start = useStartInspection();
  const record = useRecordObservation();
  const complete = useCompleteInspection();
  const cancel = useCancelInspection();

  async function save<T>(operation: () => Promise<T>, message: string): Promise<T | null> {
    try {
      const result = await operation();
      await Promise.all([
        cache.invalidateQueries({ queryKey: getGetInspectionsQueryKey() }),
        ...(id ? [cache.invalidateQueries({ queryKey: getGetInspectionByIdQueryKey(id) })] : []),
      ]);
      notifier.notifySuccess(message);
      return result;
    } catch (error) {
      notifier.notifyError(error);
      return null;
    }
  }

  return {
    isSaving: schedule.isPending || reschedule.isPending || start.isPending
      || record.isPending || complete.isPending || cancel.isPending,

    scheduleInspection: (data: ScheduleInspectionViewModel): Promise<InspectionViewModel | null> =>
      save(() => schedule.mutateAsync({ data }), 'Vistoria agendada.'),

    rescheduleInspection: (inspectionId: string, responsibleId: string, scheduledFor: string) =>
      save(() => reschedule.mutateAsync({ id: inspectionId, data: { responsibleId, scheduledFor } }),
        'Vistoria remarcada.'),

    startInspection: (inspectionId: string) =>
      save(() => start.mutateAsync({ id: inspectionId }), 'Vistoria iniciada.'),

    recordObservation: (inspectionId: string, data: RecordObservationViewModel) =>
      save(() => record.mutateAsync({ id: inspectionId, data }), 'Parada registrada.'),

    completeInspection: (inspectionId: string) =>
      save(() => complete.mutateAsync({ id: inspectionId }), 'Vistoria concluída.'),

    cancelInspection: (inspectionId: string, reason: string) =>
      save(() => cancel.mutateAsync({ id: inspectionId, data: { reason } }), 'Vistoria marcada como não realizada.'),
  };
}
