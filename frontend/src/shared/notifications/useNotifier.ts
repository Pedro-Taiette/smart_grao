import { useContext } from 'react';
import { getApiErrorMessage } from '@/api/apiErrors';
import { NotificationContext } from './NotificationContext';

/**
 * Acesso aos avisos da aplicacao.
 *
 * `notifyError` recebe o erro cru e traduz pelo catalogo — quem chama nao precisa saber que existe
 * `ProblemDetails`, codigo estavel ou catalogo de mensagens.
 */
export function useNotifier() {
  const context = useContext(NotificationContext);

  if (!context) {
    throw new Error('useNotifier precisa estar dentro de <NotificationProvider>.');
  }

  return {
    notifySuccess: (message: string) => context.notify(message, 'success'),
    notifyError: (error: unknown) => context.notify(getApiErrorMessage(error), 'error'),
  };
}
