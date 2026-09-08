import { useGetSamplingPlanById } from '@/api/generated/sampling-plans/sampling-plans';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';

export interface UseSamplingPlanResult {
  points: SamplingPointViewModel[];
  isLoading: boolean;
  error: unknown;
}

/**
 * Um plano com a malha, para desenhar os pontos no mapa.
 *
 * Separado do historico de proposito: so o plano que esta sendo olhado paga o custo de trazer as
 * coordenadas.
 */
export function useSamplingPlan(planId: string | null): UseSamplingPlanResult {
  const query = useGetSamplingPlanById(planId ?? '', {
    query: { enabled: Boolean(planId) },
  });

  return {
    points: query.data?.points ?? [],
    isLoading: query.isPending,
    error: query.error,
  };
}
