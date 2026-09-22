import { useContext, useEffect } from 'react';
import { FarmContext, type FarmContextValue } from './farmContextValue';

/** A fazenda em uso e como troca-la. Ver `FarmContext.tsx` para o porque da escolha. */
export function useFarmContext(): FarmContextValue {
  const value = useContext(FarmContext);
  if (!value) throw new Error('useFarmContext precisa estar dentro de <FarmProvider>.');
  return value;
}

/**
 * Sincroniza o contexto com a fazenda de um registro aberto por link direto.
 *
 * Quem recebe o link de uma vistoria no celular nao passou pelo seletor. Sem isso, a tela abriria
 * certa e o resto da navegacao continuaria apontando para outra propriedade.
 */
export function useSyncFarmFromRecord(farmId: string | undefined) {
  const { farmId: current, selectFarm } = useFarmContext();

  useEffect(() => {
    if (farmId && farmId !== current) selectFarm(farmId);
  }, [farmId, current, selectFarm]);
}
