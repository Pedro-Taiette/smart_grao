import { RiskLevel } from '@/api/generated/model/riskLevel';
import { SatellitePassStatus } from '@/api/generated/model/satellitePassStatus';
import { LeafLabel } from '@/api/generated/model/leafLabel';
import type { CercosporaRiskViewModel } from '@/api/generated/model/cercosporaRiskViewModel';
import type { CercosporaAlertViewModel } from '@/api/generated/model/cercosporaAlertViewModel';
import type { SatellitePassViewModel } from '@/api/generated/model/satellitePassViewModel';
import type { PhotoViewModel } from '@/api/generated/model/photoViewModel';
import { cycleDate } from '@/features/cultivations/cultivationSchema';
import { latestModel, latestReview } from '@/features/analysis/analysisLabels';
import { riskLevelLabel, stageWarning } from './riskLabels';

/**
 * Os tres eixos da deteccao de cercosporiose, cada um resumido numa frase, e o cruzamento deles.
 *
 * 1. **Clima — quando olhar.** O indice de favorabilidade das ultimas duas semanas.
 * 2. **Satelite — onde olhar.** Areas do talhao com NDRE abaixo do resto.
 * 3. **Fotos + IA — o que e.** O palpite do modelo em cada folha e a revisao de quem olhou.
 *
 * Nenhuma regra nova mora aqui: os niveis, as zonas e os palpites vem prontos do backend. Este
 * arquivo so decide como dizer — e o cruzamento junta frases, nao muda limiar nenhum. A vistoria
 * continua sendo sugerida pelo clima (`suggestsInspection`), como na fase 5; o satelite muda
 * *onde* as paradas caem, nao *se* ha vistoria.
 */

/** Igual a `CreateDirectedPlanCommandHandler.ZoneValidDays`: zona mais velha nao orienta a vistoria. */
export const zoneValidDays = 21;

/** Sentinel-2 revisita a cada ~5 dias: antes disso, buscar de novo nao traz passagem nova. */
export const satelliteRefreshDays = 5;

export type AxisTone = 'ok' | 'watch' | 'alert' | 'idle';

export interface AxisSummary {
  tone: AxisTone;
  /** O estado em poucas palavras ("Risco alto", "2 áreas destoam"). */
  title: string;
  /** O porquê, numa linha. */
  detail: string;
}

// --- 1. Clima -----------------------------------------------------------------------------------

type RiskLike = Pick<CercosporaRiskViewModel, 'current' | 'stage' | 'suggestsInspection'>;

export function summarizeClimate(risk: RiskLike | undefined, windowDays = 14): AxisSummary {
  const level = risk?.current?.level;
  if (!risk || !level) return { tone: 'idle', title: 'Sem clima ainda', detail: 'O clima é buscado sozinho a cada poucas horas.' };
  const nights = risk.current!.humidNights;
  const warm = risk.current!.favorableDays;
  const detail = `${nights} noite(s) úmida(s) e ${warm} dia(s) entre 22 e 30 °C nos últimos ${windowDays} dias.`;
  const tone: AxisTone = risk.suggestsInspection || level === RiskLevel.High ? 'alert'
    : level === RiskLevel.Moderate ? 'watch' : 'ok';
  return { tone, title: riskLevelLabel[level], detail };
}

// --- 2. Satelite --------------------------------------------------------------------------------

export interface SatelliteFacts {
  /** A passagem mais recente com leitura (sem nuvem). */
  lastReading: SatellitePassViewModel | null;
  /** A passagem com areas que destoam ainda valida para orientar a vistoria (ultimas 3 semanas). */
  recentZonesPass: SatellitePassViewModel | null;
  /** Data da passagem mais recente, com ou sem leitura. */
  lastPassDate: string | null;
}

function daysBetween(from: string, to: string) {
  return Math.round((Date.parse(`${to}T12:00:00Z`) - Date.parse(`${from}T12:00:00Z`)) / 86_400_000);
}

export function satelliteFacts(passes: SatellitePassViewModel[], today: string): SatelliteFacts {
  const readings = passes.filter(p => p.status === SatellitePassStatus.Ok);
  const lastReading = readings[readings.length - 1] ?? null;
  const recentZonesPass = [...readings].reverse()
    .find(p => p.zones.length > 0 && daysBetween(p.date, today) <= zoneValidDays) ?? null;
  return { lastReading, recentZonesPass, lastPassDate: passes[passes.length - 1]?.date ?? null };
}

/** Passagem nova provavelmente disponivel: nada buscado ainda, ou a ultima e de mais de 5 dias. */
export function satelliteIsStale(facts: SatelliteFacts, today: string) {
  return facts.lastPassDate === null || daysBetween(facts.lastPassDate, today) > satelliteRefreshDays;
}

export function summarizeSatellite(facts: SatelliteFacts | undefined, state: { isFetching: boolean; failed: boolean }): AxisSummary {
  if (state.isFetching && !facts?.lastPassDate)
    return { tone: 'idle', title: 'Buscando imagens…', detail: 'A primeira busca leva de 1 a 2 minutos.' };
  if (!facts || facts.lastPassDate === null)
    return state.failed
      ? { tone: 'idle', title: 'Sem imagens', detail: 'O serviço de análise não respondeu. Tente de novo mais tarde.' }
      : { tone: 'idle', title: 'Sem imagens ainda', detail: 'As imagens do Sentinel-2 são buscadas sozinhas.' };
  if (facts.recentZonesPass) {
    const zones = facts.recentZonesPass.zones;
    const hectares = zones.reduce((sum, z) => sum + z.areaHectares, 0);
    return {
      tone: 'watch',
      title: zones.length === 1 ? '1 área destoa' : `${zones.length} áreas destoam`,
      detail: `${hectares.toFixed(1).replace('.', ',')} ha com a planta menos vigorosa que o resto, na passagem de ${cycleDate(facts.recentZonesPass.date)}.`,
    };
  }
  if (facts.lastReading && daysBetween(facts.lastReading.date, facts.lastPassDate) <= zoneValidDays)
    return { tone: 'ok', title: 'Nada destoa', detail: `Talhão uniforme na passagem de ${cycleDate(facts.lastReading.date)}.` };
  return { tone: 'idle', title: 'Sem leitura recente', detail: 'As últimas passagens vieram encobertas por nuvem.' };
}

// --- 3. Fotos + IA ------------------------------------------------------------------------------

export interface PhotoFacts {
  total: number;
  /** Ainda na fila do modelo. */
  analyzing: number;
  /** O modelo respondeu e ninguem revisou. */
  awaitingReview: PhotoViewModel[];
  /** Palpite de cercosporiose ainda sem revisao. */
  suspected: number;
  /** Revisadas por alguem como cercosporiose. */
  confirmed: number;
  /** Alguma das fotos acima veio da estacao simulada — e a tela precisa dizer isso. */
  includesSimulated: boolean;
}

export function photoFacts(photos: PhotoViewModel[]): PhotoFacts {
  let analyzing = 0, suspected = 0, confirmed = 0, includesSimulated = false;
  const awaitingReview: PhotoViewModel[] = [];
  for (const photo of photos) {
    const model = latestModel(photo.assessments);
    const human = latestReview(photo.assessments, model);
    if (!model) {
      if (photo.analysisStatus !== 'Failed') analyzing++;
      continue;
    }
    if (photo.isSimulated) includesSimulated = true;
    if (human) {
      if (human.label === LeafLabel.Cercosporiose) confirmed++;
      continue;
    }
    awaitingReview.push(photo);
    if (model.label === LeafLabel.Cercosporiose) suspected++;
  }
  // Suspeitas de cercosporiose primeiro: e a revisao que mais muda a decisao.
  const priority = (photo: PhotoViewModel) => latestModel(photo.assessments)?.label === LeafLabel.Cercosporiose ? 0 : 1;
  awaitingReview.sort((a, b) => priority(a) - priority(b) || b.takenAt.localeCompare(a.takenAt));
  return { total: photos.length, analyzing, awaitingReview, suspected, confirmed, includesSimulated };
}

const simulatedNote = ' Inclui fotos da estação simulada.';

export function summarizePhotos(facts: PhotoFacts | undefined): AxisSummary {
  const summary = describePhotos(facts);
  return facts?.includesSimulated ? { ...summary, detail: summary.detail + simulatedNote } : summary;
}

function describePhotos(facts: PhotoFacts | undefined): AxisSummary {
  if (!facts || facts.total === 0)
    return { tone: 'idle', title: 'Nenhuma foto ainda', detail: 'As fotos entram pela vistoria; a IA analisa cada uma.' };
  if (facts.confirmed > 0)
    return { tone: 'alert', title: `${facts.confirmed} confirmada(s)`, detail: 'Cercosporiose confirmada por quem revisou a foto.' };
  if (facts.suspected > 0)
    return { tone: 'alert', title: `${facts.suspected} suspeita(s)`, detail: `A IA viu sinal de cercosporiose. Revise ${facts.awaitingReview.length === 1 ? 'a foto' : 'as fotos'} para confirmar.` };
  if (facts.awaitingReview.length > 0)
    return { tone: 'watch', title: `${facts.awaitingReview.length} para revisar`, detail: 'A IA não viu cercosporiose. Confirme olhando a foto.' };
  if (facts.analyzing > 0)
    return { tone: 'idle', title: 'Analisando…', detail: `${facts.analyzing} foto(s) na fila da IA.` };
  return { tone: 'ok', title: 'Sem sinal', detail: `${facts.total} foto(s) revisada(s), nenhuma com cercosporiose.` };
}

// --- O cruzamento -------------------------------------------------------------------------------

export type AxisAction = 'inspect' | 'review' | 'stage';

export interface Verdict {
  severity: 'success' | 'info' | 'warning' | 'error';
  headline: string;
  detail: string | null;
  actions: AxisAction[];
}

/**
 * O que fazer, juntando os tres eixos.
 *
 * Ordem: o que ja foi confirmado em foto vem primeiro (e o fato mais forte); depois a vistoria
 * sugerida pelo clima, dizendo se o satelite ja sabe onde; depois o satelite sozinho, que sem clima
 * favoravel pode ser outra coisa (seca, solo); por fim o clima. Revisar fotos entra como acao
 * sempre que houver foto esperando.
 */
export function crossVerdict(
  risk: Pick<CercosporaRiskViewModel, 'current' | 'stage' | 'suggestsInspection'> | Pick<CercosporaAlertViewModel, 'current' | 'stage' | 'suggestsInspection'> | undefined,
  satellite: SatelliteFacts | undefined,
  photos: PhotoFacts | undefined,
): Verdict {
  const actions: AxisAction[] = [];
  const level = risk?.current?.level;
  const zones = satellite?.recentZonesPass?.zones.length ?? 0;
  const review = (photos?.awaitingReview.length ?? 0) > 0;
  const needsStage = risk ? stageWarning(risk.stage) !== null : false;

  if (photos && photos.confirmed > 0) {
    if (review) actions.push('review');
    return {
      severity: 'error',
      headline: `Cercosporiose confirmada em ${photos.confirmed} foto(s) deste plantio.`,
      detail: 'Leve as fotos e o mapa das áreas ao agrônomo para decidir o manejo.'
        + (photos.includesSimulated ? simulatedNote : ''),
      actions,
    };
  }

  if (risk?.suggestsInspection) {
    actions.push('inspect');
    if (review) actions.push('review');
    return {
      severity: 'error',
      headline: zones > 0
        ? `Vistorie agora as ${zones === 1 ? 'área que o satélite marcou' : `${zones} áreas que o satélite marcou`}.`
        : 'Vistorie agora: o clima favorece a doença.',
      detail: zones > 0
        ? 'O clima das últimas duas semanas favorece a doença, e o satélite já sabe onde a lavoura destoa. As paradas vão direto para lá.'
        : 'O satélite não marcou área recente, então as 10 paradas ficam espalhadas pelo talhão.',
      actions,
    };
  }

  const climateWatch = level === RiskLevel.High || level === RiskLevel.Moderate;

  if (photos && photos.suspected > 0) {
    const notes = [
      zones > 0 && climateWatch
        ? `O satélite também marcou ${zones === 1 ? '1 área' : `${zones} áreas`} e o clima começa a favorecer a doença: vale vistoriar depois de revisar.`
        : null,
      photos.includesSimulated ? simulatedNote.trim() : null,
    ].filter(Boolean);
    return {
      severity: 'warning',
      headline: 'A IA viu sinal de cercosporiose. Confirme olhando as fotos.',
      detail: notes.length > 0 ? notes.join(' ') : null,
      actions: ['review', ...(zones > 0 && climateWatch ? ['inspect' as const] : []), ...(needsStage ? ['stage' as const] : [])],
    };
  }

  if (needsStage) actions.push('stage');
  if (review) actions.push('review');

  if (zones > 0 && climateWatch) {
    actions.unshift('inspect');
    return {
      severity: 'warning',
      headline: `Fique de olho: o satélite marcou ${zones === 1 ? '1 área' : `${zones} áreas`} e o clima começa a favorecer a doença.`,
      detail: 'Se quiser, vistorie essas áreas já. O sistema avisa quando o clima pedir.',
      actions,
    };
  }
  if (zones > 0) {
    return {
      severity: 'info',
      headline: `O satélite marcou ${zones === 1 ? '1 área' : `${zones} áreas`}, mas o clima não favorece a cercosporiose agora.`,
      detail: 'A planta menos vigorosa ali pode ser outra coisa — falta d\'água, solo, praga. Vale uma olhada quando passar por perto.',
      actions,
    };
  }
  if (climateWatch) {
    return {
      severity: 'warning',
      headline: 'Fique de olho: se o tempo continuar úmido, o risco sobe.',
      detail: null,
      actions,
    };
  }
  if (level === RiskLevel.Low) {
    return {
      severity: 'success',
      headline: 'Nada a fazer por enquanto.',
      detail: satellite?.lastReading ? 'O clima não favorece a doença e o satélite não marcou nada.' : 'O clima não favorece a doença.',
      actions,
    };
  }
  return { severity: 'info', headline: 'Juntando os dados deste talhão…', detail: 'Clima e satélite chegam sozinhos; volte em alguns minutos.', actions };
}
