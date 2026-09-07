import { useQueryClient } from '@tanstack/react-query';
import { getGetSamplingPlansByFieldQueryKey } from '@/api/generated/fields/fields';
import { useDeleteSamplingPlan } from '@/api/generated/sampling-plans/sampling-plans';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface UseRemoveSamplingPlanResult {
  removePlan: (planId: string, fieldId: string) => Promise<boolean>;
  isRemoving: boolean;
}

/**
 * Apaga um plano e a malha junto.
 *
 * Diferente do talhao, um plano ainda sem coleta nao carrega historico de lavoura — e um roteiro que
 * foi gerado e nao serviu. Por isso a exclusao e definitiva e nao ha "desativar".
 */
export function useRemoveSamplingPlan(): UseRemoveSamplingPlanResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();
  const mutation = useDeleteSamplingPlan();

  const removePlan = async (planId: string, fieldId: string) => {
    try {
      await mutation.mutateAsync({ id: planId });
      await queryClient.invalidateQueries({
        queryKey: getGetSamplingPlansByFieldQueryKey(fieldId),
      });

      notifySuccess('Pontos apagados.');
      return true;
    } catch (error) {
      notifyError(error);
      return false;
    }
  };

  return { removePlan, isRemoving: mutation.isPending };
}
