import { useState } from 'react';
import { useGetCultivations } from '@/api/generated/cultivations/cultivations';
import { useGetSeasons } from '@/api/generated/seasons/seasons';

export function useCultivations(farmId: string, fieldId: string | null) {
  const cyclesQuery = useGetCultivations({ fieldId: fieldId ?? '' }, { query: { enabled: Boolean(fieldId) } });
  const seasonsQuery = useGetSeasons({ farmId }, { query: { enabled: Boolean(farmId) } });
  const cultivations = cyclesQuery.data ?? [];
  const seasons = seasonsQuery.data ?? [];
  const [selection, setSelection] = useState<{ fieldId: string | null; id: string } | null>(null);
  const selectedId = selection?.fieldId === fieldId ? selection.id : undefined;
  // Empty string explicitly selects legacy plans without a known crop cycle.
  const selected = selectedId === '' ? null
    : cultivations.find(c => c.id === selectedId) ?? cultivations[0] ?? null;

  return {
    cultivations, seasons, selected,
    selectedId: selected?.id ?? '',
    select: (id: string) => setSelection({ fieldId, id }),
    isLoading: Boolean(fieldId) && (cyclesQuery.isPending || seasonsQuery.isPending),
    error: cyclesQuery.error ?? seasonsQuery.error,
    refetch: () => { void cyclesQuery.refetch(); void seasonsQuery.refetch(); },
  };
}
