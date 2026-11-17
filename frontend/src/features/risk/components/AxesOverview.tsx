import type { ReactNode } from 'react';
import { Box, Button, ButtonBase, Paper, Stack, Typography } from '@mui/material';
import CloudIcon from '@mui/icons-material/Cloud';
import SatelliteAltIcon from '@mui/icons-material/SatelliteAlt';
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import InfoIcon from '@mui/icons-material/Info';
import WarningIcon from '@mui/icons-material/Warning';
import ErrorIcon from '@mui/icons-material/Error';
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing';
import { InspectNowButton } from '@/features/inspections/components/InspectNowButton';
import type { AxisSummary, AxisTone, Verdict } from '../axes';

export type AxisId = 'clima' | 'satelite' | 'fotos';

/** Os tres eixos, sempre na mesma ordem, com o mesmo numero, icone e pergunta em qualquer tela. */
export const axes: { id: AxisId; number: number; label: string; question: string; icon: ReactNode }[] = [
  { id: 'clima', number: 1, label: 'Clima', question: 'Quando olhar?', icon: <CloudIcon /> },
  { id: 'satelite', number: 2, label: 'Satélite', question: 'Onde olhar?', icon: <SatelliteAltIcon /> },
  { id: 'fotos', number: 3, label: 'Fotos + IA', question: 'O que é?', icon: <PhotoCameraIcon /> },
];

const toneColor: Record<AxisTone, string> = {
  alert: 'error.main',
  watch: 'warning.main',
  ok: 'success.main',
  idle: 'divider',
};

const severityStyle = {
  error: { color: 'error.main', bg: 'rgba(211, 47, 47, 0.06)', icon: <ErrorIcon color="error" /> },
  warning: { color: 'warning.main', bg: 'rgba(237, 108, 2, 0.07)', icon: <WarningIcon color="warning" /> },
  info: { color: 'info.main', bg: 'rgba(2, 136, 209, 0.06)', icon: <InfoIcon color="info" /> },
  success: { color: 'success.main', bg: 'rgba(46, 125, 50, 0.07)', icon: <CheckCircleIcon color="success" /> },
} as const;

interface Props {
  farmId: string;
  cultivationId: string;
  verdict: Verdict;
  summaries: Record<AxisId, AxisSummary>;
  /** Plantio encerrado: mostra os eixos como historico, sem acao. */
  closed?: boolean;
  compact?: boolean;
  onAxis?: (axis: AxisId) => void;
  onReview?: () => void;
  onStage?: () => void;
  /** Onde a vistoria e pedida, oferece mandar o rover (simulado) no lugar de alguem. */
  onRover?: () => void;
  reviewCount?: number;
}

/**
 * A resposta do talhao em uma olhada: a decisao em cima, os tres eixos embaixo.
 *
 * A decisao e o cruzamento (ver `crossVerdict`); os cartoes numerados mostram de onde ela veio —
 * clima diz quando, satelite diz onde, as fotos com IA dizem o que e. O mesmo componente aparece
 * na tela Hoje (compacto, um por talhao) e no topo da pagina do talhao.
 */
export function AxesOverview(props: Props) {
  const { verdict, closed } = props;
  const style = severityStyle[verdict.severity];
  const actions = closed ? [] : verdict.actions;

  return (
    <Stack spacing={props.compact ? 1.5 : 2}>
      <Box sx={{ p: props.compact ? 1.5 : 2.5, borderRadius: 2, bgcolor: style.bg, borderLeft: 4, borderColor: style.color }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
          <Box sx={{ pt: 0.25 }}>{style.icon}</Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant={props.compact ? 'subtitle1' : 'h6'} component="p" sx={{ fontWeight: 600, lineHeight: 1.3 }}>
              {closed ? 'Plantio encerrado. Veja como clima, satélite e fotos se comportaram na safra.' : verdict.headline}
            </Typography>
            {!closed && verdict.detail && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{verdict.detail}</Typography>
            )}
            {/* Na ordem do cruzamento: a primeira acao e a principal (cheia), as outras vazadas. */}
            {actions.length > 0 && (
              <Stack direction="row" useFlexGap spacing={1} sx={{ mt: 1.5, flexWrap: 'wrap' }}>
                {actions.map((action, index) => {
                  const variant = index === 0 ? 'contained' : 'outlined';
                  if (action === 'inspect') return (
                    <InspectNowButton key={action} farmId={props.farmId} cultivationId={props.cultivationId} variant={variant}
                      color={verdict.severity === 'error' ? 'error' : 'primary'} size={props.compact || index > 0 ? 'medium' : 'large'} />
                  );
                  if (action === 'review' && props.onReview) return (
                    <Button key={action} variant={variant} startIcon={<PhotoCameraIcon />} onClick={props.onReview}>
                      Revisar fotos{props.reviewCount ? ` (${props.reviewCount})` : ''}
                    </Button>
                  );
                  if (action === 'stage' && props.onStage) return (
                    <Button key={action} variant={variant} onClick={props.onStage}>Informar estágio</Button>
                  );
                  return null;
                })}
                {actions.includes('inspect') && props.onRover && (
                  <Button variant="outlined" startIcon={<PrecisionManufacturingIcon />} onClick={props.onRover}>
                    Mandar o rover (simulação)
                  </Button>
                )}
              </Stack>
            )}
          </Box>
        </Stack>
      </Box>

      <Box sx={{ display: 'grid', gap: props.compact ? 1 : 1.5, gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' } }}>
        {axes.map(axis => (
          <AxisCard key={axis.id} axis={axis} summary={props.summaries[axis.id]} compact={props.compact}
            onClick={props.onAxis ? () => props.onAxis!(axis.id) : undefined} />
        ))}
      </Box>
    </Stack>
  );
}

function AxisCard({ axis, summary, compact, onClick }: {
  axis: typeof axes[number];
  summary: AxisSummary;
  compact?: boolean;
  onClick?: () => void;
}) {
  const content = (
    <Stack direction="row" spacing={1.25} sx={{ alignItems: 'flex-start', width: '100%', textAlign: 'left' }}>
      <Box sx={{
        width: 28, height: 28, borderRadius: '50%', flexShrink: 0, display: 'grid', placeItems: 'center',
        bgcolor: summary.tone === 'idle' ? 'grey.200' : toneColor[summary.tone],
        color: summary.tone === 'idle' ? 'text.secondary' : 'common.white',
        '& svg': { fontSize: 18 },
      }}>
        {axis.icon}
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.4 }}>
          {axis.number} · {axis.label} — {axis.question}
        </Typography>
        <Typography variant="subtitle2" sx={{ lineHeight: 1.3 }}>{summary.title}</Typography>
        {!compact && <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>{summary.detail}</Typography>}
      </Box>
    </Stack>
  );

  return (
    <Paper variant="outlined" sx={{ borderTop: 3, borderTopColor: toneColor[summary.tone], overflow: 'hidden' }}>
      {onClick
        ? <ButtonBase onClick={onClick} sx={{ p: compact ? 1.25 : 2, width: '100%', display: 'block' }}>{content}</ButtonBase>
        : <Box sx={{ p: compact ? 1.25 : 2 }}>{content}</Box>}
    </Paper>
  );
}
