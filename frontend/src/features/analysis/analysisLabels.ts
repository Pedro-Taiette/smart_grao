import type { AssessmentViewModel } from '@/api/generated/model/assessmentViewModel';
import { LeafLabel } from '@/api/generated/model/leafLabel';

export const leafLabelName: Record<LeafLabel, string> = {
  [LeafLabel.Cercosporiose]: 'Cercosporiose',
  [LeafLabel.Healthy]: 'Folha sadia',
  [LeafLabel.OtherSpots]: 'Outra mancha',
};

/** O que o modelo diz, como pergunta: o resultado nunca é diagnóstico fechado. */
export const modelVerdict: Record<LeafLabel, string> = {
  [LeafLabel.Cercosporiose]: 'Suspeita de cercosporiose — confirme',
  [LeafLabel.Healthy]: 'Sem sinal de cercosporiose nesta folha',
  [LeafLabel.OtherSpots]: 'Outra mancha, não cercosporiose — confirme',
};

export const verdictColor: Record<LeafLabel, 'warning' | 'success' | 'info'> = {
  [LeafLabel.Cercosporiose]: 'warning',
  [LeafLabel.Healthy]: 'success',
  [LeafLabel.OtherSpots]: 'info',
};

export const leafLabelOrder = [LeafLabel.Cercosporiose, LeafLabel.OtherSpots, LeafLabel.Healthy] as const;

export function isSimulated(assessment: AssessmentViewModel) {
  return assessment.modelVersion?.startsWith('simulado') ?? false;
}

export function latestModel(assessments: AssessmentViewModel[] | null | undefined) {
  return [...(assessments ?? [])].reverse().find(a => a.source === 'Model') ?? null;
}

export function latestReview(assessments: AssessmentViewModel[] | null | undefined, model: AssessmentViewModel | null) {
  return model ? [...(assessments ?? [])].reverse().find(a => a.source === 'Human' && a.reviewOf === model.id) ?? null : null;
}

export function percent(value: number | null | undefined) {
  return value == null ? '—' : `${Math.round(value * 100)}%`;
}
