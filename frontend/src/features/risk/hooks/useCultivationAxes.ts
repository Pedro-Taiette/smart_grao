import { useEffect, useMemo, useState } from 'react';
import { useMutationState, useQueryClient } from '@tanstack/react-query';
import {
  getGetSatellitePassesQueryKey, getRefreshSatellitePassesMutationKey, useGetCercosporaRisk,
  useGetSatellitePasses, useRefreshSatellitePasses,
} from '@/api/generated/risk/risk';
import { useGetPhotos } from '@/api/generated/photos/photos';
import type { CercosporaAlertViewModel } from '@/api/generated/model/cercosporaAlertViewModel';
import { today } from '@/features/cultivations/cultivationSchema';
import {
  crossVerdict, photoFacts, satelliteFacts, satelliteIsStale, summarizeClimate, summarizePhotos,
  summarizeSatellite,
} from '../axes';

/**
 * Plantios cuja busca automatica de satelite ja foi tentada nesta sessao do navegador. Fica fora do
 * React de proposito: a tela Hoje e a pagina do talhao montam o mesmo plantio, e a busca (1 a 2
 * minutos) nao pode sair duas vezes — nem repetir a cada troca de aba quando o servico esta fora.
 */
const attempted = new Set<string>();

/**
 * Os tres eixos de um plantio de milho: clima, satelite e fotos com IA, ja resumidos e cruzados.
 *
 * O satelite deixou de depender de um clique: se a ultima passagem buscada tem mais de 5 dias (o
 * intervalo de revisita do Sentinel-2), a busca sai sozinha, em segundo plano, a partir da ultima
 * leitura. Falha nao vira aviso vermelho — o resumo do eixo diz que o servico nao respondeu.
 *
 * `alert` reaproveita o risco que a tela Hoje ja trouxe para a fazenda inteira, sem nova consulta.
 */
export function useCultivationAxes(cultivationId: string, options: {
  alert?: CercosporaAlertViewModel;
  /** Plantio encerrado: as passagens vao so ate o encerramento, e uma vez buscadas nao mudam. */
  endedOn?: string | null;
} = {}) {
  const cache = useQueryClient();
  const riskQuery = useGetCercosporaRisk(cultivationId, { query: { enabled: !options.alert && Boolean(cultivationId) } });
  const satelliteQuery = useGetSatellitePasses(cultivationId, { query: { enabled: Boolean(cultivationId) } });
  const photosQuery = useGetPhotos({ cultivationId }, {
    query: {
      enabled: Boolean(cultivationId),
      refetchInterval: q => (q.state.data ?? []).some(p => p.analysisStatus === 'Pending' || p.analysisStatus === 'Running') ? 4000 : false,
    },
  });
  const refresh = useRefreshSatellitePasses();
  const [failed, setFailed] = useState(false);

  // Pendente aqui ou no botao manual do painel: os dois usam a mesma mutacao.
  const pending = useMutationState({
    filters: { mutationKey: getRefreshSatellitePassesMutationKey(), status: 'pending' },
    select: mutation => (mutation.state.variables as { id?: string } | undefined)?.id,
  });
  const isFetchingSatellite = pending.includes(cultivationId);

  const day = today();
  const risk = options.alert ?? riskQuery.data;
  const passes = satelliteQuery.data?.passes;
  const satellite = useMemo(() => passes ? satelliteFacts(passes, day) : undefined, [passes, day]);
  const photos = useMemo(() => photoFacts(photosQuery.data ?? []), [photosQuery.data]);

  const { mutateAsync } = refresh;
  useEffect(() => {
    if (!satellite || attempted.has(cultivationId)) return;
    const stale = options.endedOn ? satellite.lastPassDate === null : satelliteIsStale(satellite, day);
    if (!stale) return;
    attempted.add(cultivationId);
    mutateAsync({ id: cultivationId })
      .then(result => cache.setQueryData(getGetSatellitePassesQueryKey(cultivationId), result))
      .catch(() => setFailed(true));
  }, [satellite, cultivationId, day, options.endedOn, mutateAsync, cache]);

  return {
    risk,
    riskFull: riskQuery.data,
    satellite,
    photos,
    photoList: photosQuery.data ?? [],
    climate: summarizeClimate(risk),
    satelliteSummary: summarizeSatellite(satellite, { isFetching: isFetchingSatellite, failed }),
    photoSummary: summarizePhotos(photosQuery.data ? photos : undefined),
    verdict: crossVerdict(risk, satellite, photosQuery.data ? photos : undefined),
    isFetchingSatellite,
    isLoading: (!options.alert && riskQuery.isPending) || satelliteQuery.isPending || photosQuery.isPending,
    error: (options.alert ? null : riskQuery.error) ?? satelliteQuery.error ?? photosQuery.error,
    refetch: () => { void riskQuery.refetch(); void satelliteQuery.refetch(); void photosQuery.refetch(); },
  };
}
