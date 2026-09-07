import { useGetSamplingPlansByField } from '@/api/generated/fields/fields';
import type { SamplingPlanSummaryViewModel } from '@/api/generated/model/samplingPlanSummaryViewModel';

export interface UseSamplingPlansResult {
  plans: SamplingPlanSummaryViewModel[];
  /** O mais recente — e o que a tela mostra por padrao. */
  latestPlan: SamplingPlanSummaryViewModel | null;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
}

/**
 * Historico de amostragem do talhao, do mais recente para o mais antigo.
 *
 * Vem sem os pontos: a lista cresce safra afora — o MIP pede amostragem semanal — e carregar a malha
 * de cada plano so para desenhar o historico traria centenas de coordenadas por linha.
 */
export function useSamplingPlans(fieldId: string | null): UseSamplingPlansResult {
  const query = useGetSamplingPlansByField(fieldId ?? '', {
    query: { enabled: Boolean(fieldId) },
  });

  const plans = query.data ?? [];

  return {
    plans,
    latestPlan: plans[0] ?? null,
    isLoading: query.isPending,
    error: query.error,
    refetch: () => void query.refetch(),
  };
}
