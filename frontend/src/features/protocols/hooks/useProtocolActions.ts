import { useQueryClient } from '@tanstack/react-query';
import {
  getGetProtocolsQueryKey, getGetProtocolByIdQueryKey,
  useCreateProtocol, useCreateProtocolVersion, useRenameProtocol,
  useAddProtocolItem, useRemoveProtocolItem, usePublishProtocol, useRetireProtocol,
} from '@/api/generated/protocols/protocols';
import type { AddProtocolItemViewModel } from '@/api/generated/model/addProtocolItemViewModel';
import type { CreateProtocolViewModel } from '@/api/generated/model/createProtocolViewModel';
import type { ProtocolViewModel } from '@/api/generated/model/protocolViewModel';
import { useNotifier } from '@/shared/notifications/useNotifier';

export function useProtocolActions(id?: string) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const create = useCreateProtocol();
  const version = useCreateProtocolVersion();
  const rename = useRenameProtocol();
  const addItem = useAddProtocolItem();
  const removeItem = useRemoveProtocolItem();
  const publish = usePublishProtocol();
  const retire = useRetireProtocol();

  async function save<T>(operation: () => Promise<T>, message: string): Promise<T | null> {
    try {
      const result = await operation();
      await Promise.all([
        cache.invalidateQueries({ queryKey: getGetProtocolsQueryKey() }),
        ...(id ? [cache.invalidateQueries({ queryKey: getGetProtocolByIdQueryKey(id) })] : []),
      ]);
      notifier.notifySuccess(message);
      return result;
    } catch (error) {
      notifier.notifyError(error);
      return null;
    }
  }

  return {
    isSaving: create.isPending || version.isPending || rename.isPending || addItem.isPending
      || removeItem.isPending || publish.isPending || retire.isPending,

    createProtocol: (data: CreateProtocolViewModel): Promise<ProtocolViewModel | null> =>
      save(() => create.mutateAsync({ data }), 'Protocolo criado como rascunho.'),

    // Devolve a versao criada para que a tela navegue direto para ela: abrir a v+1 e sempre para
    // continuar editando nela, nunca para ficar olhando a versao publicada.
    createVersion: (protocolId: string): Promise<ProtocolViewModel | null> =>
      save(() => version.mutateAsync({ id: protocolId }), 'Nova versão aberta em rascunho.'),

    renameProtocol: (protocolId: string, name: string) =>
      save(() => rename.mutateAsync({ id: protocolId, data: { name } }), 'Protocolo renomeado.'),

    addItem: (protocolId: string, data: AddProtocolItemViewModel) =>
      save(() => addItem.mutateAsync({ id: protocolId, data }), 'Alvo incluído no protocolo.'),

    removeItem: (protocolId: string, itemId: string) =>
      save(() => removeItem.mutateAsync({ id: protocolId, itemId }), 'Alvo removido do protocolo.'),

    publishProtocol: (protocolId: string) =>
      save(() => publish.mutateAsync({ id: protocolId }), 'Protocolo publicado.'),

    retireProtocol: (protocolId: string) =>
      save(() => retire.mutateAsync({ id: protocolId }), 'Protocolo aposentado.'),
  };
}
