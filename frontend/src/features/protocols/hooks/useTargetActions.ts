import { useQueryClient } from '@tanstack/react-query';
import {
  getGetMonitoringTargetsQueryKey, useCreateMonitoringTarget, useChangeTargetAutomation,
} from '@/api/generated/monitoring-targets/monitoring-targets';
import type { AutomationCapability } from '@/api/generated/model/automationCapability';
import type { CreateMonitoringTargetViewModel } from '@/api/generated/model/createMonitoringTargetViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export function useTargetActions() {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const create = useCreateMonitoringTarget();
  const automation = useChangeTargetAutomation();

  async function save(operation: () => Promise<unknown>, message: string) {
    try {
      await operation();
      // Sem filtro na chave: o catalogo esta em cache por cultura e por tipo, e um alvo novo pode
      // pertencer a qualquer uma dessas listas.
      await cache.invalidateQueries({ queryKey: getGetMonitoringTargetsQueryKey() });
      notifier.notifySuccess(message);
      return true;
    } catch (error) {
      notifier.notifyError(error);
      return false;
    }
  }

  return {
    isSaving: create.isPending || automation.isPending,
    createTarget: (data: CreateMonitoringTargetViewModel) =>
      save(() => create.mutateAsync({ data }), 'Alvo cadastrado no catálogo.'),
    changeAutomation: (id: string, value: AutomationCapability) =>
      save(() => automation.mutateAsync({ id, data: { automation: value } }), 'Situação do alvo atualizada.'),
  };
}
