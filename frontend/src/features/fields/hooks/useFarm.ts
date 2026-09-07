import { useGetFarmById } from '@/api/generated/farms/farms';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';

export interface UseFarmResult {
  farm: FarmViewModel | undefined;
  isLoading: boolean;
  error: unknown;
}

/** Uma fazenda pelo id — usada pela tela do mapa para o titulo e para centralizar a visao. */
export function useFarm(farmId: string): UseFarmResult {
  const query = useGetFarmById(farmId, { query: { enabled: Boolean(farmId) } });

  return {
    farm: query.data,
    isLoading: query.isPending,
    error: query.error,
  };
}
