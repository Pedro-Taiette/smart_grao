import { useState } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { EmptyState } from '@/shared/components/EmptyState';
import { PageContainer } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { FarmCard } from '../components/FarmCard';
import { FarmFormDialog } from '../components/FarmFormDialog';
import { useFarms } from '../hooks/useFarms';
import { useRemoveFarm } from '../hooks/useRemoveFarm';

/**
 * Lista de fazendas — a porta de entrada do sistema.
 *
 * O unico estado que a pagina carrega e "qual dialogo esta aberto e sobre qual registro". Dados,
 * carregamento e erro vem dos hooks; salvar e excluir tambem.
 */
export function FarmsPage() {
  const navigate = useNavigate();
  const { farms, isLoading, error, refetch } = useFarms();
  const { removeFarm, isRemoving } = useRemoveFarm();

  const [editing, setEditing] = useState<FarmViewModel | null>(null);
  const [isFormOpen, setFormOpen] = useState(false);
  const [pendingDeletion, setPendingDeletion] = useState<FarmViewModel | null>(null);

  const openCreate = () => {
    setEditing(null);
    setFormOpen(true);
  };

  const openEdit = (farm: FarmViewModel) => {
    setEditing(farm);
    setFormOpen(true);
  };

  const confirmDeletion = async () => {
    if (!pendingDeletion) return;
    const removed = await removeFarm(pendingDeletion.id);
    if (removed) setPendingDeletion(null);
  };

  return (
    <PageContainer>
      <Stack direction="row" sx={{ mb: 3, justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h5" component="h1">
            Fazendas
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Cadastre a propriedade para começar a desenhar os talhões.
          </Typography>
        </Box>

        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          Nova fazenda
        </Button>
      </Stack>

      <QueryBoundary isLoading={isLoading} error={error} onRetry={refetch}>
        {farms.length === 0 ? (
          <EmptyState
            title="Nenhuma fazenda cadastrada"
            description="A fazenda é o guarda-chuva dos talhões. Cadastre a primeira para seguir."
            action={
              <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
                Cadastrar fazenda
              </Button>
            }
          />
        ) : (
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' },
            }}
          >
            {farms.map((farm) => (
              <FarmCard
                key={farm.id}
                farm={farm}
                onOpenFields={(selected) => navigate(`/farms/${selected.id}/fields`)}
                onEdit={openEdit}
                onDelete={setPendingDeletion}
              />
            ))}
          </Box>
        )}
      </QueryBoundary>

      <FarmFormDialog open={isFormOpen} farm={editing} onClose={() => setFormOpen(false)} />

      <ConfirmDialog
        open={pendingDeletion !== null}
        title="Excluir fazenda"
        message={`Excluir "${pendingDeletion?.name}"? Isso não pode ser desfeito.`}
        isWorking={isRemoving}
        onConfirm={confirmDeletion}
        onCancel={() => setPendingDeletion(null)}
      />
    </PageContainer>
  );
}
