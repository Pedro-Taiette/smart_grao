import { Navigate, useParams } from 'react-router-dom';

/**
 * Rotas da organizacao anterior, mantidas vivas como redirecionamento.
 *
 * Links ja foram abertos e salvos — em especial o de execucao de vistoria, que circula por mensagem
 * para quem esta no talhao. Quebrar esses links nao tem beneficio nenhum, e o custo de manter e um
 * arquivo de vinte linhas.
 *
 * Ficam fora de `router.tsx` porque aquele arquivo exporta a configuracao de rotas, e nao
 * componentes: misturar os dois desliga o recarregamento rapido no desenvolvimento.
 */

export function RedirectToField({ tab }: { tab: string }) {
  const { fieldId } = useParams();
  return <Navigate to={`/talhoes/${fieldId}?aba=${tab}`} replace />;
}

export function RedirectToInspection() {
  const { inspectionId } = useParams();
  return <Navigate to={`/vistorias/${inspectionId}`} replace />;
}

export function RedirectToProtocol() {
  const { protocolId } = useParams();
  return <Navigate to={`/ajustes/protocolos/${protocolId}`} replace />;
}
