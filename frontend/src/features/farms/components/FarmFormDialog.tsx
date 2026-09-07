import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';
import { FormTextField } from '@/shared/components/FormTextField';
import { farmSchema, toSaveFarmPayload, type FarmFormValues } from '../farmSchema';
import { useFarmForm } from '../hooks/useFarmForm';
import { useSaveFarm } from '../hooks/useSaveFarm';

interface FarmFormDialogProps {
  open: boolean;
  /** `null` cadastra; um registro edita. */
  farm: FarmViewModel | null;
  onClose: () => void;
}

/**
 * Formulario de fazenda — so marcacao.
 *
 * Nao ha `useState`, `try/catch`, chamada de API nem regra de validacao aqui: validacao esta no
 * schema, estado do formulario no `useFarmForm` e persistencia no `useSaveFarm`. O que sobra e a
 * decisao de qual campo aparece e onde.
 */
export function FarmFormDialog({ open, farm, onClose }: FarmFormDialogProps) {
  const form = useFarmForm(farm, open);
  const { saveFarm, isSaving } = useSaveFarm();

  const onSubmit = async (values: FarmFormValues) => {
    const payload = toSaveFarmPayload(farmSchema.parse(values));
    const saved = await saveFarm(payload, farm?.id);
    if (saved) onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <form onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <DialogTitle>{farm ? 'Editar fazenda' : 'Nova fazenda'}</DialogTitle>

        <DialogContent>
          <Stack spacing={2.5} sx={{ mt: 1 }}>
            <FormTextField name="name" control={form.control} label="Nome da fazenda" autoFocus />

            <Stack direction="row" spacing={2}>
              <FormTextField name="city" control={form.control} label="Município" />
              <FormTextField
                name="state"
                control={form.control}
                label="UF"
                sx={{ maxWidth: 120 }}
                slotProps={{ htmlInput: { maxLength: 2, style: { textTransform: 'uppercase' } } }}
              />
            </Stack>

            <Typography variant="body2" color="text.secondary">
              Sede da fazenda (opcional). Serve para o mapa abrir já sobre a sua terra em vez de
              sobre o oceano.
            </Typography>

            <Stack direction="row" spacing={2}>
              <FormTextField
                name="latitude"
                control={form.control}
                label="Latitude"
                placeholder="-12.5453"
              />
              <FormTextField
                name="longitude"
                control={form.control}
                label="Longitude"
                placeholder="-55.7211"
              />
            </Stack>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={isSaving}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={isSaving}>
            {isSaving ? 'Salvando…' : 'Salvar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
