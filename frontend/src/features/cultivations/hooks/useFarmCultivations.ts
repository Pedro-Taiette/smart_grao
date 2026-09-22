import { useGetCultivations } from '@/api/generated/cultivations/cultivations';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';

export interface UseFarmCultivationsResult {
  cultivations: CultivationViewModel[];
  /** O ciclo aberto de um talhao, ou `undefined` se ele esta em pousio. */
  openFor: (fieldId: string) => CultivationViewModel | undefined;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
}

/**
 * Os ciclos de todos os talhoes da propriedade.
 *
 * Existe para a tela Hoje e para a lista de talhoes, que precisam dizer o que esta plantado em cada
 * um. Buscar de uma vez e o que evita a lista disparar uma requisicao por linha.
 */
export function useFarmCultivations(farmId: string): UseFarmCultivationsResult {
  const query = useGetCultivations({ farmId }, { query: { enabled: Boolean(farmId) } });
  const cultivations = query.data ?? [];

  return {
    cultivations,
    openFor: (fieldId: string) =>
      cultivations.find((cycle) => cycle.fieldId === fieldId && cycle.endedOn === null),
    isLoading: Boolean(farmId) && query.isPending,
    error: query.error,
    refetch: () => void query.refetch(),
  };
}
