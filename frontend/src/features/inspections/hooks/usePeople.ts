import { useQueryClient } from '@tanstack/react-query';
import {
  getGetPeopleQueryKey, useGetPeople, useCreatePerson, useUpdatePerson, useSetPersonStatus,
} from '@/api/generated/people/people';
import type { PersonRole } from '@/api/generated/model/personRole';
import { useNotifier } from '@/shared/notifications/useNotifier';

export function usePeople(farmId: string, activeOnly = false) {
  const query = useGetPeople({ farmId, activeOnly }, { query: { enabled: Boolean(farmId) } });
  return {
    people: query.data ?? [],
    isLoading: query.isPending,
    error: query.error,
    refetch: () => { void query.refetch(); },
  };
}

export function usePersonActions(farmId: string) {
  const cache = useQueryClient();
  const notifier = useNotifier();
  const create = useCreatePerson();
  const update = useUpdatePerson();
  const status = useSetPersonStatus();

  async function save(operation: () => Promise<unknown>, message: string) {
    try {
      await operation();
      // Sem filtro na chave: a equipe esta em cache com e sem o recorte de ativos.
      await cache.invalidateQueries({ queryKey: getGetPeopleQueryKey() });
      notifier.notifySuccess(message);
      return true;
    } catch (error) {
      notifier.notifyError(error);
      return false;
    }
  }

  return {
    isSaving: create.isPending || update.isPending || status.isPending,
    createPerson: (name: string, role: PersonRole) =>
      save(() => create.mutateAsync({ data: { farmId, name, role } }), 'Pessoa cadastrada.'),
    updatePerson: (id: string, name: string, role: PersonRole) =>
      save(() => update.mutateAsync({ id, data: { name, role } }), 'Cadastro atualizado.'),
    setActive: (id: string, active: boolean) =>
      save(() => status.mutateAsync({ id, params: { active } }),
        active ? 'Pessoa reativada na equipe.' : 'Pessoa desativada; o histórico dela continua.'),
  };
}
