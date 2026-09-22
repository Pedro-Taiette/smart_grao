import { z } from 'zod';
import { PersonRole } from '@/api/generated/model/personRole';

const date = z.iso.date('Informe uma data válida.');

export const personSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome.').max(120),
  role: z.enum(PersonRole).refine((value): boolean => value !== PersonRole.Undefined, 'Escolha a função.'),
});

export const scheduleSchema = z.object({
  samplingPlanId: z.uuid('Escolha a marcação de pontos que será caminhada.'),
  protocolId: z.uuid('Escolha o protocolo da vistoria.'),
  responsibleId: z.uuid('Escolha quem vai a campo.'),
  scheduledFor: date,
});

export const rescheduleSchema = z.object({
  responsibleId: z.uuid('Escolha quem vai a campo.'),
  scheduledFor: date,
});

export const cancelSchema = z.object({
  reason: z.string().trim().min(1, 'Diga por que a vistoria não aconteceu.').max(500),
});

export type PersonValues = z.infer<typeof personSchema>;
export type ScheduleValues = z.infer<typeof scheduleSchema>;
export type RescheduleValues = z.infer<typeof rescheduleSchema>;
export type CancelValues = z.infer<typeof cancelSchema>;

export function today() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}
