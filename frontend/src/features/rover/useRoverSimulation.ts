import { useCallback, useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { getGetPhotosQueryKey, uploadDevicePhoto } from '@/api/generated/photos/photos';
import type { IFormFile } from '@/api/generated/model/iFormFile';
import { PhotoSource } from '@/api/generated/model/photoSource';
import { PlantOrgan } from '@/api/generated/model/plantOrgan';
import { positionAt, type RoverRoute } from './route';
import { newId } from '@/shared/newId';

export interface Sample {
  file: string;
  /** O rotulo que o conjunto publico da a esta foto — o gabarito para comparar com a IA. */
  datasetLabel: 'cercosporiose' | 'saudavel' | 'outras_manchas';
}

export interface Capture {
  stopSequence: number;
  photoId: string;
  sample: Sample;
  status: 'sending' | 'sent' | 'failed';
}

export type RoverPhase = 'ready' | 'driving' | 'capturing' | 'returning' | 'done';

/** A volta inteira leva cerca de 40 s na tela, qualquer que seja o tamanho do talhao. */
const driveSeconds = 40;
/** Tempo parado em cada ponto, fotografando. */
const captureSeconds = 2.5;

function shuffled<T>(items: T[]) {
  const copy = [...items];
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [copy[i], copy[j]] = [copy[j], copy[i]];
  }
  return copy;
}

/**
 * O rover andando pelo talhao: posicao a cada quadro, parada em cada ponto, foto enviada.
 *
 * So o movimento e as imagens sao de mentira. A foto sai pela mesma rota que um rover de verdade
 * usaria (`POST /api/photos/device`, origem `Rover`, marcada como simulada), entra na mesma fila e e
 * analisada pelo mesmo modelo — o resultado que aparece na tela e o da IA, nao um sorteio.
 */
export function useRoverSimulation(route: RoverRoute | null, cultivationId: string, samples: Sample[]) {
  const cache = useQueryClient();
  const [phase, setPhase] = useState<RoverPhase>('ready');
  const [paused, setPaused] = useState(false);
  const [traveled, setTraveled] = useState(0);
  const [nextStop, setNextStop] = useState(0);
  const [captures, setCaptures] = useState<Capture[]>([]);

  const traveledRef = useRef(0);
  const deck = useRef<Sample[]>([]);
  const lastTick = useRef<number | null>(null);
  const captureUntil = useRef<number | null>(null);
  const tickRef = useRef<(now: number) => void>(() => {});
  // O laco le daqui, e nao do estado: entre um quadro e a proxima renderizacao o estado ainda e o
  // antigo, e a mesma parada poderia disparar duas fotos.
  const phaseRef = useRef<RoverPhase>('ready');
  const nextStopRef = useRef(0);
  const pausedRef = useRef(false);

  function goTo(next: RoverPhase) { phaseRef.current = next; setPhase(next); }
  function advanceTo(stop: number) { nextStopRef.current = stop; setNextStop(stop); }

  const speed = route ? Math.max(route.totalMeters / driveSeconds, 1) : 1;

  const capture = useCallback(async (stopSequence: number) => {
    if (!route) return;
    if (deck.current.length === 0) deck.current = shuffled(samples);
    const sample = deck.current.pop();
    if (!sample) return;
    const photoId = newId();
    setCaptures(current => [...current, { stopSequence, photoId, sample, status: 'sending' }]);
    try {
      const blob = await (await fetch(`/rover-samples/${sample.file}`)).blob();
      const [latitude, longitude] = route.stops[stopSequence - 1].position;
      await uploadDevicePhoto({
        id: photoId, cultivationId, source: PhotoSource.Rover, isSimulated: true, organ: PlantOrgan.LowerLeaves,
        takenAt: new Date().toISOString(), latitude, longitude,
        file: new File([blob], sample.file, { type: 'image/jpeg' }) as unknown as IFormFile,
      });
      setCaptures(current => current.map(c => c.photoId === photoId ? { ...c, status: 'sent' } : c));
      await cache.invalidateQueries({ queryKey: getGetPhotosQueryKey({ cultivationId }) });
    } catch {
      setCaptures(current => current.map(c => c.photoId === photoId ? { ...c, status: 'failed' } : c));
    }
  }, [route, samples, cultivationId, cache]);

  /** Um quadro da animacao: anda, ou espera a foto, ou para. */
  function tick(now: number) {
    if (!route) return;
    const last = lastTick.current ?? now;
    lastTick.current = now;
    const current = phaseRef.current;
    const stopIndex = nextStopRef.current;
    if (pausedRef.current || current === 'ready' || current === 'done') return;

    if (current === 'capturing') {
      if (captureUntil.current !== null && now >= captureUntil.current) {
        captureUntil.current = null;
        const following = stopIndex + 1;
        advanceTo(following);
        goTo(following >= route.stops.length ? 'returning' : 'driving');
      }
      return;
    }

    const step = ((now - last) / 1000) * speed;
    const stopAt = stopIndex < route.stops.length ? route.cumulative[stopIndex + 1] : route.totalMeters;
    const next = Math.min(traveledRef.current + step, stopAt);
    traveledRef.current = next;
    setTraveled(next);

    if (next >= stopAt) {
      if (current === 'returning') {
        goTo('done');
      } else {
        goTo('capturing');
        captureUntil.current = now + captureSeconds * 1000;
        void capture(route.stops[stopIndex].sequence);
      }
    }
  }

  // O laco chama sempre a versao mais recente de `tick` (que enxerga `route` e `capture` atuais).
  useEffect(() => {
    tickRef.current = tick;
  });

  useEffect(() => {
    let frame = 0;
    const loop = (now: number) => {
      tickRef.current(now);
      frame = requestAnimationFrame(loop);
    };
    frame = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(frame);
  }, []);

  function start() {
    traveledRef.current = 0;
    lastTick.current = null;
    captureUntil.current = null;
    deck.current = shuffled(samples);
    setTraveled(0);
    advanceTo(0);
    setCaptures([]);
    pausedRef.current = false;
    setPaused(false);
    goTo(route && route.stops.length > 0 ? 'driving' : 'done');
  }

  const where = route ? positionAt(route, traveled) : null;
  const target = route && nextStop < route.stops.length ? route.stops[nextStop] : null;
  const remainingToTarget = route
    ? (nextStop < route.stops.length ? route.cumulative[nextStop + 1] : route.totalMeters) - traveled
    : 0;

  return {
    phase, paused, traveled, captures, target,
    position: where?.position ?? route?.base ?? null,
    heading: where?.heading ?? 0,
    stopsDone: Math.min(nextStop, route?.stops.length ?? 0),
    remainingToTarget,
    secondsToTarget: remainingToTarget / speed,
    start,
    togglePause: () => {
      lastTick.current = null;
      pausedRef.current = !pausedRef.current;
      setPaused(pausedRef.current);
    },
  };
}
