import { useState } from 'react';
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Radio, RadioGroup, Stack,
  TextField, Typography, type ButtonProps,
} from '@mui/material';
import DirectionsWalkIcon from '@mui/icons-material/DirectionsWalk';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  getGetInspectionsQueryKey, useGetInspections, useScheduleInspection, useStartInspection,
} from '@/api/generated/inspections/inspections';
import { getGetPeopleQueryKey, useCreatePerson } from '@/api/generated/people/people';
import { useGetProtocols } from '@/api/generated/protocols/protocols';
import { useCreateDirectedPlan } from '@/api/generated/photos/photos';
import { Crop } from '@/api/generated/model/crop';
import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import { PersonRole } from '@/api/generated/model/personRole';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import { useNotifier } from '@/shared/notifications/useNotifier';
import { daysFromToday, today } from '../inspectionLabels';
import { usePeople } from '../hooks/usePeople';

const responsibleKey = 'smartgrao.responsible';

function storedResponsible() {
  try { return window.localStorage.getItem(responsibleKey); } catch { return null; }
}
function storeResponsible(id: string) {
  try { window.localStorage.setItem(responsibleKey, id); } catch { /* sem storage, segue */ }
}

/**
 * "Vistoriar agora": do talhao ate a primeira parada em um clique.
 *
 * Antes eram cinco passos — agendar, escolher pessoa e data, abrir a vistoria, iniciar, registrar a
 * parada — e um sexto se ninguem estivesse cadastrado na equipe (ir a Ajustes, voltar). Agora o
 * botao cria as 10 paradas dirigidas pelo satelite, agenda para hoje, inicia e abre a primeira
 * parada. So pergunta quando nao tem como decidir sozinho:
 *
 * - **ninguem na equipe:** pede o nome de quem vai e cadastra na hora;
 * - **mais de uma pessoa:** pergunta quem vai (a ultima escolha ja vem marcada).
 *
 * Se ja ha vistoria em andamento, o botao continua ela; se ha uma agendada para hoje ou atrasada,
 * comeca ela. Nunca cria uma segunda visita por cima da que ja existe.
 */
export function InspectNowButton({ farmId, cultivationId, ...buttonProps }: {
  farmId: string;
  cultivationId: string;
} & Omit<ButtonProps, 'onClick'>) {
  const navigate = useNavigate();
  const cache = useQueryClient();
  const notifier = useNotifier();
  const inspectionsQuery = useGetInspections({ cultivationId }, { query: { enabled: Boolean(cultivationId) } });
  const { people, isLoading: peopleLoading } = usePeople(farmId, true);
  const protocolsQuery = useGetProtocols({ crop: Crop.Corn, status: ProtocolStatus.Published });
  const directedPlan = useCreateDirectedPlan();
  const schedule = useScheduleInspection();
  const start = useStartInspection();
  const createPerson = useCreatePerson();
  const [asking, setAsking] = useState(false);
  const [working, setWorking] = useState(false);

  const inspections = inspectionsQuery.data ?? [];
  const running = inspections.find(i => i.status === InspectionStatus.InProgress);
  const due = inspections
    .filter(i => i.status === InspectionStatus.Scheduled && daysFromToday(i.scheduledFor) <= 0)
    .sort((a, b) => a.scheduledFor.localeCompare(b.scheduledFor))[0];
  const protocol = (protocolsQuery.data ?? []).find(p => p.code === 'cercosporiose_deteccao') ?? protocolsQuery.data?.[0];

  const label = running ? 'Continuar a vistoria' : due ? 'Começar a vistoria agendada' : 'Vistoriar agora';

  async function refreshCaches() {
    await cache.invalidateQueries({ queryKey: getGetInspectionsQueryKey() });
  }

  async function go(responsibleId: string) {
    if (!protocol) {
      notifier.notifyError(new Error('Nenhum protocolo de cercosporiose publicado.'));
      return;
    }
    setWorking(true);
    try {
      const plan = await directedPlan.mutateAsync({ id: cultivationId });
      const created = await schedule.mutateAsync({
        data: { cultivationId, samplingPlanId: plan.plan.id, protocolId: protocol.id, responsibleId, scheduledFor: today() },
      });
      await start.mutateAsync({ id: created.id });
      await refreshCaches();
      notifier.notifySuccess('Vistoria iniciada. Vá até a parada 1.');
      navigate(`/vistorias/${created.id}?registrar=parada`);
    } catch (failure) {
      notifier.notifyError(failure);
    } finally {
      setWorking(false);
    }
  }

  async function onClick() {
    if (running) {
      navigate(`/vistorias/${running.id}?registrar=parada`);
      return;
    }
    if (due) {
      setWorking(true);
      try {
        await start.mutateAsync({ id: due.id });
        await refreshCaches();
        navigate(`/vistorias/${due.id}?registrar=parada`);
      } catch (failure) {
        notifier.notifyError(failure);
      } finally {
        setWorking(false);
      }
      return;
    }
    if (people.length === 1) {
      void go(people[0].id);
      return;
    }
    setAsking(true);
  }

  async function confirm(choice: { personId?: string; newName?: string }) {
    let responsibleId = choice.personId;
    if (!responsibleId && choice.newName) {
      try {
        const person = await createPerson.mutateAsync({ data: { farmId, name: choice.newName, role: PersonRole.Producer } });
        await cache.invalidateQueries({ queryKey: getGetPeopleQueryKey() });
        responsibleId = person.id;
      } catch (failure) {
        notifier.notifyError(failure);
        return;
      }
    }
    if (!responsibleId) return;
    storeResponsible(responsibleId);
    setAsking(false);
    await go(responsibleId);
  }

  return <>
    <Button variant="contained" startIcon={<DirectionsWalkIcon />} {...buttonProps}
      disabled={buttonProps.disabled || working || inspectionsQuery.isPending || peopleLoading}
      onClick={() => void onClick()}>
      {working ? 'Preparando a vistoria…' : label}
    </Button>
    {asking && <WhoGoesDialog
      people={people}
      isSaving={working || createPerson.isPending}
      onClose={() => setAsking(false)}
      onConfirm={choice => void confirm(choice)} />}
  </>;
}

function WhoGoesDialog({ people, isSaving, onClose, onConfirm }: {
  people: { id: string; name: string }[];
  isSaving: boolean;
  onClose: () => void;
  onConfirm: (choice: { personId?: string; newName?: string }) => void;
}) {
  const remembered = storedResponsible();
  const [personId, setPersonId] = useState(
    people.find(p => p.id === remembered)?.id ?? people[0]?.id ?? '');
  const [name, setName] = useState('');
  const isNew = people.length === 0;

  return <Dialog open onClose={isSaving ? undefined : onClose} maxWidth="xs" fullWidth>
    <form noValidate onSubmit={event => {
      event.preventDefault();
      if (isNew) { if (name.trim()) onConfirm({ newName: name.trim() }); }
      else if (personId) onConfirm({ personId });
    }}>
      <DialogTitle>Quem vai a campo?</DialogTitle>
      <DialogContent>
        {isNew
          ? <Stack spacing={1.5} sx={{ pt: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Escreva seu nome (ou o de quem vai). Fica guardado para as próximas vistorias.
              </Typography>
              <TextField label="Nome" value={name} onChange={e => setName(e.target.value)} autoFocus />
            </Stack>
          : <RadioGroup value={personId} onChange={e => setPersonId(e.target.value)}>
              {people.map(p => <FormControlLabel key={p.id} value={p.id} control={<Radio />} label={p.name} />)}
            </RadioGroup>}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={isSaving}>Cancelar</Button>
        <Button type="submit" variant="contained" disabled={isSaving || (isNew ? !name.trim() : !personId)}>
          {isSaving ? 'Preparando…' : 'Começar'}
        </Button>
      </DialogActions>
    </form>
  </Dialog>;
}
