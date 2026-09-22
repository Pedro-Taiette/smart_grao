import { createContext } from 'react';
import type { FarmViewModel } from '@/api/generated/model/farmViewModel';

/**
 * O contrato do contexto de fazenda, separado do provider.
 *
 * Mesma divisao de `NotificationProvider.tsx` / `useNotifier.ts`: arquivo que exporta componente
 * exporta so componente, para o recarregamento rapido continuar funcionando no desenvolvimento.
 */
export interface FarmContextValue {
  /** Fazendas cadastradas — ja no formato que o seletor consome. */
  farms: FarmViewModel[];
  /** A selecionada, ou `null` enquanto carrega e quando nao ha nenhuma cadastrada. */
  farm: FarmViewModel | null;
  /** Atalho para `farm?.id ?? ''`, que e o que os hooks de dados esperam. */
  farmId: string;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
  selectFarm: (id: string) => void;
}

export const FarmContext = createContext<FarmContextValue | null>(null);
