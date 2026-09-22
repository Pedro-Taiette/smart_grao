import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import type { SeasonViewModel } from '@/api/generated/model/seasonViewModel';
import { FormTextField } from '@/shared/components/FormTextField';
import { FormSelectField } from '@/shared/components/FormSelectField';
import { cropLabels, cropOptions } from '@/features/fields/cropLabels';
import type { CultivationValues, StageValues } from '../cultivationSchema';
import { useSeasonForm, useCultivationForm, useStageForm, useClosureForm } from '../hooks/useCultivationForms';
import { useGrowthStages } from '../hooks/useGrowthStages';

interface CommonProps { isSaving: boolean; onClose: () => void }
function Actions({ isSaving, onClose }: CommonProps) {
  return <DialogActions sx={{ px: 3, pb: 2 }}>
    <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
    <Button type="submit" variant="contained" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Salvar'}</Button>
  </DialogActions>;
}

export function SeasonDialog(props: CommonProps & { onSave: (name: string) => Promise<boolean> }) {
  const form = useSeasonForm();
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values.name)) props.onClose(); })}>
      <DialogTitle>Nova safra</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="name" control={form.control} label="Nome da safra" helperText="Ex.: 2026/2027 — compartilhada pelos talhões desta fazenda." autoFocus />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

export function CultivationDialog(props: CommonProps & {
  seasons: SeasonViewModel[]; onSave: (values: CultivationValues) => Promise<boolean>;
}) {
  const form = useCultivationForm(props.seasons[0]?.id ?? '');
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Novo cultivo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">Confira os dados antes de salvar. Cada cultivo mantém seu próprio histórico e o plantio deve ocorrer após o encerramento do ciclo anterior.</Alert>
        <FormSelectField name="seasonId" control={form.control} label="Safra" options={props.seasons.map(s => ({ value: s.id, label: s.name }))} />
        <FormSelectField name="crop" control={form.control} label="Cultura" options={cropOptions.filter(c => c !== 'Undefined').map(c => ({ value: c, label: cropLabels[c] }))} />
        <FormTextField name="cultivar" control={form.control} label="Cultivar ou híbrido" />
        <FormTextField name="plantedOn" control={form.control} label="Data do plantio" type="date" slotProps={{ inputLabel: { shrink: true } }} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

/**
 * Milho e soja tem escala fenologica, entao o estadio vira escolha, e nao codigo digitado: quem
 * esta no campo reconhece "6 folhas totalmente expandidas" bem mais facil do que lembra que aquilo
 * se escreve `V6`. Cultura sem escala transcrita mantem o campo de texto.
 */
export function StageDialog(props: CommonProps & {
  cultivation: CultivationViewModel; onSave: (values: StageValues) => Promise<boolean>;
}) {
  const form = useStageForm();
  const { stages, hasScale } = useGrowthStages(props.cultivation.crop);
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Registrar estágio observado</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="observedOn" control={form.control} label="Data da observação" type="date"
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: props.cultivation.plantedOn, max: props.cultivation.endedOn ?? undefined } }} />
        {hasScale
          ? <FormSelectField name="stage" control={form.control} label="Estágio"
              options={stages.map(s => ({ value: s.code, label: `${s.code} — ${s.description}` }))} />
          : <FormTextField name="stage" control={form.control} label="Estágio"
              helperText="Esta cultura não tem escala cadastrada. Informe o código usado pela equipe técnica." />}
        <FormTextField name="notes" control={form.control} label="Observações (opcional)" multiline minRows={2} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

export function ClosureDialog(props: CommonProps & {
  cultivation: CultivationViewModel; onSave: (endedOn: string) => Promise<boolean>;
}) {
  const form = useClosureForm();
  const minimum = props.cultivation.stages[0]?.observedOn ?? props.cultivation.plantedOn;
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values.endedOn)) props.onClose(); })}>
      <DialogTitle>Encerrar cultivo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">O histórico será preservado. Novos planos de amostragem devem ser vinculados ao próximo cultivo.</Alert>
        <FormTextField name="endedOn" control={form.control} label="Data de encerramento" type="date"
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: minimum } }} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}
