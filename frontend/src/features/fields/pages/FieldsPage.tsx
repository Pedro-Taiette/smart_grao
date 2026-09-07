import { Box, Divider, IconButton, List, Paper, Stack, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { formatHectares } from '@/shared/format';
import { FieldDetailsPanel } from '../components/FieldDetailsPanel';
import { FieldFormDialog } from '../components/FieldFormDialog';
import { FieldListItem } from '../components/FieldListItem';
import { FieldMap } from '../components/FieldMap';
import { useFieldsWorkspace } from '../hooks/useFieldsWorkspace';

/**
 * A tela do Pilar 1: desenhar os talhoes sobre o mapa.
 *
 * Layout e composicao apenas. Dados, estado de interacao e escrita vivem no
 * {@link useFieldsWorkspace}.
 */
export function FieldsPage() {
  const { farmId = '' } = useParams<{ farmId: string }>();
  const navigate = useNavigate();
  const workspace = useFieldsWorkspace(farmId);

  const totalHectares = workspace.fields.reduce((sum, field) => sum + field.areaHectares, 0);

  return (
    <Box sx={{ display: 'flex', height: '100%', minHeight: 0 }}>
      <Paper
        square
        sx={{
          width: 340,
          flexShrink: 0,
          borderTop: 0,
          borderBottom: 0,
          borderLeft: 0,
          display: 'flex',
          flexDirection: 'column',
          minHeight: 0,
        }}
      >
        <Box sx={{ p: 2 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <IconButton size="small" aria-label="Voltar" onClick={() => navigate('/farms')}>
              <ArrowBackIcon fontSize="small" />
            </IconButton>
            <Typography variant="h6" component="h1" noWrap>
              {workspace.farm?.name ?? 'Talhões'}
            </Typography>
          </Stack>

          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {workspace.fields.length} talhão(ões) — {formatHectares(totalHectares)}
          </Typography>
        </Box>

        <Divider />

        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto', p: 1 }}>
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

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <FieldMap
          fields={workspace.fields}
          farmCenter={workspace.farmCenter}
          selectedFieldId={workspace.selectedFieldId}
          editingFieldId={workspace.redrawingFieldId}
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
        message={`Excluir "${workspace.pendingDeletion?.name}"? O histórico do talhão vai junto. Para tirá-lo de operação sem perder nada, use "Desativar".`}
        isWorking={workspace.isBusy}
        onConfirm={workspace.confirmDeletion}
        onCancel={() => workspace.requestDeletion(null)}
      />
    </Box>
  );
}
