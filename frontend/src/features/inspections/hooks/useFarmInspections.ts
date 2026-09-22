import { useGetInspections } from '@/api/generated/inspections/inspections';
import type { InspectionSummaryViewModel } from '@/api/generated/model/inspectionSummaryViewModel';

export interface UseFarmInspectionsResult {
  inspections: InspectionSummaryViewModel[];
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
}

/**
 * Todas as vistorias da propriedade, do agendamento mais recente para o mais antigo.
 *
 * A agenda de quem opera e da fazenda, nao de um talhao: "o que tem para hoje" atravessa todos
 * eles. Antes so existia o recorte por cultivo, e montar essa visao no cliente custaria uma
 * requisicao por talhao cadastrado.
 */
export function useFarmInspections(farmId: string): UseFarmInspectionsResult {
  const query = useGetInspections(
    { farmId },
    { query: { enabled: Boolean(farmId) } },
  );

  return {
    inspections: query.data ?? [],
    isLoading: Boolean(farmId) && query.isPending,
    error: query.error,
    refetch: () => void query.refetch(),
  };
}
