import { RiskLevel } from '@/api/generated/model/riskLevel';
import { StageWindow } from '@/api/generated/model/stageWindow';
import type { DailyRiskViewModel } from '@/api/generated/model/dailyRiskViewModel';
import type { StageStatusViewModel } from '@/api/generated/model/stageStatusViewModel';
import { cycleDate } from '@/features/cultivations/cultivationSchema';

export const riskLevelLabel: Record<RiskLevel, string> = {
  [RiskLevel.Low]: 'Risco baixo',
  [RiskLevel.Moderate]: 'Risco moderado',
  [RiskLevel.High]: 'Risco alto',
};

/** Rampa ordinal de um tom, validada contra a superficie clara. */
export const riskLevelColor: Record<RiskLevel, string> = {
  [RiskLevel.Low]: '#ee9566',
  [RiskLevel.Moderate]: '#d9581f',
  [RiskLevel.High]: '#93360e',
};

export const noDataColor = '#d6d4cf';

export const riskLevelOrder = [RiskLevel.Low, RiskLevel.Moderate, RiskLevel.High] as const;

export function describeWeather(day: DailyRiskViewModel, windowDays: number) {
  const nights = day.humidNights === 1 ? '1 noite úmida' : `${day.humidNights} noites úmidas`;
  const warm = day.favorableDays === 1 ? '1 dia' : `${day.favorableDays} dias`;
  return `${nights} e ${warm} com temperatura entre 22 e 30 °C nos últimos ${windowDays} dias`;
}

/** O que fazer quando o estádio não sustenta a conta; null quando está tudo em ordem. */
export function stageWarning(stage: StageStatusViewModel): string | null {
  if (stage.window === StageWindow.Unknown && !stage.stage)
    return 'Nenhum estádio registrado. Registre o estádio atual para o sistema saber se a lavoura já está na fase de risco (a partir de V4).';
  if (stage.isStale && stage.stage && stage.observedOn)
    return `O último estádio registrado é ${stage.stage}, de ${cycleDate(stage.observedOn)}. Registre o estádio atual.`;
  if (stage.window === StageWindow.Before)
    return `A lavoura está em ${stage.stage}. O acompanhamento começa em V4.`;
  if (stage.window === StageWindow.After)
    return `A lavoura está em ${stage.stage}, depois de R2. O alerta de vistoria não vale mais para esta safra.`;
  return null;
}

export function riskChipSx(level: RiskLevel) {
  return { bgcolor: 'background.paper', border: 2, borderColor: riskLevelColor[level], fontWeight: 600 };
}
