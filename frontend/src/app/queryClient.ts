import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/ApiError';

/**
 * Politica de cache e de repeticao de toda a aplicacao.
 *
 * O `retry` e a decisao que importa: repetir um 404 ou um 409 e inutil — a resposta seria a mesma —
 * e ainda atrasa em segundos a mensagem que o usuario precisa ler. Falha de rede ou erro do
 * servidor, sim, merece uma segunda chance.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.isUserFixable) return false;
        return failureCount < 2;
      },
    },
    mutations: {
      // Uma escrita nunca e repetida automaticamente: sem idempotencia garantida, repetir um POST
      // que talvez tenha chegado criaria o segundo talhao identico.
      retry: false,
    },
  },
});
