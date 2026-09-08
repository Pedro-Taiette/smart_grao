import { useEffect, useMemo } from 'react';
import { MapContainer, TileLayer, useMap } from 'react-leaflet';
import { latLngBounds } from 'leaflet';
import type { FieldViewModel } from '@/api/generated/model/fieldViewModel';
import type { GeoJsonPolygon } from '@/api/generated/model/geoJsonPolygon';
import type { SamplingPointViewModel } from '@/api/generated/model/samplingPointViewModel';
import { SamplingPointsLayer } from '@/features/sampling/components/SamplingPointsLayer';
import { BRAZIL_CENTER, toLeafletPositions, type LatLngTuple } from '../geo/geoJson';
import { DrawControl } from './DrawControl';
import { FieldPolygon } from './FieldPolygon';

// Efeito colateral: o Geoman injeta `map.pm` no Leaflet. Sem este import, `map.pm` e undefined em
// tempo de execucao ainda que os tipos compilem.
import '@geoman-io/leaflet-geoman-free';

interface FieldMapProps {
  fields: FieldViewModel[];
  farmCenter: LatLngTuple | null;
  selectedFieldId: string | null;
  editingFieldId: string | null;
  samplingPoints: SamplingPointViewModel[];
  isSamplingOutdated: boolean;
  onSelectField: (fieldId: string) => void;
  onPolygonDrawn: (boundary: GeoJsonPolygon) => void;
  onGeometryChange: (boundary: GeoJsonPolygon) => void;
}

/**
 * Enquadra o mapa nos talhoes existentes.
 *
 * Roda apenas quando a quantidade de talhoes muda — nao a cada renderizacao. Reenquadrar a cada
 * atualizacao arrancaria o mapa do lugar no meio de um arrasto de vertice, que e exatamente quando
 * o usuario mais precisa que ele fique parado.
 */
function FitToFields({ fields, fallback }: { fields: FieldViewModel[]; fallback: LatLngTuple | null }) {
  const map = useMap();
  const count = fields.length;

  useEffect(() => {
    if (count === 0) {
      if (fallback) map.setView(fallback, 13);
      return;
    }

    const positions = fields.flatMap((field) => toLeafletPositions(field.boundary));
    if (positions.length > 0) {
      map.fitBounds(latLngBounds(positions), { padding: [40, 40] });
    }
    // `fields` fora das dependencias de proposito: ver a explicacao acima.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map, count, fallback]);

  return null;
}

/**
 * O mapa dos talhoes.
 *
 * Componente apresentacional: nao consulta a API nem guarda estado de negocio. Recebe os talhoes e
 * devolve intencoes — "desenharam este contorno", "selecionaram este talhao", "moveram este
 * vertice" — para a pagina decidir o que fazer.
 */
export function FieldMap({
  fields,
  farmCenter,
  selectedFieldId,
  editingFieldId,
  samplingPoints,
  isSamplingOutdated,
  onSelectField,
  onPolygonDrawn,
  onGeometryChange,
}: FieldMapProps) {
  const initialCenter = useMemo(() => farmCenter ?? BRAZIL_CENTER, [farmCenter]);

  return (
    <MapContainer center={initialCenter} zoom={farmCenter ? 13 : 5} style={{ height: '100%' }}>
      {/* OpenStreetMap para orientacao geral; a atribuicao e exigida pela licenca de uso. */}
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        maxZoom={19}
      />

      <FitToFields fields={fields} fallback={farmCenter} />

      {fields.map((field) => (
        <FieldPolygon
          key={field.id}
          field={field}
          isSelected={field.id === selectedFieldId}
          isEditing={field.id === editingFieldId}
          onSelect={onSelectField}
          onGeometryChange={onGeometryChange}
        />
      ))}

      {/* Os pontos ficam por cima dos contornos, e somem durante o redesenho: eles pertencem ao
          contorno antigo, e mante-los na tela enquanto ele e arrastado mostraria uma malha que ja
          nao corresponde ao talhao. */}
      {editingFieldId === null && (
        <SamplingPointsLayer points={samplingPoints} isOutdated={isSamplingOutdated} />
      )}

      {/* A barra de desenho some durante a edicao de um contorno: desenhar um talhao novo enquanto
          outro esta aberto para edicao perderia as alteracoes ainda nao salvas. */}
      {editingFieldId === null && <DrawControl onPolygonDrawn={onPolygonDrawn} />}
    </MapContainer>
  );
}
