import type { SamplingMode } from '@/api/generated/model/samplingMode';

/**
 * O vocabulario da amostragem em portugues de lavoura.
 *
 * A regra deste arquivo: a tela nunca mostra "modo", "Monitoring", "Mapping" nem "espacamento". O
 * produtor nao escolhe um modo — ele tem uma duvida, e a duvida e que decide a densidade da malha.
 * Essa e literalmente a tese de `docs/amostragem.md`: as duas perguntas sao tratadas pela literatura
 * com densidades separadas por um fator de dez.
 *
 * Entao a tela pergunta a duvida, e a traducao para o modo acontece aqui.
 */

/** A pergunta que o produtor tem, como ele a faria. */
export const modeQuestion: Record<SamplingMode, string> = {
  Monitoring: 'Preciso aplicar?',
  Mapping: 'Onde estão as manchas?',
};

/** O que a escolha significa na pratica, em uma frase, sem jargao. */
export const modeExplanation: Record<SamplingMode, string> = {
  Monitoring:
    'Poucas paradas, bem espalhadas pelo talhão, para saber se a praga passou do limite que justifica aplicar. Segue a recomendação da Embrapa para o tamanho da sua área.',
  Mapping:
    'Muitas paradas, para enxergar em qual parte do talhão a praga está concentrada — e aplicar só ali.',
};

/** Rotulo curto, para lista e historico. */
export const modeLabel: Record<SamplingMode, string> = {
  Monitoring: 'Preciso aplicar?',
  Mapping: 'Onde estão as manchas?',
};

/**
 * Os tres niveis de detalhe do modo Mapeamento.
 *
 * Sao os espacamentos que o estudo de densidade amostral avaliou de fato — 50, 71 e 100 m —, mas o
 * produtor nao ve metro nenhum: ve quanto vai andar e quanto vai enxergar. O padrao e o de 100 m,
 * que e o sugerido pelo estudo e o que rende cerca de uma parada por hectare.
 */
export interface DetailLevel {
  spacingMeters: number;
  label: string;
  description: string;
}

export const detailLevels: DetailLevel[] = [
  {
    spacingMeters: 100,
    label: 'Padrão',
    description: 'Cerca de 1 parada por hectare. Suficiente para localizar as manchas maiores.',
  },
  {
    spacingMeters: 71,
    label: 'Mais detalhe',
    description: 'Cerca de 2 paradas por hectare. Enxerga manchas menores, e dá mais caminhada.',
  },
  {
    spacingMeters: 50,
    label: 'Máximo detalhe',
    description: 'Cerca de 4 paradas por hectare. O mapa mais preciso, e bem mais caminhada.',
  },
];

export const defaultDetailLevel = detailLevels[0];

/**
 * Como a malha ficou, numa frase.
 *
 * Traduz o espacamento em algo que se enxerga no campo — a distancia de uma parada para a proxima —
 * em vez de repetir o numero cru.
 */
export function describeMesh(pointCount: number, spacingMeters: number): string {
  const paradas = pointCount === 1 ? '1 parada' : `${pointCount} paradas`;
  return `${paradas}, uma a cada ${Math.round(spacingMeters)} metros.`;
}
