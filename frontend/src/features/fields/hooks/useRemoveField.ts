import { useQueryClient } from '@tanstack/react-query';
import { getGetFieldsByFarmQueryKey } from '@/api/generated/farms/farms';
import { useDeleteField } from '@/api/generated/fields/fields';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface UseRemoveFieldResult {
  removeField: (fieldId: string, farmId: string) => Promise<boolean>;
  isRemoving: boolean;
}

/** Exclui um talhao definitivamente. Para tirar de operacao sem perder historico, use desativar. */
export function useRemoveField(): UseRemoveFieldResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();
  const mutation = useDeleteField();

  const removeField = async (fieldId: string, farmId: string) => {
    try {
      await mutation.mutateAsync({ id: fieldId });
      await queryClient.invalidateQueries({ queryKey: getGetFieldsByFarmQueryKey(farmId) });
      notifySuccess('Talhão excluído.');
      return true;
    } catch (error) {
      notifyError(error);
      return false;
    }
  };

  return { removeField, isRemoving: mutation.isPending };
}
