import { useQueryClient } from '@tanstack/react-query';
import { getGetFieldsByFarmQueryKey } from '@/api/generated/farms/farms';
import { useChangeFieldStatus } from '@/api/generated/fields/fields';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface UseToggleFieldStatusResult {
  toggleFieldStatus: (fieldId: string, farmId: string, active: boolean) => Promise<boolean>;
  isToggling: boolean;
}

/**
 * Ativa ou desativa um talhao.
 *
 * Desativar e o caminho preferido a excluir: a area sai da operacao e o historico de amostragem e
 * diagnostico continua existindo.
 */
export function useToggleFieldStatus(): UseToggleFieldStatusResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();
  const mutation = useChangeFieldStatus();

  const toggleFieldStatus = async (fieldId: string, farmId: string, active: boolean) => {
    try {
      await mutation.mutateAsync({ id: fieldId, params: { active } });
      await queryClient.invalidateQueries({ queryKey: getGetFieldsByFarmQueryKey(farmId) });
      notifySuccess(active ? 'Talhão reativado.' : 'Talhão desativado.');
      return true;
    } catch (error) {
      notifyError(error);
      return false;
    }
  };

  return { toggleFieldStatus, isToggling: mutation.isPending };
}
