import {
  Alert,
  Box,
  Button,
  Chip,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined';
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined';
import type { SamplingPlanSummaryViewModel } from '@/api/generated/model/samplingPlanSummaryViewModel';
import { formatDate } from '@/shared/format';
import { describeMesh, modeLabel } from '../samplingLabels';

interface SamplingPanelProps {
  plans: SamplingPlanSummaryViewModel[];
  visiblePlanId: string | null;
  isBusy: boolean;
  onGenerate: () => void;
  onSelectPlan: (planId: string) => void;
  onRemovePlan: (plan: SamplingPlanSummaryViewModel) => void;
}

/**
 * Os pontos de amostragem do talhao selecionado.
 *
 * Mostra o plano em uso e, quando ha mais de um, os anteriores. Tudo em frases — o painel nunca
 * exibe espacamento em metros isolado, bordadura nem contagem alvo crua: esses numeros existem no
 * contrato para o sistema, nao para o produtor.
 */
export function SamplingPanel({
  plans,
  visiblePlanId,
  isBusy,
  onGenerate,
  onSelectPlan,
  onRemovePlan,
}: SamplingPanelProps) {
  const current = plans.find((plan) => plan.id === visiblePlanId) ?? plans[0] ?? null;
  const previous = plans.filter((plan) => plan.id !== current?.id);

  if (!current) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          Nenhum ponto de amostragem marcado neste talhão.
        </Typography>
        <Button
          variant="contained"
          size="small"
          startIcon={<PlaceOutlinedIcon />}
          onClick={onGenerate}
          disabled={isBusy}
        >
          Marcar pontos
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
        <Typography variant="subtitle2">Pontos de amostragem</Typography>
        <Chip label={modeLabel[current.mode]} size="small" />
      </Stack>

      <Typography variant="body2" sx={{ mb: 0.5 }}>
        {describeMesh(current.pointCount, current.spacingMeters)}
      </Typography>

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5 }}>
        Marcados em {formatDate(current.createdAt)}
      </Typography>

      {current.fallsShortOfTarget && (
        <Alert severity="warning" sx={{ mb: 1.5 }}>
          Este talhão é pequeno demais para as {current.targetPointCount} paradas que a recomendação
          pede. Couberam {current.pointCount}, o máximo que a área comporta.
        </Alert>
      )}

      {current.subdivisionRecommended && (
        <Alert severity="info" sx={{ mb: 1.5 }}>
          Talhão grande. A recomendação da Embrapa para áreas acima de 100 ha é dividir em partes
          menores e amostrar cada uma separadamente.
        </Alert>
      )}

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
        <Button size="small" startIcon={<PlaceOutlinedIcon />} onClick={onGenerate} disabled={isBusy}>
          Marcar de novo
        </Button>
        <Button
          size="small"
          color="error"
          startIcon={<DeleteOutlineIcon />}
          onClick={() => onRemovePlan(current)}
          disabled={isBusy}
        >
          Apagar
        </Button>
      </Stack>

      {previous.length > 0 && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">
            Marcações anteriores
          </Typography>
          <List dense disablePadding>
            {previous.map((plan) => (
              <ListItemButton key={plan.id} onClick={() => onSelectPlan(plan.id)} sx={{ px: 1 }}>
                <ListItemText
                  primary={`${formatDate(plan.createdAt)} — ${plan.pointCount} paradas`}
                  secondary={modeLabel[plan.mode]}
                  slotProps={{
                    primary: { variant: 'body2' },
                    secondary: { variant: 'caption' },
                  }}
                />
              </ListItemButton>
            ))}
          </List>
        </Box>
      )}
    </Box>
  );
}
