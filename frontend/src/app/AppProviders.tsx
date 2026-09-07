import type { ReactNode } from 'react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClientProvider } from '@tanstack/react-query';
import { NotificationProvider } from '@/shared/notifications/NotificationProvider';
import { queryClient } from './queryClient';
import { theme } from './theme';

/** Todo o contexto global num lugar so, para que `main.tsx` continue com tres linhas. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <NotificationProvider>{children}</NotificationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
