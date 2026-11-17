import { z } from 'zod';
import { Crop } from '@/api/generated/model/crop';
import { CropSequence } from '@/api/generated/model/cropSequence';
import { HybridReaction } from '@/api/generated/model/hybridReaction';
import { TillageSystem } from '@/api/generated/model/tillageSystem';

const date = z.iso.date('Informe uma data válida.');
export const riskFactorsSchema = z.object({
  hybridReaction: z.enum(HybridReaction),
  cropSequence: z.enum(CropSequence),
  tillage: z.enum(TillageSystem),
});
export const cultivationSchema = z.object({
  crop: z.enum(Crop).refine((value): boolean => value !== Crop.Undefined, 'Selecione uma cultura.'),
  cultivar: z.string().trim().min(1, 'Informe o híbrido plantado.').max(120),
  plantedOn: date,
  riskFactors: riskFactorsSchema,
});
export const stageSchema = z.object({
  observedOn: date,
  stage: z.string().trim().min(1, 'Escolha o estágio.').max(32),
  notes: z.string().trim().max(1000),
});
export const closureSchema = z.object({ endedOn: date });
export type CultivationValues = z.infer<typeof cultivationSchema>;
export type StageValues = z.infer<typeof stageSchema>;
export type ClosureValues = z.infer<typeof closureSchema>;
export type RiskFactorsValues = z.infer<typeof riskFactorsSchema>;

export function today() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

/**
 * A safra em que um plantio cai, no formato que o produtor usa: "2026/2027".
 *
 * O ano agricola brasileiro vai de julho a junho, entao um milho de setembro de 2026 e da safra
 * 2026/2027, e uma safrinha de fevereiro de 2027 tambem. Antes a safra era um cadastro proprio, que
 * precisava existir antes do primeiro cultivo — um passo a mais que ninguem entendia por que
 * existia. Agora ela sai da data do plantio.
 */
export function seasonNameFor(plantedOn: string) {
  const [year, month] = plantedOn.split('-').map(Number);
  const start = month >= 7 ? year : year - 1;
  return `${start}/${start + 1}`;
}

// DateOnly values must not pass through UTC conversion, which can display the previous day.
export function cycleDate(value: string) { return value.split('-').reverse().join('/'); }
