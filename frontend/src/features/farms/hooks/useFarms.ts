import { useGetFarms } from '@/api/generated/farms/farms';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';

export interface UseFarmsResult {
  farms: FarmViewModel[];
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
}

/**
 * Lista de fazendas.
 *
 * O componente recebe `farms` ja com valor padrao e nunca ve `data`, `isPending` ou `queryKey`:
 * este hook e a fronteira entre a tela e o TanStack Query. Trocar a fonte de dados — outra rota,
 * outra biblioteca de cache — muda este arquivo e mais nenhum.
 */
export function useFarms(): UseFarmsResult {
  const query = useGetFarms();

  return {
    farms: query.data ?? [],
    isLoading: query.isPending,
    error: query.error,
    refetch: () => void query.refetch(),
  };
}
