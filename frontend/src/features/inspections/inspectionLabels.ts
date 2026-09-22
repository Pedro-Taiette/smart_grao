import { InspectionStatus } from '@/api/generated/model/inspectionStatus';
import { PersonRole } from '@/api/generated/model/personRole';
import { CountUnit } from '@/api/generated/model/countUnit';
import { countUnitLabel } from '@/features/protocols/protocolLabels';

/**
 * O vocabulario da vistoria em portugues.
 *
 * Mesma regra de `samplingLabels.ts` e `protocolLabels.ts`: a tela nunca mostra `InProgress` nem
 * `Technician`. Cada `Record<Enum, string>` e proposital — um valor novo no backend quebra a
 * compilacao aqui em vez de aparecer em ingles para quem esta no campo.
 */

export const personRoleLabel: Record<PersonRole, string> = {
  [PersonRole.Undefined]: 'Não definida',
  [PersonRole.Agronomist]: 'Agrônomo(a)',
  [PersonRole.Technician]: 'Técnico(a)',
  [PersonRole.Operator]: 'Operador(a)',
  [PersonRole.Producer]: 'Produtor(a)',
  [PersonRole.Other]: 'Outra',
};

export const personRoleOptions = [
  PersonRole.Agronomist,
  PersonRole.Technician,
  PersonRole.Operator,
  PersonRole.Producer,
  PersonRole.Other,
] as const;

export const inspectionStatusLabel: Record<InspectionStatus, string> = {
  [InspectionStatus.Scheduled]: 'Agendada',
  [InspectionStatus.InProgress]: 'Em andamento',
  [InspectionStatus.Completed]: 'Concluída',
  [InspectionStatus.Cancelled]: 'Não realizada',
};

export const inspectionStatusExplanation: Record<InspectionStatus, string> = {
  [InspectionStatus.Scheduled]: 'Ninguém foi a campo ainda. Dá para trocar a data e o responsável.',
  [InspectionStatus.InProgress]: 'Em campo. As paradas registradas entram nesta visita.',
  [InspectionStatus.Completed]: 'Concluída. O que foi registrado nesta visita não muda mais.',
  [InspectionStatus.Cancelled]: 'Não aconteceu. O motivo fica registrado, e a próxima visita começa do zero.',
};

export const inspectionStatusColor: Record<InspectionStatus, 'default' | 'info' | 'success' | 'warning'> = {
  [InspectionStatus.Scheduled]: 'info',
  [InspectionStatus.InProgress]: 'warning',
  [InspectionStatus.Completed]: 'success',
  [InspectionStatus.Cancelled]: 'default',
};

export const inspectionStatusOptions = [
  InspectionStatus.Scheduled,
  InspectionStatus.InProgress,
  InspectionStatus.Completed,
  InspectionStatus.Cancelled,
] as const;

/** Rótulo do campo de contagem, já na unidade que o protocolo pediu para aquele alvo. */
export function countFieldLabel(unit: CountUnit): string {
  return unit === CountUnit.Presence ? 'Encontrou?' : countUnitLabel[unit];
}

/** O que foi anotado, como se lê. */
export function describeCount(value: number | null, detected: boolean, unit: CountUnit): string {
  if (unit === CountUnit.Presence) return detected ? 'Encontrado' : 'Não encontrado';
  if (value === null) return '—';

  const number = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value);
  const isPercentage =
    unit === CountUnit.AttackedPlantPercentage || unit === CountUnit.LesionedLeafAreaPercentage;
  return isPercentage ? `${number}%` : number;
}

/** Progresso da caminhada, sem obrigar quem lê a fazer a conta. */
export function describeProgress(visited: number, total: number): string {
  if (total === 0) return 'Sem paradas planejadas';
  return `${visited} de ${total} paradas`;
}

/** Data e hora em português, sem depender do locale do navegador. */
export function formatMoment(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

/** Datas de calendário não passam por conversão de fuso, que exibiria o dia anterior. */
export function formatDay(value: string): string {
  return value.split('-').reverse().join('/');
}

/** Hoje no calendário local, no formato `aaaa-mm-dd` que a API usa para `DateOnly`. */
export function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * Distância em dias entre uma data de calendário e hoje.
 *
 * Contada sobre o meio-dia UTC dos dois lados: às 23h de um dia de horário de verão, a diferença
 * bruta entre duas meias-noites não dá 24 horas, e o arredondamento erraria o dia.
 */
export function daysFromToday(value: string): number {
  const at = (day: string) => Date.parse(`${day}T12:00:00Z`);
  return Math.round((at(value) - at(today())) / 86_400_000);
}

/**
 * A data como quem opera a lê.
 *
 * "22/09/2026" obriga a conferir o calendário para saber se já passou. O painel do dia precisa
 * responder isso na própria frase.
 */
export function describeDay(value: string): string {
  const days = daysFromToday(value);
  if (days === 0) return 'Hoje';
  if (days === 1) return 'Amanhã';
  if (days === -1) return 'Ontem';
  if (days < 0) return `Atrasada há ${-days} dias`;
  return `Em ${days} dias — ${formatDay(value)}`;
}
