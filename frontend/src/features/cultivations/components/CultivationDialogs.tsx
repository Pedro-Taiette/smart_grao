import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from '@mui/material';
import type { Control, FieldValues, Path } from 'react-hook-form';
import type { CercosporaRiskFactorsViewModel } from '@/api/generated/model/cercosporaRiskFactorsViewModel';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { FormTextField } from '@/shared/components/FormTextField';
import { FormSelectField } from '@/shared/components/FormSelectField';
import { seasonNameFor, type CultivationValues, type RiskFactorsValues, type StageValues } from '../cultivationSchema';
import { useCultivationForm, useStageForm, useClosureForm, useRiskFactorsForm } from '../hooks/useCultivationForms';
import { cropSequenceOptions, hybridReactionOptions, tillageOptions } from '../riskFactorLabels';
import { useGrowthStages } from '../hooks/useGrowthStages';

interface CommonProps { isSaving: boolean; onClose: () => void }
function Actions({ isSaving, onClose }: CommonProps) {
  return <DialogActions sx={{ px: 3, pb: 2 }}>
    <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
    <Button type="submit" variant="contained" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Salvar'}</Button>
  </DialogActions>;
}

function RiskFactorFields<T extends FieldValues>({ control, prefix }: { control: Control<T>; prefix: string }) {
  const name = (field: string) => (prefix + field) as Path<T>;
  return <>
    <FormSelectField name={name('hybridReaction')} control={control} label="O híbrido resiste à cercosporiose?"
      helperText="Está no catálogo da semente." options={hybridReactionOptions} />
    <FormSelectField name={name('cropSequence')} control={control} label="O que estava plantado antes aqui?" options={cropSequenceOptions} />
    <FormSelectField name={name('tillage')} control={control} label="Como foi o plantio?" options={tillageOptions} />
  </>;
}

/**
 * Registrar o plantio de milho.
 *
 * Duas perguntas obrigatorias — qual semente e quando — e tres opcionais que ja vem em "Nao sei".
 * A safra nao e perguntada: sai da data (ver `seasonNameFor`).
 */
export function CultivationDialog(props: CommonProps & {
  riskFactors: CercosporaRiskFactorsViewModel;
  onSave: (values: CultivationValues) => Promise<boolean>;
}) {
  const form = useCultivationForm(props.riskFactors);
  const plantedOn = form.watch('plantedOn');
  const season = /^\d{4}-\d{2}-\d{2}$/.test(plantedOn ?? '') ? seasonNameFor(plantedOn) : null;
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Registrar plantio de milho</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="cultivar" control={form.control} label="Qual híbrido foi plantado?"
          helperText="O nome que está no saco da semente. Ex.: AG 8700 PRO4." autoFocus />
        <FormTextField name="plantedOn" control={form.control} label="Quando foi plantado?" type="date"
          helperText={season ? `Safra ${season}.` : undefined}
          slotProps={{ inputLabel: { shrink: true } }} />
        <Typography variant="subtitle2" sx={{ pt: 1 }}>Ajuda a calcular o risco (opcional)</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: '0 !important' }}>
          Se não souber, deixe "Não sei". Dá para responder depois.
        </Typography>
        <RiskFactorFields control={form.control} prefix="riskFactors." />
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
      <DialogTitle>Em que estágio a lavoura está?</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="observedOn" control={form.control} label="Quando você olhou?" type="date"
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: props.cultivation.plantedOn, max: props.cultivation.endedOn ?? undefined } }} />
        {hasScale
          ? <FormSelectField name="stage" control={form.control} label="Estágio"
              helperText="Conte as folhas com a gola visível: 6 folhas = V6."
              options={stages.map(s => ({ value: s.code, label: `${s.code} — ${s.description}` }))} />
          : <FormTextField name="stage" control={form.control} label="Estágio"
              helperText="Esta cultura não tem escala cadastrada. Informe o código usado pela equipe técnica." />}
        <FormTextField name="notes" control={form.control} label="Observações (opcional)" multiline minRows={2} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

export function RiskFactorsDialog(props: CommonProps & {
  riskFactors: CercosporaRiskFactorsViewModel; onSave: (values: RiskFactorsValues) => Promise<boolean>;
}) {
  const form = useRiskFactorsForm(props.riskFactors);
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Sobre o plantio</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Typography variant="body2" color="text.secondary">
          Essas respostas ajudam a calcular o risco de cercosporiose. Se não souber, deixe "Não sei".
        </Typography>
        <RiskFactorFields control={form.control} prefix="" />
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
      <DialogTitle>Encerrar este plantio</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">Use depois da colheita. O histórico fica guardado, e o talhão fica livre para o próximo plantio.</Alert>
        <FormTextField name="endedOn" control={form.control} label="Quando terminou (colheita)?" type="date"
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: minimum } }} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}
