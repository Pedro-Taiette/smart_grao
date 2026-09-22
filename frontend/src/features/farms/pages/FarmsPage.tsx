import { useState } from 'react';
import { Box, Button } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { EmptyState } from '@/shared/components/EmptyState';
import { PageContainer, PageHeader } from '@/shared/components/AppLayout';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { FarmCard } from '../components/FarmCard';
import { FarmFormDialog } from '../components/FarmFormDialog';
import { useFarmContext } from '../useFarmContext';
import { useRemoveFarm } from '../hooks/useRemoveFarm';

/**
 * As propriedades cadastradas.
 *
 * Deixou de ser a porta de entrada: quem opera uma fazenda nao quer comecar por um indice de
 * fazendas. Virou cadastro, em Ajustes, e a troca de propriedade no dia a dia acontece pelo seletor
 * da barra do topo. Escolher uma aqui tambem troca o contexto — e o que torna a tela util para quem
 * tem mais de uma.
 *
 * O unico estado que a pagina carrega e "qual dialogo esta aberto e sobre qual registro". Dados,
 * carregamento e erro vem dos hooks; salvar e excluir tambem.
 */
export function FarmsPage() {
  const navigate = useNavigate();
  const { farms, farmId, isLoading, error, refetch, selectFarm } = useFarmContext();
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
      <PageHeader
        title="Fazendas"
        description="As propriedades. A que estiver selecionada é a que o resto do sistema mostra."
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
            Nova fazenda
          </Button>
        }
      />

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
                isSelected={farm.id === farmId}
                // Trocar de propriedade leva ao mapa dela: ficar na lista depois de escolher
                // esconderia o unico efeito visivel do clique.
                onSelect={(selected) => { selectFarm(selected.id); navigate('/talhoes'); }}
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
