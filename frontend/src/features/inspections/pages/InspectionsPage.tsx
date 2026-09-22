import { useState } from 'react';
import { Alert, Button, MenuItem, Stack, TextField } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';
import { EmptyState } from '@/shared/components/EmptyState';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { useFarmContext } from '@/features/farms/useFarmContext';
import { InspectionRow } from '@/features/today/components/InspectionRow';
import { inspectionStatusLabel, inspectionStatusOptions } from '../inspectionLabels';
import { useFarmInspections } from '../hooks/useFarmInspections';

/**
 * Todas as visitas da propriedade — o historico completo.
 *
 * Antes a lista era por talhao, alcancada de dentro dele. So que "quando foi a ultima vez que
 * alguem passou na fazenda" e "o que a Ana visitou esta semana" sao perguntas que atravessam os
 * talhoes, e nenhuma delas tinha resposta sem abrir um talhao por vez. A lista de um talhao
 * especifico continua existindo, na aba de vistorias da pagina dele.
 */
export function InspectionsPage() {
  const navigate = useNavigate();
  const { farm, farmId } = useFarmContext();
  const [status, setStatus] = useState<InspectionStatus | ''>('');

  const { inspections, isLoading, error, refetch } = useFarmInspections(farmId);
  const visible = status ? inspections.filter(item => item.status === status) : inspections;

  if (!farm) {
    return (
      <PageContainer>
        <Alert
          severity="info"
          action={<Button color="inherit" onClick={() => navigate('/ajustes/fazendas')}>Cadastrar</Button>}
        >
          Cadastre uma fazenda para acompanhar vistorias.
        </Alert>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title="Vistorias"
        description="Cada visita aos talhões desta fazenda, da mais recente para a mais antiga."
      />

      <Stack spacing={2}>
        {/* O agendamento mora no talhao, e nao aqui: agendar exige o cultivo, a malha e o protocolo
            daquele talhao. Um botao nesta tela so abriria um formulario pedindo o talhao primeiro. */}
        <TextField
          select
          size="small"
          label="Situação"
          sx={{ minWidth: 200, alignSelf: 'flex-start' }}
          value={status}
          onChange={event => setStatus(event.target.value as InspectionStatus | '')}
        >
          <MenuItem value="">Todas</MenuItem>
          {inspectionStatusOptions.map(option => (
            <MenuItem key={option} value={option}>{inspectionStatusLabel[option]}</MenuItem>
          ))}
        </TextField>

        <QueryBoundary isLoading={isLoading} error={error} onRetry={refetch}>
          {visible.length === 0 ? (
            <EmptyState
              title={status ? 'Nenhuma vistoria nesta situação' : 'Nenhuma vistoria ainda'}
              description="As vistorias são agendadas dentro do talhão, onde estão o cultivo, os pontos de coleta e o protocolo."
              action={
                <Button variant="contained" onClick={() => navigate('/talhoes')}>
                  Escolher o talhão
                </Button>
              }
            />
          ) : (
            <Stack spacing={1.5}>
              {visible.map(inspection => (
                <InspectionRow key={inspection.id} inspection={inspection} />
              ))}
            </Stack>
          )}
        </QueryBoundary>
      </Stack>
    </PageContainer>
  );
}
