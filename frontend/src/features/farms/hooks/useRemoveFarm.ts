import { useQueryClient } from '@tanstack/react-query';
import { getGetFarmsQueryKey, useDeleteFarm } from '@/api/generated/farms/farms';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface UseRemoveFarmResult {
  removeFarm: (farmId: string) => Promise<boolean>;
  isRemoving: boolean;
}

/**
 * Exclui uma fazenda.
 *
 * O backend recusa (409 `farm.has_fields`) quando ainda existem talhoes. Nao ha tratamento
 * especial aqui: o catalogo de erros ja transforma esse codigo numa frase que diz ao produtor o
 * que fazer, e o retorno `false` deixa a tela manter o dialogo aberto.
 */
export function useRemoveFarm(): UseRemoveFarmResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();
  const mutation = useDeleteFarm();

  const removeFarm = async (farmId: string) => {
    try {
      await mutation.mutateAsync({ id: farmId });
      await queryClient.invalidateQueries({ queryKey: getGetFarmsQueryKey() });
      notifySuccess('Fazenda excluída.');
      return true;
    } catch (error) {
      notifyError(error);
      return false;
    }
  };

  return { removeFarm, isRemoving: mutation.isPending };
}
