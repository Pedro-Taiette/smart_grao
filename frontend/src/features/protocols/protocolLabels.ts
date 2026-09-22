import { AutomationCapability } from '@/api/generated/model/automationCapability';
import { CountUnit } from '@/api/generated/model/countUnit';
import { PlantOrgan } from '@/api/generated/model/plantOrgan';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import { TargetKind } from '@/api/generated/model/targetKind';

/**
 * O vocabulario do catalogo e dos protocolos em portugues.
 *
 * Mesma regra de `samplingLabels.ts`: a tela nunca mostra `AttackedPlantPercentage`,
 * `EarLeaf` nem `UnderValidation`. Cada `Record<Enum, string>` e proposital — se o backend
 * acrescentar um valor, a proxima geracao do cliente quebra a compilacao aqui em vez de mostrar o
 * nome do enum em ingles para a equipe.
 *
 * As frases de automacao sao o ponto delicado da fase 2: elas existem para que um alvo do catalogo
 * nunca seja lido como diagnostico automatico enquanto nao for. Ver `docs/fase-2-protocolos.md`.
 */

export const targetKindLabel: Record<TargetKind, string> = {
  [TargetKind.Undefined]: 'Não definido',
  [TargetKind.Pest]: 'Praga',
  [TargetKind.FoliarDisease]: 'Doença foliar',
};

export const targetKindOptions = [TargetKind.Pest, TargetKind.FoliarDisease] as const;

// ── Capacidade de automacao ──────────────────────────────────────────────────

export const automationLabel: Record<AutomationCapability, string> = {
  [AutomationCapability.ManualRecord]: 'Registro manual',
  [AutomationCapability.UnderValidation]: 'Em validação',
  [AutomationCapability.AutomationEnabled]: 'Automação liberada',
};

/** O que a equipe pode esperar do alvo hoje, numa frase, sem prometer o que nao foi validado. */
export const automationExplanation: Record<AutomationCapability, string> = {
  [AutomationCapability.ManualRecord]:
    'A equipe registra e acompanha este alvo normalmente. Não existe análise automática para ele.',
  [AutomationCapability.UnderValidation]:
    'O modelo está sendo avaliado. Todo resultado passa por revisão humana antes de valer.',
  [AutomationCapability.AutomationEnabled]:
    'O modelo está liberado para uso, mantendo o registro do que foi avaliado em cada imagem.',
};

export const automationColor: Record<AutomationCapability, 'default' | 'warning' | 'success'> = {
  [AutomationCapability.ManualRecord]: 'default',
  [AutomationCapability.UnderValidation]: 'warning',
  [AutomationCapability.AutomationEnabled]: 'success',
};

export const automationOptions = [
  AutomationCapability.ManualRecord,
  AutomationCapability.UnderValidation,
  AutomationCapability.AutomationEnabled,
] as const;

// ── Situacao do protocolo ────────────────────────────────────────────────────

export const protocolStatusLabel: Record<ProtocolStatus, string> = {
  [ProtocolStatus.Draft]: 'Rascunho',
  [ProtocolStatus.Published]: 'Publicado',
  [ProtocolStatus.Retired]: 'Aposentado',
};

export const protocolStatusExplanation: Record<ProtocolStatus, string> = {
  [ProtocolStatus.Draft]: 'Em edição. Nenhuma vistoria usa esta versão ainda.',
  [ProtocolStatus.Published]:
    'Em uso e não muda mais. Para alterar o que se coleta, abra a próxima versão — as vistorias já feitas continuam apontando para a versão em que foram realizadas.',
  [ProtocolStatus.Retired]:
    'Fora de uso para vistorias novas. O histórico que aponta para esta versão continua legível.',
};

export const protocolStatusColor: Record<ProtocolStatus, 'info' | 'success' | 'default'> = {
  [ProtocolStatus.Draft]: 'info',
  [ProtocolStatus.Published]: 'success',
  [ProtocolStatus.Retired]: 'default',
};

// ── Unidade de contagem ──────────────────────────────────────────────────────

/** O que a pessoa anota em campo. O rotulo ja diz o que contar. */
export const countUnitLabel: Record<CountUnit, string> = {
  [CountUnit.Undefined]: '—',
  [CountUnit.AttackedPlantPercentage]: '% de plantas atacadas',
  [CountUnit.AttackedPlantsPer10MetreRow]: 'Plantas atacadas em 10 m de fileira',
  [CountUnit.InsectsPerPlant]: 'Insetos por planta',
  [CountUnit.InsectsPerTenPlants]: 'Insetos em 10 plantas',
  [CountUnit.InsectsPerTrap]: 'Capturas por armadilha',
  [CountUnit.LesionedLeafAreaPercentage]: '% de área foliar lesionada',
  [CountUnit.SeverityScore1To9]: 'Nota de 1 a 9',
  [CountUnit.Presence]: 'Só presença, sem contagem',
};

export const countUnitOptions = [
  CountUnit.AttackedPlantPercentage,
  CountUnit.AttackedPlantsPer10MetreRow,
  CountUnit.InsectsPerPlant,
  CountUnit.InsectsPerTenPlants,
  CountUnit.InsectsPerTrap,
  CountUnit.LesionedLeafAreaPercentage,
  CountUnit.SeverityScore1To9,
  CountUnit.Presence,
] as const;

/**
 * As mesmas regras que o dominio aplica, repetidas aqui para que o formulario **esconda** o campo
 * que seria recusado, em vez de deixar a pessoa preenche-lo e levar um erro no salvar. A decisao
 * continua sendo do backend; isto e so a tela chegando na mesma conclusao antes.
 */
export function unitNeedsOrgan(unit: CountUnit) {
  return unit !== CountUnit.InsectsPerTrap;
}

export function unitAcceptsReferenceLevel(unit: CountUnit) {
  return unit !== CountUnit.Presence;
}

// ── Orgao observado ──────────────────────────────────────────────────────────

export const plantOrganLabel: Record<PlantOrgan, string> = {
  [PlantOrgan.Undefined]: 'Não definido',
  [PlantOrgan.Whorl]: 'Cartucho',
  [PlantOrgan.Leaf]: 'Folha',
  [PlantOrgan.EarLeaf]: 'Folha da espiga',
  [PlantOrgan.StalkBase]: 'Base do colmo',
  [PlantOrgan.Stalk]: 'Colmo',
  [PlantOrgan.Root]: 'Raiz',
  [PlantOrgan.Ear]: 'Espiga',
  [PlantOrgan.Tassel]: 'Pendão',
  [PlantOrgan.Silk]: 'Cabelo da espiga',
  [PlantOrgan.Seedling]: 'Plântula',
  [PlantOrgan.WholePlant]: 'Planta inteira',
};

export const plantOrganOptions = [
  PlantOrgan.Whorl,
  PlantOrgan.Leaf,
  PlantOrgan.EarLeaf,
  PlantOrgan.StalkBase,
  PlantOrgan.Stalk,
  PlantOrgan.Root,
  PlantOrgan.Ear,
  PlantOrgan.Tassel,
  PlantOrgan.Silk,
  PlantOrgan.Seedling,
  PlantOrgan.WholePlant,
] as const;

// ── Frases compostas ─────────────────────────────────────────────────────────

/** Onde olhar e o que anotar, numa linha. */
export function describeCollection(organ: PlantOrgan | null, unit: CountUnit): string {
  const counting = countUnitLabel[unit];
  return organ ? `${plantOrganLabel[organ]} · ${counting}` : counting;
}

/** O limiar como decisao, e nao como numero solto. */
export function describeReferenceLevel(threshold: number, unit: CountUnit): string {
  const number = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(threshold);
  switch (unit) {
    case CountUnit.AttackedPlantPercentage:
      return `Age a partir de ${number}% de plantas atacadas`;
    case CountUnit.LesionedLeafAreaPercentage:
      return `Age a partir de ${number}% de área foliar lesionada`;
    case CountUnit.SeverityScore1To9:
      return `Age a partir da nota ${number}`;
    default:
      return `Age a partir de ${number} — ${countUnitLabel[unit].toLowerCase()}`;
  }
}

export function describePhotos(count: number): string {
  if (count === 0) return 'Sem foto solicitada';
  return count === 1 ? '1 foto por ponto' : `${count} fotos por ponto`;
}
