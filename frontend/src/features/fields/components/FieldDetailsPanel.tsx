import { Box, Button, Divider, Stack, Typography } from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import GestureIcon from '@mui/icons-material/Gesture';
import VisibilityOffOutlinedIcon from '@mui/icons-material/VisibilityOffOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import { formatDistance, formatHectares } from '@/shared/format';
import { cropLabels } from '../cropLabels';

interface FieldDetailsPanelProps {
  field: FieldViewModel;
  isRedrawing: boolean;
  isBusy: boolean;
  hasPendingGeometry: boolean;
  onEdit: () => void;
  onStartRedraw: () => void;
  onSaveRedraw: () => void;
  onCancelRedraw: () => void;
  onToggleStatus: () => void;
  onDelete: () => void;
}

function Measure({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {value}
      </Typography>
    </Box>
  );
}

/**
 * Detalhes do talhao selecionado e as acoes sobre ele.
 *
 * Durante o redesenho o painel troca de conjunto de botoes: fica so salvar ou descartar. Deixar
 * "excluir" ao alcance enquanto o contorno esta sendo arrastado convida ao acidente.
 */
export function FieldDetailsPanel({
  field,
  isRedrawing,
  isBusy,
  hasPendingGeometry,
  onEdit,
  onStartRedraw,
  onSaveRedraw,
  onCancelRedraw,
  onToggleStatus,
  onDelete,
}: FieldDetailsPanelProps) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 600 }}>
        {field.name}
      </Typography>

      <Stack direction="row" spacing={3} sx={{ mb: 2 }}>
        <Measure label="Área" value={formatHectares(field.areaHectares)} />
        <Measure label="Perímetro" value={formatDistance(field.perimeterMeters)} />
        <Measure label="Cultura" value={cropLabels[field.crop]} />
      </Stack>

      <Divider sx={{ mb: 2 }} />

      {isRedrawing ? (
        <Stack spacing={1}>
          <Typography variant="body2" color="text.secondary">
            Arraste os pontos do contorno no mapa e salve quando terminar.
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button
              variant="contained"
              size="small"
              onClick={onSaveRedraw}
              disabled={isBusy || !hasPendingGeometry}
            >
              Salvar contorno
            </Button>
            <Button size="small" onClick={onCancelRedraw} disabled={isBusy}>
              Descartar
            </Button>
          </Stack>
        </Stack>
      ) : (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <Button size="small" startIcon={<EditOutlinedIcon />} onClick={onEdit}>
            Editar
          </Button>
          <Button size="small" startIcon={<GestureIcon />} onClick={onStartRedraw}>
            Redesenhar
          </Button>
          <Button
            size="small"
            startIcon={field.active ? <VisibilityOffOutlinedIcon /> : <VisibilityOutlinedIcon />}
            onClick={onToggleStatus}
            disabled={isBusy}
          >
            {field.active ? 'Desativar' : 'Reativar'}
          </Button>
          <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={onDelete}>
            Excluir
          </Button>
        </Stack>
      )}
    </Box>
  );
}
