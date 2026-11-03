import { useEffect } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { PersonViewModel } from '@/api/generated/model/personViewModel';
import { FormSelectField } from '@/shared/components/FormSelectField';
import { FormTextField } from '@/shared/components/FormTextField';
import {
  cancelSchema, personSchema, rescheduleSchema, scheduleSchema, today,
  type CancelValues, type PersonValues, type RescheduleValues, type ScheduleValues,
} from '../inspectionSchema';
import { personRoleLabel, personRoleOptions } from '../inspectionLabels';

interface CommonProps { isSaving: boolean; onClose: () => void }

function Actions({ isSaving, onClose, label = 'Salvar' }: CommonProps & { label?: string }) {
  return <DialogActions sx={{ px: 3, pb: 2 }}>
    <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
    <Button type="submit" variant="contained" disabled={isSaving}>{isSaving ? 'Salvando…' : label}</Button>
  </DialogActions>;
}

export function PersonDialog(props: CommonProps & {
  person: PersonViewModel | null;
  onSave: (values: PersonValues) => Promise<boolean>;
}) {
  const form = useForm<PersonValues>({
    resolver: zodResolver(personSchema),
    defaultValues: { name: props.person?.name ?? '', role: props.person?.role ?? 'Technician' },
  });

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>{props.person ? 'Editar pessoa' : 'Nova pessoa na equipe'}</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormTextField name="name" control={form.control} label="Nome" autoFocus />
        <FormSelectField name="role" control={form.control} label="Função"
          options={personRoleOptions.map(role => ({ value: role, label: personRoleLabel[role] }))} />
      </Stack></DialogContent>
      <Actions {...props} />
    </form>
  </Dialog>;
}

/**
 * Agendar a visita.
 *
 * Os tres seletores sao as tres decisoes da fase 3: quem vai, que caminho vai fazer e o que vai
 * coletar em cada parada. So aparecem opcoes validas — pessoa ativa, protocolo publicado da cultura
 * e malha do cultivo —, porque o backend recusaria o resto e nao faz sentido oferecer.
 */
export function ScheduleInspectionDialog(props: CommonProps & {
  people: PersonViewModel[];
  onSave: (values: ScheduleValues) => Promise<boolean>;
}) {
  const form = useForm<ScheduleValues>({
    resolver: zodResolver(scheduleSchema),
    defaultValues: { responsibleId: props.people[0]?.id ?? '', scheduledFor: today() },
  });
  const firstPerson = props.people[0]?.id;
  useEffect(() => {
    if (firstPerson && !form.getValues('responsibleId')) form.setValue('responsibleId', firstPerson);
  }, [firstPerson, form]);

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="sm" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Agendar vistoria de cercosporiose</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">
          São 10 paradas onde o satélite viu a lavoura destoar nas últimas três semanas — ou espalhadas pelo talhão,
          se não houver área marcada. Em cada parada, olhe 10 plantas, das folhas de baixo até a da espiga.
        </Alert>
        {props.people.length === 0 && <Alert severity="warning">Cadastre alguém na equipe para ir a campo.</Alert>}
        <FormSelectField name="responsibleId" control={form.control} label="Quem vai a campo"
          options={props.people.map(person => ({
            value: person.id, label: `${person.name} · ${personRoleLabel[person.role]}`,
          }))} />
        <FormTextField name="scheduledFor" control={form.control} label="Data" type="date"
          slotProps={{ inputLabel: { shrink: true } }} />
      </Stack></DialogContent>
      <Actions {...props} label="Agendar" />
    </form>
  </Dialog>;
}

export function RescheduleDialog(props: CommonProps & {
  people: PersonViewModel[];
  responsibleId: string;
  scheduledFor: string;
  onSave: (values: RescheduleValues) => Promise<boolean>;
}) {
  const form = useForm<RescheduleValues>({
    resolver: zodResolver(rescheduleSchema),
    defaultValues: { responsibleId: props.responsibleId, scheduledFor: props.scheduledFor },
  });

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Remarcar vistoria</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <FormSelectField name="responsibleId" control={form.control} label="Quem vai a campo"
          options={props.people.map(person => ({ value: person.id, label: person.name }))} />
        <FormTextField name="scheduledFor" control={form.control} label="Data" type="date"
          slotProps={{ inputLabel: { shrink: true } }} />
      </Stack></DialogContent>
      <Actions {...props} label="Remarcar" />
    </form>
  </Dialog>;
}

export function CancelInspectionDialog(props: CommonProps & {
  onSave: (values: CancelValues) => Promise<boolean>;
}) {
  const form = useForm<CancelValues>({ resolver: zodResolver(cancelSchema), defaultValues: { reason: '' } });

  return <Dialog open onClose={props.isSaving ? undefined : props.onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={form.handleSubmit(async values => { if (await props.onSave(values)) props.onClose(); })}>
      <DialogTitle>Vistoria não realizada</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Alert severity="info">
          O motivo fica registrado no histórico. A próxima visita a este cultivo começa do zero.
        </Alert>
        <FormTextField name="reason" control={form.control} label="Por que não aconteceu?"
          multiline minRows={2} autoFocus helperText="Ex.: chuva forte; não deu para entrar no talhão." />
      </Stack></DialogContent>
      <Actions {...props} label="Registrar" />
    </form>
  </Dialog>;
}
