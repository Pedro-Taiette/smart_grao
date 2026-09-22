import { Box, Button, Divider, IconButton, List, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { SamplingPanel } from '@/features/sampling/components/SamplingPanel';
import { SamplingPlanDialog } from '@/features/sampling/components/SamplingPlanDialog';
import { useSamplingWorkspace } from '@/features/sampling/hooks/useSamplingWorkspace';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { QueryBoundary } from '@/shared/components/QueryBoundary';
import { formatHectares } from '@/shared/format';
import { FieldDetailsPanel } from '../components/FieldDetailsPanel';
import { FieldFormDialog } from '../components/FieldFormDialog';
import { FieldListItem } from '../components/FieldListItem';
import { FieldMap } from '../components/FieldMap';
import { useFieldsWorkspace } from '../hooks/useFieldsWorkspace';
import { useCultivations } from '@/features/cultivations/hooks/useCultivations';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { cropLabels } from '../cropLabels';

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
  const cycles = useCultivations(farmId, workspace.selectedFieldId);
  const sampling = useSamplingWorkspace(workspace.selectedFieldId, cycles.selectedId);

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
          overflowY: 'auto',
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

            {/* A amostragem some durante o redesenho: os pontos pertencem ao contorno antigo, e
                oferecer "marcar de novo" no meio de um arrasto geraria a malha sobre uma geometria
                que ainda nao foi salva. */}
            {workspace.redrawingFieldId === null && (
              <>
                <Divider />
                <Box sx={{ p: 2 }}>
                  <Button onClick={() => navigate(`/farms/${farmId}/fields/${workspace.selectedField!.id}/cultivations`)}>
                    Safras e cultivos
                  </Button>
                  <Button onClick={() => navigate(`/farms/${farmId}/fields/${workspace.selectedField!.id}/inspections`)}>
                    Vistorias
                  </Button>
                  <QueryBoundary isLoading={cycles.isLoading} error={cycles.error} onRetry={cycles.refetch}>
                    <TextField select fullWidth size="small" label="Cultivo da amostragem" value={cycles.selectedId}
                      onChange={event => cycles.select(event.target.value)} sx={{ mt: 1 }}>
                      {cycles.cultivations.map(c => <MenuItem key={c.id} value={c.id}>
                        {cropLabels[c.crop]} · {cycleDate(c.plantedOn)}{c.endedOn ? ' · Encerrado' : ''}
                      </MenuItem>)}
                      <MenuItem value="">Histórico sem cultivo vinculado</MenuItem>
                    </TextField>
                    {!cycles.selected && <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Cadastre ou selecione um cultivo para marcar novos pontos.
                    </Typography>}
                    {cycles.selected?.endedOn && <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Cultivo encerrado. Os planos ficam disponíveis para consulta.
                    </Typography>}
                  </QueryBoundary>
                </Box>
                <SamplingPanel
                  plans={sampling.plans}
                  visiblePlanId={sampling.visiblePlanId}
                  isBusy={sampling.isBusy}
                  canGenerate={Boolean(workspace.selectedField.active && cycles.selected && !cycles.selected.endedOn && !cycles.isLoading && !cycles.error)}
                  onGenerate={sampling.openDialog}
                  onSelectPlan={sampling.selectPlan}
                  onRemovePlan={sampling.requestDeletion}
                />
              </>
            )}
          </>
        )}
      </Paper>

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <FieldMap
          fields={workspace.fields}
          farmCenter={workspace.farmCenter}
          selectedFieldId={workspace.selectedFieldId}
          editingFieldId={workspace.redrawingFieldId}
          samplingPoints={sampling.points}
          isSamplingOutdated={sampling.isVisiblePlanOutdated}
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

      {/* Montado so quando aberto: e o que faz a escolha voltar ao padrao a cada vez, sem o dialogo
          precisar de um efeito para se reinicializar. */}
      {workspace.selectedField && cycles.selected && !cycles.selected.endedOn && sampling.isDialogOpen && (
        <SamplingPlanDialog
          allowMonitoring={cycles.selected.crop === 'Soybean'}
          fieldName={workspace.selectedField.name}
          fieldAreaHectares={workspace.selectedField.areaHectares}
          isGenerating={sampling.isGenerating}
          onConfirm={sampling.confirmGeneration}
          onClose={sampling.closeDialog}
        />
      )}

      <ConfirmDialog
        open={sampling.pendingDeletion !== null}
        title="Apagar pontos"
        message="Apagar os pontos desta marcação? O talhão e o contorno não são afetados — você pode marcar de novo quando quiser."
        confirmLabel="Apagar"
        isWorking={sampling.isBusy}
        onConfirm={sampling.confirmDeletion}
        onCancel={() => sampling.requestDeletion(null)}
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
