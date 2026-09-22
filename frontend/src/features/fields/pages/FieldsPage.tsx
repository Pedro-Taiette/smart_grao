import { Alert, Box, Button, Divider, List, Paper, Typography } from '@mui/material';
import OpenInNewIcon from '@mui/icons-material/ArrowForward';
import { useNavigate } from 'react-router-dom';
import { useFarmContext } from '@/features/farms/useFarmContext';
import { useFarmCultivations } from '@/features/cultivations/hooks/useFarmCultivations';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { formatHectares } from '@/shared/format';
import { FieldDetailsPanel } from '../components/FieldDetailsPanel';
import { FieldFormDialog } from '../components/FieldFormDialog';
import { FieldListItem } from '../components/FieldListItem';
import { FieldMap } from '../components/FieldMap';
import { useFieldsWorkspace } from '../hooks/useFieldsWorkspace';
import { cropLabels } from '../cropLabels';

/**
 * O mapa da propriedade: desenhar, redesenhar e escolher um talhao.
 *
 * A tela perdeu tudo o que nao e o mapa. Cultivos, pontos de coleta e vistorias eram botoes e
 * paineis empilhados nesta barra lateral, cada um levando a outra tela — o que fazia dela um indice
 * disfarcado de mapa. Agora ela faz uma coisa: mostra a fazenda e leva a pagina do talhao.
 */
export function FieldsPage() {
  const navigate = useNavigate();
  const { farm, farmId, isLoading: farmLoading } = useFarmContext();
  const workspace = useFieldsWorkspace(farmId);
  const cultivations = useFarmCultivations(farmId);

  const totalHectares = workspace.fields.reduce((sum, field) => sum + field.areaHectares, 0);

  const describeCycle = (fieldId: string) => {
    const open = cultivations.openFor(fieldId);
    return open ? `${cropLabels[open.crop]} ${open.cultivar}` : null;
  };

  if (!farmLoading && !farm) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert
          severity="info"
          action={<Button color="inherit" onClick={() => navigate('/ajustes/fazendas')}>Cadastrar</Button>}
        >
          Cadastre uma fazenda para desenhar os talhões.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', height: '100%', minHeight: 0, flexDirection: { xs: 'column', md: 'row' } }}>
      <Paper
        square
        sx={{
          width: { xs: '100%', md: 340 },
          flexShrink: 0,
          borderTop: 0,
          borderBottom: 0,
          borderLeft: 0,
          display: 'flex',
          flexDirection: 'column',
          minHeight: 0,
          maxHeight: { xs: '45%', md: 'none' },
          overflowY: 'auto',
        }}
      >
        <Box sx={{ p: 2 }}>
          <Typography variant="h6" component="h1" noWrap>Talhões</Typography>
          <Typography variant="body2" color="text.secondary">
            {workspace.fields.length} talhão(ões) — {formatHectares(totalHectares)}
          </Typography>
        </Box>

        <Divider />

        <Box sx={{ flex: '1 0 120px', p: 1 }}>
          <QueryBoundary
            isLoading={workspace.isLoading}
            error={workspace.error}
            onRetry={workspace.refetch}
          >
            {workspace.fields.length === 0 ? (
              <Box sx={{ p: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  Nenhum talhão ainda. Use a ferramenta de polígono no canto superior direito do
                  mapa para desenhar o primeiro contorno.
                </Typography>
              </Box>
            ) : (
              <List disablePadding>
                {workspace.fields.map((field) => (
                  <FieldListItem
                    key={field.id}
                    field={field}
                    cultivation={describeCycle(field.id)}
                    isSelected={field.id === workspace.selectedFieldId}
                    onSelect={workspace.selectField}
                  />
                ))}
              </List>
            )}
          </QueryBoundary>
        </Box>

        {workspace.selectedField && (
          <>
            <Divider />

            {/* A acao principal da selecao e abrir o talhao — e onde moram cultivo, pontos e
                vistorias. As operacoes sobre o contorno ficam abaixo, porque sao as unicas que
                precisam do mapa ao lado. */}
            {workspace.redrawingFieldId === null && (
              <Box sx={{ p: 2, pb: 0 }}>
                <Button
                  fullWidth
                  variant="contained"
                  endIcon={<OpenInNewIcon />}
                  onClick={() => navigate(`/talhoes/${workspace.selectedField!.id}`)}
                >
                  Abrir talhão
                </Button>
              </Box>
            )}

            <FieldDetailsPanel
              field={workspace.selectedField}
              isRedrawing={workspace.redrawingFieldId === workspace.selectedField.id}
              isBusy={workspace.isBusy}
              hasPendingGeometry={workspace.pendingGeometry !== null}
              onEdit={() => workspace.openEdit(workspace.selectedField!)}
              onStartRedraw={() => workspace.startRedraw(workspace.selectedField!.id)}
              onSaveRedraw={workspace.saveRedraw}
              onCancelRedraw={workspace.cancelRedraw}
              onToggleStatus={workspace.toggleStatus}
              onDelete={() => workspace.requestDeletion(workspace.selectedField)}
            />
          </>
        )}
      </Paper>

      <Box sx={{ flex: 1, minWidth: 0, minHeight: 240 }}>
        <FieldMap
          fields={workspace.fields}
          farmCenter={workspace.farmCenter}
          selectedFieldId={workspace.selectedFieldId}
          editingFieldId={workspace.redrawingFieldId}
          samplingPoints={[]}
          isSamplingOutdated={false}
          onSelectField={workspace.selectField}
          onPolygonDrawn={workspace.handlePolygonDrawn}
          onGeometryChange={workspace.setPendingGeometry}
        />
      </Box>

      <FieldFormDialog
        open={workspace.isFormOpen}
        farmId={farmId}
        field={workspace.editingField}
        boundary={workspace.drawnBoundary}
        onClose={workspace.closeForm}
      />

      <ConfirmDialog
        open={workspace.pendingDeletion !== null}
        title="Excluir talhão"
        message={`Excluir "${workspace.pendingDeletion?.name}"? Talhões com histórico de cultivos ou amostragem não podem ser excluídos. Para tirá-los de operação, use "Desativar".`}
        isWorking={workspace.isBusy}
        onConfirm={workspace.confirmDeletion}
        onCancel={() => workspace.requestDeletion(null)}
      />
    </Box>
  );
}
