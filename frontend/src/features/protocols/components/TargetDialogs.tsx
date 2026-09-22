import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from '@mui/material';
import type { AutomationCapability } from '@/api/generated/model/automationCapability';
import type { Crop } from '@/api/generated/model/crop';
import type { MonitoringTargetViewModel } from '@/api/generated/model/monitoringTargetViewModel';
import { cropLabels, cropOptions } from '@/features/fields/cropLabels';
import { FormSelectField } from '@/shared/components/FormSelectField';
import { FormTextField } from '@/shared/components/FormTextField';
import { Crop as CropEnum } from '@/api/generated/model/crop';
import {
  automationExplanation, automationLabel, automationOptions, targetKindLabel, targetKindOptions,
} from '../protocolLabels';
import type { TargetValues } from '../protocolSchema';
import { useAutomationForm, useTargetForm } from '../hooks/useProtocolForms';

interface CommonProps { isSaving: boolean; onClose: () => void }

function Actions({ isSaving, onClose }: CommonProps) {
  return <DialogActions sx={{ px: 3, pb: 2 }}>
    <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
    <Button type="submit" variant="contained" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Salvar'}</Button>
  </DialogActions>;
}

export function TargetDialog(props: CommonProps & {
  crop: Crop; onSave: (values: TargetValues) => Promise<boolean>;
}) {
  const form = useTargetForm(props.crop);
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Novo alvo no catálogo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">
          O alvo vale para qualquer lavoura desta cultura — não é de uma fazenda. Ele entra como
          registro manual: a equipe acompanha, sem análise automática.
        </Alert>
        <FormSelectField name="crop" control={form.control} label="Cultura"
          options={cropOptions.filter(c => c !== CropEnum.Undefined).map(c => ({ value: c, label: cropLabels[c] }))} />
        <FormSelectField name="kind" control={form.control} label="Tipo"
          options={targetKindOptions.map(k => ({ value: k, label: targetKindLabel[k] }))} />
        <FormTextField name="commonName" control={form.control} label="Nome comum" helperText="Ex.: Cercosporiose" autoFocus />
        <FormTextField name="scientificName" control={form.control} label="Nome científico" helperText="Ex.: Cercospora zeina" />
        <FormTextField name="code" control={form.control} label="Código"
          helperText="Identificação fixa do alvo, usada pelo sistema. Ex.: cercospora_zeina" />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

/**
 * A escada da automacao.
 *
 * O dialogo mostra o que cada situacao significa em vez de so tres palavras: e a diferenca entre
 * "em validacao" como rotulo e "todo resultado passa por revisao humana" como compromisso. O atalho
 * de registro manual para automacao liberada e recusado pelo backend, e o aviso diz o caminho.
 */
export function AutomationDialog(props: CommonProps & {
  target: MonitoringTargetViewModel;
  onSave: (value: AutomationCapability) => Promise<boolean>;
}) {
  const form = useAutomationForm(props.target.automation);
  const chosen = form.watch('automation');
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values.automation)) props.onClose(); })}>
      <DialogTitle>Situação de {props.target.commonName}</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormSelectField name="automation" control={form.control} label="Situação"
          options={automationOptions.map(a => ({ value: a, label: automationLabel[a] }))} />
        <Typography variant="body2" color="text.secondary">{automationExplanation[chosen]}</Typography>
        {props.target.automation === 'ManualRecord' && chosen === 'AutomationEnabled' && <Alert severity="warning">
          Este alvo ainda não passou pela validação com revisão humana. Coloque-o em validação
          primeiro — liberar a análise automática sem essa etapa apresentaria como diagnóstico algo
          que ninguém conferiu.
        </Alert>}
        {props.target.automation === 'AutomationEnabled' && chosen !== 'AutomationEnabled' && <Alert severity="info">
          A análise automática deste alvo deixa de valer assim que você salvar.
        </Alert>}
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}
