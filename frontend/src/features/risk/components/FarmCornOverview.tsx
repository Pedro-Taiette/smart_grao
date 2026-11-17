import { useCallback, useEffect, useState } from 'react';
import { Box, Button, Paper, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { useNavigate } from 'react-router-dom';
import { useGetCercosporaAlerts } from '@/api/generated/risk/risk';
import type { CercosporaAlertViewModel } from '@/api/generated/model/cercosporaAlertViewModel';
import { useFields } from '@/features/fields/hooks/useFields';
import { formatHectares } from '@/shared/format';
import { useCultivationAxes } from '../hooks/useCultivationAxes';
import { AxesOverview } from './AxesOverview';

/** O que cada talhao pede agora, em hectares: o talhao inteiro, so as areas do satelite, ou nada. */
interface Attention { area: number; toInspect: number }

/**
 * Cada talhao de milho da fazenda com os tres eixos e a decisao — o centro da tela Hoje.
 *
 * Em cima, o valor do sistema numa linha: quantos hectares ele vigia sozinho e quantos pedem alguem
 * (ou o rover) agora. E a diferenca entre os dois que justifica o sistema — em vez de percorrer a
 * fazenda inteira toda semana, so as areas que clima e satelite apontaram.
 */
export function FarmCornOverview({ farmId }: { farmId: string }) {
  const query = useGetCercosporaAlerts({ farmId }, { query: { enabled: Boolean(farmId) } });
  const { fields } = useFields(farmId);
  const alerts = query.data ?? [];
  const [attention, setAttention] = useState<Record<string, Attention>>({});
  const report = useCallback((id: string, value: Attention) => {
    setAttention(current => current[id]?.area === value.area && current[id]?.toInspect === value.toInspect
      ? current : { ...current, [id]: value });
  }, []);

  if (alerts.length === 0) return null;

  const reported = alerts.map(alert => attention[alert.cultivationId]).filter((v): v is Attention => Boolean(v));
  const watched = reported.reduce((sum, v) => sum + v.area, 0);
  const toInspect = reported.reduce((sum, v) => sum + v.toInspect, 0);

  return (
    <Box>
      <Typography variant="h6">Seus talhões de milho</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Para cada talhão: o clima diz quando olhar, o satélite diz onde, e a IA nas fotos diz o que é.
      </Typography>

      {reported.length === alerts.length && watched > 0 && (
        <Paper variant="outlined" sx={{ p: 2, mb: 2, display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
          <Box>
            <Typography variant="overline" color="text.secondary">Vigiado sozinho</Typography>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>{formatHectares(watched)}</Typography>
            <Typography variant="body2" color="text.secondary">
              Clima e satélite atualizam sem ninguém ir a campo.
            </Typography>
          </Box>
          <Box>
            <Typography variant="overline" color="text.secondary">Pede vistoria agora</Typography>
            <Typography variant="h5" sx={{ fontWeight: 700, color: toInspect > 0 ? 'error.main' : 'success.main' }}>
              {toInspect > 0 ? formatHectares(toInspect) : 'Nada'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {toInspect > 0
                ? `Em vez de percorrer os ${formatHectares(watched)}, só onde clima e satélite apontaram.`
                : 'Nenhum talhão precisa de visita hoje.'}
            </Typography>
          </Box>
        </Paper>
      )}

      <Stack spacing={2}>
        {alerts.map(alert => (
          <FieldCard key={alert.cultivationId} farmId={farmId} alert={alert}
            area={fields.find(f => f.id === alert.fieldId)?.areaHectares ?? 0} onReport={report} />
        ))}
      </Stack>
    </Box>
  );
}

function FieldCard({ farmId, alert, area, onReport }: {
  farmId: string;
  alert: CercosporaAlertViewModel;
  area: number;
  onReport: (cultivationId: string, value: Attention) => void;
}) {
  const navigate = useNavigate();
  const data = useCultivationAxes(alert.cultivationId, { alert });
  const fieldUrl = `/talhoes/${alert.fieldId}`;

  // Com vistoria pedida, a area e a das zonas do satelite (para onde as paradas vao) ou o talhao
  // inteiro, se o satelite nao marcou nada.
  const zonesArea = data.satellite?.recentZonesPass?.zones.reduce((sum, z) => sum + z.areaHectares, 0) ?? 0;
  const toInspect = data.verdict.actions.includes('inspect') ? (zonesArea > 0 ? zonesArea : area) : 0;
  const ready = !data.isLoading && area > 0;
  useEffect(() => {
    if (ready) onReport(alert.cultivationId, { area, toInspect });
  }, [ready, area, toInspect, alert.cultivationId, onReport]);

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 1.5, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>{alert.fieldName}</Typography>
          <Typography variant="body2" color="text.secondary">Milho {alert.cultivar} · {formatHectares(area)}</Typography>
        </Box>
        <Button endIcon={<ArrowForwardIcon />} onClick={() => navigate(fieldUrl)}>Abrir talhão</Button>
      </Stack>
      <AxesOverview
        compact
        farmId={farmId}
        cultivationId={alert.cultivationId}
        verdict={data.verdict}
        summaries={{ clima: data.climate, satelite: data.satelliteSummary, fotos: data.photoSummary }}
        onAxis={axis => navigate(`${fieldUrl}#${axis}`)}
        onReview={() => navigate(`${fieldUrl}#fotos`)}
        onStage={() => navigate(`${fieldUrl}?aba=plantio&registrar=estagio`)}
        onRover={() => navigate(`${fieldUrl}/rover`)}
        reviewCount={data.photos.awaitingReview.length}
      />
    </Paper>
  );
}
