import { RiskLevel } from '@/api/generated/model/riskLevel';
import { StageWindow } from '@/api/generated/model/stageWindow';
import type { DailyRiskViewModel } from '@/api/generated/model/dailyRiskViewModel';
import type { StageStatusViewModel } from '@/api/generated/model/stageStatusViewModel';
import type { CercosporaRiskFactorsViewModel } from '@/api/generated/model/cercosporaRiskFactorsViewModel';
import { riskFactorsSummary } from '@/features/cultivations/riskFactorLabels';
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

/** O porquê do nível, numa frase: o clima e, se houver, o que se sabe do plantio. */
export function describeRisk(day: DailyRiskViewModel, windowDays: number, factors: CercosporaRiskFactorsViewModel) {
  const planting = riskFactorsSummary(factors);
  return `${describeWeather(day, windowDays)}${planting ? `. ${planting}` : ''}.`;
}

/**
 * O que fazer com o nível, em uma frase. O nível sozinho ("moderado") nao diz se e para agir —
 * e essa e a unica pergunta de quem abre a tela.
 */
export function riskAdvice(level: RiskLevel | null | undefined, suggestsInspection: boolean): string {
  if (suggestsInspection) return 'Vale vistoriar agora. Olhe as folhas de baixo.';
  if (level === RiskLevel.High) return 'O clima favorece a doença. Fique de olho nas folhas de baixo.';
  if (level === RiskLevel.Moderate) return 'Fique de olho: se o tempo continuar úmido, o risco sobe.';
  if (level === RiskLevel.Low) return 'Nada a fazer por enquanto.';
  return 'Ainda sem clima suficiente para calcular o risco.';
}

/** O que fazer quando o estágio não sustenta a conta; null quando está tudo em ordem. */
export function stageWarning(stage: StageStatusViewModel): string | null {
  if (stage.window === StageWindow.Unknown && !stage.stage)
    return 'Informe o estágio da lavoura: o risco só vale a partir de V4 (4 folhas).';
  if (stage.isStale && stage.stage && stage.observedOn)
    return `O último estágio informado é ${stage.stage}, de ${cycleDate(stage.observedOn)}. Informe o estágio de hoje.`;
  if (stage.window === StageWindow.Before)
    return `A lavoura está em ${stage.stage}. O acompanhamento começa em V4.`;
  if (stage.window === StageWindow.After)
    return `A lavoura está em ${stage.stage}, depois de R2. O alerta de vistoria não vale mais para esta safra.`;
  return null;
}

export function riskChipSx(level: RiskLevel) {
  return { bgcolor: 'background.paper', border: 2, borderColor: riskLevelColor[level], fontWeight: 600 };
}
