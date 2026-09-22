import { Alert, Paper, Stack } from '@mui/material';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { Crop } from '@/api/generated/model/crop';
import { FieldMap } from '@/features/fields/components/FieldMap';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { SamplingPanel } from './SamplingPanel';
import { SamplingPlanDialog } from './SamplingPlanDialog';
import { useSamplingWorkspace } from '../hooks/useSamplingWorkspace';

/**
 * Os pontos de coleta do ciclo aberto, sobre o mapa do proprio talhao.
 *
 * O mapa aqui e de consulta: quem desenha contorno esta na tela de talhoes, com a fazenda inteira a
 * vista. Nesta pagina o mapa responde uma pergunta so — "onde sao as paradas desta caminhada" — e
 * uma barra de desenho no canto so convidaria a redesenhar o talhao por engano.
 */
export function SamplingSection({ field, cultivation }: {
  field: FieldViewModel;
  /** `null` em talhao sem ciclo aberto: sem cultivo nao ha o que planejar. */
  cultivation: CultivationViewModel | null;
}) {
  const sampling = useSamplingWorkspace(field.id, cultivation?.id ?? '');
  const canGenerate = Boolean(field.active && cultivation && !cultivation.endedOn);

  return (
    <Stack spacing={2}>
      {!cultivation && (
        <Alert severity="info">
          Registre um cultivo neste talhão antes de marcar os pontos. A densidade da malha depende do
          que está plantado.
        </Alert>
      )}

      <Paper variant="outlined" sx={{ height: 380, overflow: 'hidden' }}>
        <FieldMap
          fields={[field]}
          // O centro do proprio talhao: aqui nao ha outro contorno para enquadrar junto.
          farmCenter={[field.center.latitude, field.center.longitude]}
          selectedFieldId={field.id}
          editingFieldId={null}
          samplingPoints={sampling.points}
          isSamplingOutdated={sampling.isVisiblePlanOutdated}
          readOnly
          onSelectField={() => {}}
          onPolygonDrawn={() => {}}
          onGeometryChange={() => {}}
        />
      </Paper>

      <Paper variant="outlined">
        <SamplingPanel
          plans={sampling.plans}
          visiblePlanId={sampling.visiblePlanId}
          isBusy={sampling.isBusy}
          canGenerate={canGenerate}
          onGenerate={sampling.openDialog}
          onSelectPlan={sampling.selectPlan}
          onRemovePlan={sampling.requestDeletion}
        />
      </Paper>

      {/* Montado so quando aberto: e o que faz a escolha voltar ao padrao a cada vez, sem o dialogo
          precisar de um efeito para se reinicializar. */}
      {sampling.isDialogOpen && cultivation && (
        <SamplingPlanDialog
          allowMonitoring={cultivation.crop === Crop.Soybean}
          fieldName={field.name}
          fieldAreaHectares={field.areaHectares}
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
    </Stack>
  );
}
