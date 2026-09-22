import { useCallback, useMemo, useState } from 'react';
import { FarmContext, type FarmContextValue } from './farmContextValue';
import { useFarms } from './hooks/useFarms';

/**
 * A fazenda e contexto, nao destino.
 *
 * Antes ela era a tela inicial e o primeiro segmento de quase toda rota. So que quem usa o sistema
 * opera uma propriedade, nao um catalogo de propriedades: obrigar a "entrar" nela a cada visita
 * cobrava um clique por uma escolha que nao muda de semana para semana. Aqui ela vira um seletor
 * fixo no topo, lembrado entre sessoes.
 *
 * O preco e a rota deixar de carregar o id — um link para `/talhoes` significa coisas diferentes
 * para duas pessoas. Por isso as telas de um registro especifico (`/talhoes/:fieldId`,
 * `/vistorias/:id`) continuam com id proprio na URL: esses ids sao globais, o link e compartilhavel
 * como antes, e abrir um deles **corrige** a fazenda selecionada em vez de dar erro.
 */

const STORAGE_KEY = 'smartgrao.selectedFarmId';

/** localStorage indisponivel (modo privado, storage cheio) nao pode derrubar a aplicacao. */
function readStoredFarmId(): string {
  try {
    return window.localStorage.getItem(STORAGE_KEY) ?? '';
  } catch {
    return '';
  }
}

function storeFarmId(id: string) {
  try {
    window.localStorage.setItem(STORAGE_KEY, id);
  } catch {
    /* Sem persistencia a escolha vale so para esta aba — degradacao aceitavel. */
  }
}

export function FarmProvider({ children }: { children: React.ReactNode }) {
  const { farms, isLoading, error, refetch } = useFarms();
  const [requestedId, setRequestedId] = useState(readStoredFarmId);

  // A escolha guardada pode apontar para uma fazenda excluida, ou ainda nao ter sido feita. Cair na
  // primeira da lista e melhor do que a aplicacao inteira operar sobre um id que nao existe.
  //
  // Derivado a cada renderizacao, e nao copiado para um estado: guardar a correcao exigiria um
  // efeito para manter a copia em dia, e um efeito que chama `setState` e justamente o que dispara
  // a renderizacao em cascata. O id guardado pode ficar obsoleto sem prejuizo — ele so e consultado
  // atraves desta linha.
  const farm = farms.find((candidate) => candidate.id === requestedId) ?? farms[0] ?? null;

  const selectFarm = useCallback((id: string) => {
    setRequestedId(id);
    storeFarmId(id);
  }, []);

  const value = useMemo<FarmContextValue>(
    () => ({ farms, farm, farmId: farm?.id ?? '', isLoading, error, refetch, selectFarm }),
    [farms, farm, isLoading, error, refetch, selectFarm],
  );

  return <FarmContext.Provider value={value}>{children}</FarmContext.Provider>;
}
