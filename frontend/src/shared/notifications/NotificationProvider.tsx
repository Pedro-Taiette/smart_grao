import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { Alert, Snackbar, type AlertColor } from '@mui/material';
import { NotificationContext } from './NotificationContext';

interface Notification {
  message: string;
  severity: AlertColor;
}

/**
 * Avisos efemeros (sucesso/erro) num unico lugar.
 *
 * Existe para que nenhum hook de feature precise carregar seu proprio estado de snackbar, e para
 * que dois avisos disparados juntos nao briguem por espaco na tela.
 */
export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notification, setNotification] = useState<Notification | null>(null);

  const notify = useCallback((message: string, severity: AlertColor = 'success') => {
    setNotification({ message, severity });
  }, []);

  const value = useMemo(() => ({ notify }), [notify]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Snackbar
        open={notification !== null}
        autoHideDuration={notification?.severity === 'error' ? 8000 : 4000}
        onClose={() => setNotification(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={notification?.severity ?? 'success'}
          variant="filled"
          onClose={() => setNotification(null)}
        >
          {notification?.message}
        </Alert>
      </Snackbar>
    </NotificationContext.Provider>
  );
}
