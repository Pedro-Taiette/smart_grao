import { useState } from 'react';
import { useGetMonitoringTargets } from '@/api/generated/monitoring-targets/monitoring-targets';
import { Crop } from '@/api/generated/model/crop';
import type { TargetKind } from '@/api/generated/model/targetKind';

export function useTargetCatalog() {
  // Milho e o foco da fase 2, entao o catalogo ja abre nele em vez de numa lista de tudo.
  const [crop, setCrop] = useState<Crop>(Crop.Corn);
  const [kind, setKind] = useState<TargetKind | ''>('');
  const query = useGetMonitoringTargets({ crop, ...(kind ? { kind } : {}) });

  return {
    targets: query.data ?? [],
    crop, setCrop,
    kind, setKind,
    isLoading: query.isPending,
    error: query.error,
    refetch: () => { void query.refetch(); },
  };
}
