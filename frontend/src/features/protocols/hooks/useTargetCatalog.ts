import { useState } from 'react';
import { useGetMonitoringTargets } from '@/api/generated/monitoring-targets/monitoring-targets';
import { focusCrop } from '@/features/fields/cropLabels';
import type { TargetKind } from '@/api/generated/model/targetKind';

export function useTargetCatalog() {
  const crop = focusCrop;
  const [kind, setKind] = useState<TargetKind | ''>('');
  const query = useGetMonitoringTargets({ crop, ...(kind ? { kind } : {}) });

  return {
    targets: query.data ?? [],
    crop,
    kind, setKind,
    isLoading: query.isPending,
    error: query.error,
    refetch: () => { void query.refetch(); },
  };
}
