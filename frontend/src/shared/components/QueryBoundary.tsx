import type { ReactNode } from 'react';
import { Alert, Box, Button, CircularProgress } from '@mui/material';
import { getApiErrorMessage } from '@/api/apiErrors';

interface QueryBoundaryProps {
  isLoading: boolean;
  error: unknown;
  onRetry?: () => void;
  children: ReactNode;
}

/**
 * Os tres estados de uma consulta — carregando, falhou, pronto — num componente so.
 *
 * Existe porque essa tripla se repete em toda tela, e escrever `if (isLoading) ... if (error) ...`
 * a cada pagina e como o "carregando" eterno nasce: basta alguem esquecer um dos ramos.
 */
export function QueryBoundary({ isLoading, error, onRetry, children }: QueryBoundaryProps) {
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert
        severity="error"
        action={
          onRetry ? (
            <Button color="inherit" size="small" onClick={onRetry}>
              Tentar de novo
            </Button>
          ) : undefined
        }
      >
        {getApiErrorMessage(error)}
      </Alert>
    );
  }

  return <>{children}</>;
}
