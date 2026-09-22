import type { ReactNode } from 'react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClientProvider } from '@tanstack/react-query';
import { FarmProvider } from '@/features/farms/FarmContext';
import { NotificationProvider } from '@/shared/notifications/NotificationProvider';
import { queryClient } from './queryClient';
import { theme } from './theme';

/**
 * Todo o contexto global num lugar so, para que `main.tsx` continue com tres linhas.
 *
 * O `FarmProvider` fica aqui, e nao dentro do layout, porque a fazenda selecionada e anterior ao
 * roteamento: qualquer tela ja abre sabendo sobre qual propriedade esta operando.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <NotificationProvider>
          <FarmProvider>{children}</FarmProvider>
        </NotificationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
