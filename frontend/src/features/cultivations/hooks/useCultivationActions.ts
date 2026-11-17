import { useQueryClient } from '@tanstack/react-query';
import {
  getGetCultivationsQueryKey, getGetCultivationByIdQueryKey,
  useCreateCultivation, useCloseCultivation, useRecordGrowthStage, useUpdateCultivationRiskFactors,
} from '@/api/generated/cultivations/cultivations';
import { getGetSeasonsQueryKey, useCreateSeason } from '@/api/generated/seasons/seasons';
import type { CreateCultivationViewModel } from '@/api/generated/model/createCultivationViewModel';
import type { CercosporaRiskFactorsViewModel } from '@/api/generated/model/cercosporaRiskFactorsViewModel';
import type { RecordGrowthStageViewModel } from '@/api/generated/model/recordGrowthStageViewModel';
import type { SeasonViewModel } from '@/api/generated/model/seasonViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { seasonNameFor } from '../cultivationSchema';

export function useCultivationActions(farmId: string, fieldId: string, seasons: SeasonViewModel[] = []) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const season = useCreateSeason();
  const create = useCreateCultivation();
  const close = useCloseCultivation();
  const stage = useRecordGrowthStage();
  const risk = useUpdateCultivationRiskFactors();

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

  /**
   * A safra sai da data do plantio e e criada na hora se ainda nao existir — quem cadastra o milho
   * nao precisa saber que "safra" e um registro a parte.
   */
  async function seasonIdFor(plantedOn: string) {
    const name = seasonNameFor(plantedOn);
    const existing = seasons.find(s => s.name.trim().toLowerCase() === name.toLowerCase());
    if (existing) return existing.id;
    const created = await season.mutateAsync({ data: { farmId, name } });
    return created.id;
  }

  return {
    isSaving: season.isPending || create.isPending || close.isPending || stage.isPending || risk.isPending,
    createCultivation: (data: Omit<CreateCultivationViewModel, 'fieldId' | 'seasonId'>) =>
      save(async () => {
        const seasonId = await seasonIdFor(data.plantedOn);
        await create.mutateAsync({ data: { ...data, seasonId, fieldId } });
      }, 'Plantio registrado.'),
    closeCultivation: (id: string, endedOn: string) =>
      save(() => close.mutateAsync({ id, data: { endedOn } }), 'Cultivo encerrado.', id),
    recordStage: (id: string, data: RecordGrowthStageViewModel) =>
      save(() => stage.mutateAsync({ id, data }), 'Estágio registrado.', id),
    updateRiskFactors: (id: string, data: CercosporaRiskFactorsViewModel) =>
      save(() => risk.mutateAsync({ id, data }), 'Risco atualizado.', id),
  };
}
