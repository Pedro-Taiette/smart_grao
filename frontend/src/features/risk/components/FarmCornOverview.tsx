import { Box, Button, Paper, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { useNavigate } from 'react-router-dom';
import { useGetCercosporaAlerts } from '@/api/generated/risk/risk';
import type { CercosporaAlertViewModel } from '@/api/generated/model/cercosporaAlertViewModel';
import { useCultivationAxes } from '../hooks/useCultivationAxes';
import { AxesOverview } from './AxesOverview';

/**
 * Cada talhao de milho da fazenda com os tres eixos e a decisao — o centro da tela Hoje.
 *
 * Antes a tela Hoje so mostrava talhao com risco climatico moderado ou alto; satelite e fotos so
 * apareciam abrindo o talhao. Agora todo plantio de milho em andamento aparece, com clima,
 * satelite e IA lado a lado, e o botao da acao ja no cartao.
 */
export function FarmCornOverview({ farmId }: { farmId: string }) {
  const query = useGetCercosporaAlerts({ farmId }, { query: { enabled: Boolean(farmId) } });
  const alerts = query.data ?? [];
  if (alerts.length === 0) return null;

  return (
    <Box>
      <Typography variant="h6">Seus talhões de milho</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Para cada talhão: o clima diz quando olhar, o satélite diz onde, e a IA nas fotos diz o que é.
      </Typography>
      <Stack spacing={2}>
        {alerts.map(alert => <FieldCard key={alert.cultivationId} farmId={farmId} alert={alert} />)}
      </Stack>
    </Box>
  );
}

function FieldCard({ farmId, alert }: { farmId: string; alert: CercosporaAlertViewModel }) {
  const navigate = useNavigate();
  const data = useCultivationAxes(alert.cultivationId, { alert });
  const fieldUrl = `/talhoes/${alert.fieldId}`;

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 1.5, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>{alert.fieldName}</Typography>
          <Typography variant="body2" color="text.secondary">Milho {alert.cultivar}</Typography>
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
        reviewCount={data.photos.awaitingReview.length}
      />
    </Paper>
  );
}
