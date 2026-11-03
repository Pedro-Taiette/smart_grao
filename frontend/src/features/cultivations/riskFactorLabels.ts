import { CropSequence } from '@/api/generated/model/cropSequence';
import { HybridReaction } from '@/api/generated/model/hybridReaction';
import { TillageSystem } from '@/api/generated/model/tillageSystem';
import type { CercosporaRiskFactorsViewModel } from '@/api/generated/model/cercosporaRiskFactorsViewModel';

export const hybridReactionLabel: Record<HybridReaction, string> = {
  [HybridReaction.Unknown]: 'Não sei',
  [HybridReaction.Resistant]: 'Resistente',
  [HybridReaction.ModeratelyResistant]: 'Moderadamente resistente',
  [HybridReaction.ModeratelySusceptible]: 'Moderadamente suscetível',
  [HybridReaction.Susceptible]: 'Suscetível',
};

export const cropSequenceLabel: Record<CropSequence, string> = {
  [CropSequence.Unknown]: 'Não sei',
  [CropSequence.CornAfterCorn]: 'Milho',
  [CropSequence.Rotation]: 'Outra cultura',
};

export const tillageLabel: Record<TillageSystem, string> = {
  [TillageSystem.Unknown]: 'Não sei',
  [TillageSystem.NoTill]: 'Plantio direto, sobre a palhada',
  [TillageSystem.Conventional]: 'Convencional, com a terra revirada',
};

const options = <T extends string>(labels: Record<T, string>, order: readonly T[]) =>
  order.map(value => ({ value, label: labels[value] }));

export const hybridReactionOptions = options(hybridReactionLabel, [
  HybridReaction.Susceptible, HybridReaction.ModeratelySusceptible,
  HybridReaction.ModeratelyResistant, HybridReaction.Resistant, HybridReaction.Unknown,
]);
export const cropSequenceOptions = options(cropSequenceLabel, [
  CropSequence.CornAfterCorn, CropSequence.Rotation, CropSequence.Unknown,
]);
export const tillageOptions = options(tillageLabel, [
  TillageSystem.NoTill, TillageSystem.Conventional, TillageSystem.Unknown,
]);

export const unknownRiskFactors: CercosporaRiskFactorsViewModel = {
  hybridReaction: HybridReaction.Unknown, cropSequence: CropSequence.Unknown, tillage: TillageSystem.Unknown,
};

export function riskFactorsSummary(factors: CercosporaRiskFactorsViewModel) {
  const parts = [
    factors.hybridReaction !== HybridReaction.Unknown && `Híbrido ${hybridReactionLabel[factors.hybridReaction].toLowerCase()}`,
    factors.cropSequence === CropSequence.CornAfterCorn && 'milho sobre milho',
    factors.cropSequence === CropSequence.Rotation && 'em rotação',
    factors.tillage === TillageSystem.NoTill && 'plantio direto',
    factors.tillage === TillageSystem.Conventional && 'plantio convencional',
  ].filter(Boolean);
  return parts.length === 0 ? 'Risco de cercosporiose não informado' : parts.join(' · ');
}
