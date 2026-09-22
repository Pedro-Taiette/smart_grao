import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { InspectionSummaryViewModel } from '@/api/generated/model/inspectionSummaryViewModel';
import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import {
  daysFromToday, describeDay, describeProgress, formatDay, inspectionStatusColor,
  inspectionStatusLabel,
} from '@/features/inspections/inspectionLabels';

/** O verbo muda com a situacao: o botao diz o que vai acontecer, nao "abrir". */
function actionLabel(status: InspectionStatus): string {
  if (status === InspectionStatus.InProgress) return 'Continuar';
  if (status === InspectionStatus.Scheduled) return 'Começar';
  return 'Ver';
}

/**
 * Uma vistoria como linha de lista — a mesma no painel do dia, na agenda e no talhao.
 *
 * Era um bloco copiado em duas telas com campos ligeiramente diferentes. Unificar importa mais pela
 * leitura do que pelo reuso: a visita precisa ser reconhecivel no mesmo formato em qualquer lugar
 * onde apareca.
 */
export function InspectionRow({ inspection, showField = true, showRelativeDay = false }: {
  inspection: InspectionSummaryViewModel;
  /** Desligado dentro do proprio talhao, onde repetir o nome so ocupa a linha. */
  showField?: boolean;
  /** "Hoje" e "Atrasada ha 3 dias" so fazem sentido no que ainda vai acontecer. */
  showRelativeDay?: boolean;
}) {
  const navigate = useNavigate();
  const isLate = showRelativeDay && daysFromToday(inspection.scheduledFor) < 0;

  return (
    <Paper variant="outlined" sx={{ p: 2, borderColor: isLate ? 'warning.main' : undefined }}>
      <Stack
        direction="row"
        sx={{ gap: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}
      >
        <Box sx={{ minWidth: 0 }}>
          <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
            <Typography variant="subtitle1">
              {showRelativeDay ? describeDay(inspection.scheduledFor) : formatDay(inspection.scheduledFor)}
            </Typography>
            {showField && inspection.fieldName && (
              <Typography variant="subtitle1" color="text.secondary">
                · {inspection.fieldName}
              </Typography>
            )}
            <Chip
              size="small"
              color={inspectionStatusColor[inspection.status]}
              label={inspectionStatusLabel[inspection.status]}
            />
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {inspection.responsibleName}
            {' · '}
            {describeProgress(inspection.visitedPointCount, inspection.pointCount)}
            {inspection.observationCount > inspection.visitedPointCount
              && ` · ${inspection.observationCount - inspection.visitedPointCount} fora da malha`}
          </Typography>

          {inspection.cancellationReason && (
            <Typography variant="body2" color="text.secondary">
              {inspection.cancellationReason}
            </Typography>
          )}
        </Box>

        <Button
          variant={inspection.status === InspectionStatus.Completed ? 'text' : 'contained'}
          onClick={() => navigate(`/vistorias/${inspection.id}`)}
        >
          {actionLabel(inspection.status)}
        </Button>
      </Stack>
    </Paper>
  );
}
