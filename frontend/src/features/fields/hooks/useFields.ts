import { useGetFieldsByFarm } from '@/api/generated/farms/farms';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';

export interface UseFieldsResult {
  fields: FieldViewModel[];
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
}

/** Talhoes de uma fazenda, cada um com o contorno em GeoJSON pronto para o mapa. */
export function useFields(farmId: string): UseFieldsResult {
  const query = useGetFieldsByFarm(farmId, undefined, {
    // Sem id na rota nao ha o que buscar; a consulta fica parada em vez de disparar um 404.
    query: { enabled: Boolean(farmId) },
  });

  return {
    fields: query.data ?? [],
    isLoading: query.isPending,
    error: query.error,
    refetch: () => void query.refetch(),
  };
}
