import { useGetGrowthStages } from '@/api/generated/growth-stages/growth-stages';
import type { Crop } from '@/api/generated/model/crop';

/**
 * A escala fenologica da cultura do cultivo.
 *
 * Lista vazia nao e erro: significa cultura sem escala transcrita, e ali o estadio segue sendo
 * texto livre. E a mesma distincao que o dominio faz em `GrowthStageCatalog`.
 */
export function useGrowthStages(crop: Crop) {
  const query = useGetGrowthStages({ crop });
  const stages = query.data ?? [];
  return { stages, hasScale: stages.length > 0, isLoading: query.isPending };
}
