import { useQueryClient } from '@tanstack/react-query';
import {
  getGetFarmByIdQueryKey,
  getGetFarmsQueryKey,
  useCreateFarm,
  useUpdateFarm,
} from '@/api/generated/farms/farms';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';
import type { SaveFarmViewModel } from '@/api/generated/model/saveFarmViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export interface UseSaveFarmResult {
  saveFarm: (values: SaveFarmViewModel, farmId?: string) => Promise<FarmViewModel | null>;
  isSaving: boolean;
}

/**
 * Cadastra ou atualiza uma fazenda.
 *
 * Uma unica funcao para os dois casos porque o formulario e o mesmo: quem chama passa o `farmId`
 * quando esta editando e omite quando esta criando. Sem isso, todo dialogo de formulario carregaria
 * um `if (isEditing)` para escolher entre duas mutations — logica que nao e da tela.
 *
 * A invalidacao de cache e o aviso de erro moram aqui, entao a tela nao repete nenhum dos dois.
 * O retorno e `null` em caso de falha, e nao uma excecao, porque o erro ja foi comunicado ao
 * usuario — o formulario so precisa saber se pode fechar.
 */
export function useSaveFarm(): UseSaveFarmResult {
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useNotifier();

  const createMutation = useCreateFarm();
  const updateMutation = useUpdateFarm();

  const saveFarm = async (values: SaveFarmViewModel, farmId?: string) => {
    try {
      const saved = farmId
        ? await updateMutation.mutateAsync({ id: farmId, data: values })
        : await createMutation.mutateAsync({ data: values });

      await queryClient.invalidateQueries({ queryKey: getGetFarmsQueryKey() });
      if (farmId) {
        await queryClient.invalidateQueries({ queryKey: getGetFarmByIdQueryKey(farmId) });
      }

      notifySuccess(farmId ? 'Fazenda atualizada.' : 'Fazenda cadastrada.');
      return saved as FarmViewModel;
    } catch (error) {
      notifyError(error);
      return null;
    }
  };

  return {
    saveFarm,
    isSaving: createMutation.isPending || updateMutation.isPending,
  };
}
