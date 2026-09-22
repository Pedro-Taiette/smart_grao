import { useState } from 'react';
import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate, useParams } from 'react-router-dom';
import type { PersonViewModel } from '@/api/generated/model/personViewModel';
import { PageContainer } from '@/shared/components/AppLayout';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { PersonDialog } from '../components/InspectionDialogs';
import { personRoleLabel } from '../inspectionLabels';
import { usePeople, usePersonActions } from '../hooks/usePeople';

/**
 * A equipe de campo da fazenda.
 *
 * Quem sai da equipe e desativado, nunca excluido: a vistoria que a pessoa fez continua apontando
 * para ela, e apagar a linha levaria junto o registro de quem esteve naquele talhao.
 */
export function TeamPage() {
  const { farmId = '' } = useParams();
  const navigate = useNavigate();
  const { people, isLoading, error, refetch } = usePeople(farmId);
  const actions = usePersonActions(farmId);
  const [editing, setEditing] = useState<PersonViewModel | null>(null);
  const [isCreating, setCreating] = useState(false);

  const active = people.filter(person => person.active);
  const former = people.filter(person => !person.active);

  return (
    <PageContainer>
      <Button sx={{ mb: 2 }} onClick={() => navigate(`/farms/${farmId}/fields`)}>Voltar aos talhões</Button>

      <Stack direction="row" sx={{ mb: 3, justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="h5" component="h1">Equipe</Typography>
          <Typography variant="body2" color="text.secondary">
            Quem pode ser responsável por uma vistoria nesta fazenda.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} disabled={actions.isSaving}
          onClick={() => setCreating(true)}>Nova pessoa</Button>
      </Stack>

      <QueryBoundary isLoading={isLoading} error={error} onRetry={refetch}>
        {people.length === 0 ? (
          <EmptyState
            title="Ninguém cadastrado ainda"
            description="Cadastre quem vai a campo para poder agendar a primeira vistoria."
            action={<Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>Cadastrar pessoa</Button>}
          />
        ) : (
          <Stack spacing={3}>
            <PeopleGroup title="Na equipe" people={active} isSaving={actions.isSaving}
              onEdit={setEditing} onToggle={actions.setActive} />
            {former.length > 0 && <PeopleGroup title="Fora da equipe" people={former}
              isSaving={actions.isSaving} onEdit={setEditing} onToggle={actions.setActive} />}
          </Stack>
        )}
      </QueryBoundary>

      {isCreating && <PersonDialog isSaving={actions.isSaving} person={null}
        onClose={() => setCreating(false)}
        onSave={values => actions.createPerson(values.name, values.role)} />}
      {editing && <PersonDialog isSaving={actions.isSaving} person={editing}
        onClose={() => setEditing(null)}
        onSave={values => actions.updatePerson(editing.id, values.name, values.role)} />}
    </PageContainer>
  );
}

function PeopleGroup({ title, people, isSaving, onEdit, onToggle }: {
  title: string;
  people: PersonViewModel[];
  isSaving: boolean;
  onEdit: (person: PersonViewModel) => void;
  onToggle: (id: string, active: boolean) => void;
}) {
  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 1.5 }}>{title}</Typography>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' } }}>
        {people.map(person => (
          <Paper key={person.id} variant="outlined" sx={{ p: 2 }}>
            <Stack spacing={1}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                <Typography variant="subtitle1">{person.name}</Typography>
                <Chip size="small" label={personRoleLabel[person.role]} />
              </Stack>
              <Stack direction="row" spacing={1}>
                <Button size="small" disabled={isSaving} onClick={() => onEdit(person)}>Editar</Button>
                <Button size="small" disabled={isSaving} color={person.active ? 'error' : 'primary'}
                  onClick={() => onToggle(person.id, !person.active)}>
                  {person.active ? 'Tirar da equipe' : 'Trazer de volta'}
                </Button>
              </Stack>
            </Stack>
          </Paper>
        ))}
      </Box>
    </Box>
  );
}
