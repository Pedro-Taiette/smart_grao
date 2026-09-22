import { z } from 'zod';
import { Crop } from '@/api/generated/model/crop';

const date = z.iso.date('Informe uma data válida.');
export const seasonSchema = z.object({ name: z.string().trim().min(1, 'Informe a safra.').max(80) });
export const cultivationSchema = z.object({
  seasonId: z.uuid('Selecione uma safra.'),
  crop: z.enum(Crop).refine((value): boolean => value !== Crop.Undefined, 'Selecione uma cultura.'),
  cultivar: z.string().trim().min(1, 'Informe a cultivar ou híbrido.').max(120),
  plantedOn: date,
});
export const stageSchema = z.object({
  observedOn: date,
  stage: z.string().trim().min(1, 'Informe o estágio observado.').max(32),
  notes: z.string().trim().max(1000),
});
export const closureSchema = z.object({ endedOn: date });
export type SeasonValues = z.infer<typeof seasonSchema>;
export type CultivationValues = z.infer<typeof cultivationSchema>;
export type StageValues = z.infer<typeof stageSchema>;
export type ClosureValues = z.infer<typeof closureSchema>;

export function today() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

// DateOnly values must not pass through UTC conversion, which can display the previous day.
export function cycleDate(value: string) { return value.split('-').reverse().join('/'); }
