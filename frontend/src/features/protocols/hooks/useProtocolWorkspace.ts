import { useState } from 'react';
import { useGetProtocolById } from '@/api/generated/protocols/protocols';
import { useGetMonitoringTargets } from '@/api/generated/monitoring-targets/monitoring-targets';
import type { ProtocolItemViewModel } from '@/api/generated/model/protocolItemViewModel';
import { ProtocolStatus } from '@/api/generated/model/protocolStatus';
import { useProtocolActions } from './useProtocolActions';

type Dialog = { kind: 'item' | 'rename' } | { kind: 'remove'; item: ProtocolItemViewModel } | null;

export function useProtocolWorkspace(protocolId: string) {
  const query = useGetProtocolById(protocolId, { query: { enabled: Boolean(protocolId) } });
  const protocol = query.data;
  const actions = useProtocolActions(protocolId);
  const [dialog, setDialog] = useState<Dialog>(null);

  // O catalogo so e buscado depois que a cultura do protocolo e conhecida: um protocolo de milho
  // nao deve nem oferecer alvos de outra cultura, ja que o backend os recusaria.
  const catalogQuery = useGetMonitoringTargets(
    { crop: protocol?.crop ?? 'Corn' },
    { query: { enabled: protocol !== undefined } },
  );

  const chosen = new Set(protocol?.items.map((item) => item.targetId) ?? []);

  return {
    ...actions,
    protocol,
    isDraft: protocol?.status === ProtocolStatus.Draft,
    isPublished: protocol?.status === ProtocolStatus.Published,
    /** Alvos da cultura que ainda nao estao nesta versao, em nenhum orgao. */
    availableTargets: (catalogQuery.data ?? []).filter((target) => !chosen.has(target.id)),
    isLoading: query.isPending,
    error: query.error ?? catalogQuery.error,
    refetch: () => { void query.refetch(); void catalogQuery.refetch(); },
    dialog, openDialog: setDialog, closeDialog: () => setDialog(null),
  };
}
