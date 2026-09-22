import { useState } from 'react';
import { useGetFieldById } from '@/api/generated/fields/fields';
import type { CultivationViewModel } from '@/api/generated/model/cultivationViewModel';
import { useCultivations } from './useCultivations';
import { useCultivationActions } from './useCultivationActions';

type Dialog = { kind: 'season' | 'cultivation' } | { kind: 'stage' | 'closure'; cultivation: CultivationViewModel } | null;

export function useCultivationWorkspace(farmId: string, fieldId: string) {
  const data = useCultivations(farmId, fieldId);
  const fieldQuery = useGetFieldById(fieldId, { query: { enabled: Boolean(fieldId) } });
  const actions = useCultivationActions(farmId, fieldId);
  const [dialog, setDialog] = useState<Dialog>(null);
  return {
    ...data, ...actions, field: fieldQuery.data,
    isLoading: data.isLoading || fieldQuery.isPending,
    error: data.error ?? fieldQuery.error,
    wrongFarm: fieldQuery.data !== undefined && fieldQuery.data.farmId !== farmId,
    refetch: () => { data.refetch(); void fieldQuery.refetch(); },
    dialog, openDialog: setDialog, closeDialog: () => setDialog(null),
  };
}
