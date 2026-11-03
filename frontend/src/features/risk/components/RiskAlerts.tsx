import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { RiskLevel } from '@/api/generated/model/riskLevel';
import { useGetCercosporaAlerts } from '@/api/generated/risk/risk';
import { riskFactorsSummary } from '@/features/cultivations/riskFactorLabels';
import { describeWeather, riskChipSx, riskLevelLabel, stageWarning } from '../riskLabels';

const windowDays = 14;

/** Talhões de milho com risco moderado ou alto hoje. Sem nenhum, o grupo não aparece. */
export function RiskAlerts({ farmId }: { farmId: string }) {
  const navigate = useNavigate();
  const query = useGetCercosporaAlerts({ farmId }, { query: { enabled: Boolean(farmId) } });
  const alerts = (query.data ?? []).filter(alert =>
    alert.suggestsInspection || alert.current?.level === RiskLevel.High || alert.current?.level === RiskLevel.Moderate);
  if (alerts.length === 0) return null;

  return (
    <Box>
      <Typography variant="h6">Risco de cercosporiose</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Talhões onde o clima das últimas duas semanas favorece a doença.
      </Typography>
      <Stack spacing={1.5}>
        {alerts.map(alert => (
          <Paper key={alert.cultivationId} variant="outlined" sx={{ p: 2 }}>
            <Stack direction="row" sx={{ gap: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
              <Box sx={{ minWidth: 0 }}>
                <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Typography variant="subtitle1">{alert.fieldName}</Typography>
                  {alert.current?.level && <Chip size="small" label={riskLevelLabel[alert.current.level]}
                    sx={riskChipSx(alert.current.level)} />}
                </Stack>
                {alert.current && <Typography variant="body2" color="text.secondary">
                  {describeWeather(alert.current, windowDays)}. {riskFactorsSummary(alert.riskFactors)}.
                </Typography>}
                {stageWarning(alert.stage) && <Typography variant="body2" color="warning.dark">{stageWarning(alert.stage)}</Typography>}
              </Box>
              <Stack direction="row" spacing={1}>
                <Button onClick={() => navigate(`/talhoes/${alert.fieldId}?aba=risco`)}>Ver risco</Button>
                {alert.suggestsInspection && <Button variant="contained"
                  onClick={() => navigate(`/talhoes/${alert.fieldId}?aba=vistorias&agendar=cercosporiose`)}>
                  Agendar vistoria
                </Button>}
              </Stack>
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Box>
  );
}
