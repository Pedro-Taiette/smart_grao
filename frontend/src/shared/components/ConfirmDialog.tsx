import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  isWorking?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Confirmacao para acoes destrutivas. O botao de confirmar e vermelho e nunca e o padrao. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Excluir',
  isWorking = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Dialog open={open} onClose={onCancel} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onCancel} disabled={isWorking}>
          Cancelar
        </Button>
        <Button onClick={onConfirm} color="error" variant="contained" disabled={isWorking}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
