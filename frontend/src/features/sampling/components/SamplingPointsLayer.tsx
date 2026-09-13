import { useMemo } from 'react';
import { CircleMarker, Tooltip } from 'react-leaflet';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';

interface SamplingPointsLayerProps {
  points: SamplingPointViewModel[];
  /** O contorno mudou depois que esta malha foi gerada. */
  isOutdated?: boolean;
}

/**
 * Os pontos de amostragem sobre o talhao.
 *
 * Circulos e nao alfinetes: o alfinete aponta para o chao a partir da ponta de baixo, e com dezenas
 * deles proximos fica dificil saber que ponto e qual. O circulo marca o lugar exato onde ele esta.
 *
 * O numero da parada aparece ao passar o mouse — a malha e gerada em serpentina justamente para que
 * seguir a numeracao seja um caminho continuo pelo talhao, sem atravessa-lo de ponta a ponta a cada
 * fileira.
 *
 * Malha defasada aparece em cinza. O aviso escrito no painel resolve para quem le; a cor resolve
 * para quem so bate o olho no mapa, que e onde o erro custaria caro — ir a campo atras de um ponto
 * que nao esta mais dentro do talhao.
 */
export function SamplingPointsLayer({ points, isOutdated = false }: SamplingPointsLayerProps) {
  // O tamanho do circulo cai quando a malha e densa: no modo Mapeamento sao centenas de pontos, e
  // no raio fixo eles se sobrepoem ate virar uma mancha unica.
  const radius = useMemo(() => (points.length > 120 ? 3 : 5), [points.length]);

  return (
    <>
      {points.map((point) => (
        <CircleMarker
          key={point.id}
          center={[point.location.coordinates[1], point.location.coordinates[0]]}
          radius={radius}
          pathOptions={{
            color: '#ffffff',
            weight: 1,
            fillColor: isOutdated ? '#9e9e9e' : '#d32f2f',
            fillOpacity: isOutdated ? 0.6 : 0.9,
          }}
        >
          <Tooltip direction="top" offset={[0, -4]}>
            Parada {point.sequence}
          </Tooltip>
        </CircleMarker>
      ))}
    </>
  );
}
