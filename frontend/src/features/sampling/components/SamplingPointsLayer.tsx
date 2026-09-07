import { useMemo } from 'react';
import { CircleMarker, Tooltip } from 'react-leaflet';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';

interface SamplingPointsLayerProps {
  points: SamplingPointViewModel[];
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
 */
export function SamplingPointsLayer({ points }: SamplingPointsLayerProps) {
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
            fillColor: '#d32f2f',
            fillOpacity: 0.9,
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
