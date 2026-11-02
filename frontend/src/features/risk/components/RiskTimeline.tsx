import { Box, Stack, Tooltip, Typography } from '@mui/material';
import type { DailyRiskViewModel } from '@/api/generated/model/dailyRiskViewModel';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { describeWeather, noDataColor, riskLevelColor, riskLevelLabel, riskLevelOrder } from '../riskLabels';

const cell = 10;
const gap = 2;
const height = 28;

function Swatch({ color, label, hatched }: { color: string; label: string; hatched?: boolean }) {
  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Box sx={{
        width: 12, height: 12, borderRadius: '2px', bgcolor: color,
        backgroundImage: hatched ? 'repeating-linear-gradient(45deg, rgba(255,255,255,.65) 0 2px, transparent 2px 4px)' : undefined,
      }} />
      <Typography variant="caption" color="text.secondary">{label}</Typography>
    </Stack>
  );
}

/** Um quadrado por dia do ciclo; a cor é o nível de risco do dia. */
export function RiskTimeline({ days, windowDays }: { days: DailyRiskViewModel[]; windowDays: number }) {
  const width = days.length * (cell + gap);
  const first = days[0];
  const last = days[days.length - 1];
  if (!first || !last) return null;

  return (
    <Stack spacing={1}>
      <Box>
        <svg width="100%" height={height} viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" role="img" aria-label={`Risco diário de ${cycleDate(first.date)} a ${cycleDate(last.date)}`}>
          <defs>
            <pattern id="forecast" width="4" height="4" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
              <rect width="2" height="4" fill="rgba(255,255,255,0.65)" />
            </pattern>
          </defs>
          {days.map((day, index) => {
            const color = day.level ? riskLevelColor[day.level] : noDataColor;
            const text = day.level
              ? `${cycleDate(day.date)}${day.isForecast ? ' (previsão)' : ''} · ${riskLevelLabel[day.level]} · ${describeWeather(day, windowDays)}`
              : `${cycleDate(day.date)} · sem clima suficiente`;
            return (
              <Tooltip key={day.date} title={text} arrow>
                <g>
                  <rect x={index * (cell + gap) - gap / 2} y={0} width={cell + gap} height={height} fill="transparent" />
                  <rect x={index * (cell + gap)} y={4} width={cell} height={height - 8} rx={2} fill={color} />
                  {day.isForecast && <rect x={index * (cell + gap)} y={4} width={cell} height={height - 8} rx={2} fill="url(#forecast)" />}
                </g>
              </Tooltip>
            );
          })}
        </svg>
      </Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
        <Typography variant="caption" color="text.secondary">{cycleDate(first.date)}</Typography>
        <Typography variant="caption" color="text.secondary">{cycleDate(last.date)}</Typography>
      </Stack>
      <Stack direction="row" useFlexGap spacing={2} sx={{ flexWrap: 'wrap' }}>
        {riskLevelOrder.map(level => <Swatch key={level} color={riskLevelColor[level]} label={riskLevelLabel[level]} />)}
        <Swatch color={noDataColor} label="Sem clima" />
        <Swatch color={riskLevelColor.Moderate} label="Previsão" hatched />
      </Stack>
    </Stack>
  );
}
