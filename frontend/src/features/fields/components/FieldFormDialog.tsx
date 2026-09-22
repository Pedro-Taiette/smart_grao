import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
} from '@mui/material';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import { FormTextField } from '@/shared/components/FormTextField';
import type { FieldFormValues } from '../fieldSchema';
import { useFieldForm } from '../hooks/useFieldForm';
import { useSaveField } from '../hooks/useSaveField';

interface FieldFormDialogProps {
  open: boolean;
  farmId: string;
  /** `null` cadastra um talhao novo; um registro edita nome. */
  field: FieldViewModel | null;
  /** Contorno recem-desenhado, no cadastro. Na edicao, o contorno atual do talhao. */
  boundary: GeoJsonPolygon | null;
  onClose: () => void;
}


/**
 * Formulario de talhao — nome.
 *
 * O contorno nao aparece como campo porque nao se digita um poligono: ele chega pronto do mapa,
 * pela prop `boundary`, e segue para a API sem passar pelo formulario.
 */
export function FieldFormDialog({ open, farmId, field, boundary, onClose }: FieldFormDialogProps) {
  const form = useFieldForm(field, open);
  const { saveField, isSaving } = useSaveField();

  const onSubmit = async (values: FieldFormValues) => {
    if (!boundary) return;

    const saved = await saveField({
      fieldId: field?.id,
      farmId,
      name: values.name,
      boundary,
    });

    if (saved) onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <form onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <DialogTitle>{field ? 'Editar talhão' : 'Novo talhão'}</DialogTitle>

        <DialogContent>
          <Stack spacing={2.5} sx={{ mt: 1 }}>
            {!field && (
              <Alert severity="info">
                A área em hectares é calculada pelo sistema a partir do contorno desenhado.
              </Alert>
            )}

            <FormTextField name="name" control={form.control} label="Nome do talhão" autoFocus />

          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={isSaving}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={isSaving || !boundary}>
            {isSaving ? 'Salvando…' : 'Salvar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
