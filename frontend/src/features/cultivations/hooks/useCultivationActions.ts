import { useQueryClient } from '@tanstack/react-query';
import {
  getGetCultivationsQueryKey, getGetCultivationByIdQueryKey,
  useCreateCultivation, useCloseCultivation, useRecordGrowthStage,
} from '@/api/generated/cultivations/cultivations';
import { getGetSeasonsQueryKey, useCreateSeason } from '@/api/generated/seasons/seasons';
import type { CreateCultivationViewModel } from '@/api/generated/model/createCultivationViewModel';
import type { RecordGrowthStageViewModel } from '@/api/generated/model/recordGrowthStageViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export function useCultivationActions(farmId: string, fieldId: string) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const season = useCreateSeason();
  const create = useCreateCultivation();
  const close = useCloseCultivation();
  const stage = useRecordGrowthStage();

  async function save(operation: () => Promise<unknown>, message: string, id?: string) {
    try {
      await operation();
      await Promise.all([
        // Sem filtro na chave: o mesmo cultivo esta em cache sob o recorte do talhao (a aba de
        // cultivo) e sob o da fazenda (a tela Hoje, a lista de talhoes, a aba de pontos). Invalidar
        // so `{ fieldId }` deixava as outras com o dado velho ate um F5.
        cache.invalidateQueries({ queryKey: getGetCultivationsQueryKey() }),
        cache.invalidateQueries({ queryKey: getGetSeasonsQueryKey({ farmId }) }),
        ...(id ? [cache.invalidateQueries({ queryKey: getGetCultivationByIdQueryKey(id) })] : []),
      ]);
      notifier.notifySuccess(message);
      return true;
    } catch (error) {
      notifier.notifyError(error);
      return false;
    }
  }

  return {
    isSaving: season.isPending || create.isPending || close.isPending || stage.isPending,
    createSeason: (name: string) => save(() => season.mutateAsync({ data: { farmId, name } }), 'Safra cadastrada.'),
    createCultivation: (data: Omit<CreateCultivationViewModel, 'fieldId'>) =>
      save(() => create.mutateAsync({ data: { ...data, fieldId } }), 'Cultivo cadastrado.'),
    closeCultivation: (id: string, endedOn: string) =>
      save(() => close.mutateAsync({ id, data: { endedOn } }), 'Cultivo encerrado.', id),
    recordStage: (id: string, data: RecordGrowthStageViewModel) =>
      save(() => stage.mutateAsync({ id, data }), 'Estágio registrado.', id),
  };
}
