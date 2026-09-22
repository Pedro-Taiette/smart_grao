import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material';
import type { Crop } from '@/api/generated/model/crop';
import { Crop as CropEnum } from '@/api/generated/model/crop';
import type { MonitoringTargetViewModel } from '@/api/generated/model/monitoringTargetViewModel';
import { cropLabels, cropOptions } from '@/features/fields/cropLabels';
import { FormSelectField } from '@/shared/components/FormSelectField';
import { FormTextField } from '@/shared/components/FormTextField';
import {
  countUnitLabel, countUnitOptions, plantOrganLabel, plantOrganOptions,
  unitAcceptsReferenceLevel, unitNeedsOrgan,
} from '../protocolLabels';
import type { ProtocolItemValues, ProtocolValues } from '../protocolSchema';
import { useProtocolForm, useProtocolItemForm, useProtocolNameForm } from '../hooks/useProtocolForms';

interface CommonProps { isSaving: boolean; onClose: () => void }

function Actions({ isSaving, onClose }: CommonProps) {
  return <DialogActions sx={{ px: 3, pb: 2 }}>
    <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
    <Button type="submit" variant="contained" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Salvar'}</Button>
  </DialogActions>;
}

export function ProtocolDialog(props: CommonProps & {
  crop: Crop; onSave: (values: ProtocolValues) => Promise<boolean>;
}) {
  const form = useProtocolForm(props.crop);
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Novo protocolo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">
          O protocolo nasce como rascunho da versão 1 e só aceita alvos da cultura escolhida.
          Publique quando estiver pronto — a partir daí ele não muda mais, e alterar exige abrir a
          próxima versão.
        </Alert>
        <FormSelectField name="crop" control={form.control} label="Cultura"
          options={cropOptions.filter(c => c !== CropEnum.Undefined).map(c => ({ value: c, label: cropLabels[c] }))} />
        <FormTextField name="name" control={form.control} label="Nome do protocolo"
          helperText="Ex.: Milho — vistoria padrão" autoFocus />
        <FormTextField name="code" control={form.control} label="Código"
          helperText="Identificação fixa, compartilhada por todas as versões. Ex.: milho_padrao" />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

export function RenameProtocolDialog(props: CommonProps & {
  name: string; onSave: (name: string) => Promise<boolean>;
}) {
  const form = useProtocolNameForm(props.name);
  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values.name)) props.onClose(); })}>
      <DialogTitle>Renomear protocolo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="name" control={form.control} label="Nome do protocolo" autoFocus />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

/**
 * Incluir um alvo na versao.
 *
 * Os campos condicionais seguem a unidade: a armadilha nao observa parte de planta, e o registro de
 * presenca nao tem limiar. Escondê-los evita que alguem preencha algo que o backend vai recusar.
 *
 * O nivel de referencia vem por ultimo e opcional de proposito: so tres dos dezoito alvos de milho
 * tem valor publicado, e o campo de fonte ao lado deixa claro que um numero sem procedencia nao
 * entra. Ver `docs/fase-2-catalogo-alvos.md`.
 */
export function ProtocolItemDialog(props: CommonProps & {
  targets: MonitoringTargetViewModel[];
  onSave: (values: ProtocolItemValues) => Promise<boolean>;
}) {
  const form = useProtocolItemForm(props.targets[0]?.id ?? '');
  const unit = form.watch('unit');
  const needsOrgan = unitNeedsOrgan(unit);
  const acceptsLevel = unitAcceptsReferenceLevel(unit);

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Incluir alvo no protocolo</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormSelectField name="targetId" control={form.control} label="Alvo"
          options={props.targets.map(t => ({ value: t.id, label: t.commonName }))} />
        <FormSelectField name="unit" control={form.control} label="O que será contado"
          options={countUnitOptions.map(u => ({ value: u, label: countUnitLabel[u] }))} />
        {needsOrgan
          ? <FormSelectField name="organ" control={form.control} label="Onde olhar"
              options={plantOrganOptions.map(o => ({ value: o, label: plantOrganLabel[o] }))} />
          : <Alert severity="info">A contagem por armadilha não observa parte da planta.</Alert>}
        <FormTextField name="instructions" control={form.control} label="Como coletar"
          helperText="Instrução que a pessoa vai ler no campo." multiline minRows={3} />
        <FormTextField name="photosRequested" control={form.control} label="Fotos por ponto"
          type="number" slotProps={{ htmlInput: { min: 0, max: 5 } }}
          helperText="De 0 a 5. As fotos são o material das etapas seguintes." />
        {acceptsLevel ? <>
          <FormTextField name="referenceThreshold" control={form.control} label="Nível de referência (opcional)"
            helperText="Deixe em branco quando não houver valor publicado — a maioria dos alvos é decidida pelo nível de dano." />
          <FormTextField name="referenceSource" control={form.control} label="Fonte do nível"
            helperText="De onde veio o número. Obrigatório quando há nível." />
        </> : <Alert severity="info">
          O registro de presença não tem nível de referência: a presença já justifica a ação.
        </Alert>}
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}
