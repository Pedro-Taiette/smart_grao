# Navegação e organização das telas

Reorganização da interface feita depois da fase 3, antes de seguir para a fase 4. Nenhuma regra de
domínio mudou: o que mudou foi onde cada coisa fica e em que ordem aparece.

## O problema

A navegação era o diagrama do banco de dados virado para fora:

```
Fazendas → Talhões → Cultivos → Planos → Vistorias → Observações
```

Essa é a ordem em que as tabelas se referenciam, e a ordem em que o sistema foi construído. Não é a
ordem em que alguém pensa. Os sintomas:

- **Três abas no topo, duas delas de implantação.** Catálogo e Protocolos são dados de referência,
  cadastrados uma vez, e ocupavam dois terços do menu principal.
- **Quatro cliques até uma vistoria.** Fazendas → Talhão → Vistorias → a visita — com a fazenda e o
  talhão sendo quase sempre os mesmos.
- **A tela inicial era uma lista de fazendas**, e o usuário típico tem uma.
- **O talhão estava espalhado em três rotas** (`fields`, `cultivations`, `inspections`) sendo um
  objeto só.
- **Nada dizia o que fazer.** O sistema era um arquivo, não uma agenda.

## As cinco decisões

### 1. A fazenda virou contexto, não destino

Um seletor fixo na barra do topo, lembrado entre sessões em `localStorage`. Some quando há uma
fazenda só — um seletor de uma opção não é escolha.

**O que se perdeu:** a rota deixou de carregar o id, então `/talhoes` significa coisas diferentes
para duas pessoas. Por isso as telas de um **registro específico** mantêm id próprio na URL
(`/talhoes/:fieldId`, `/vistorias/:id`): esses ids são globais, o link continua compartilhável, e
abrir um deles **corrige** a fazenda selecionada em vez de dar erro. É o que faz o link de uma
vistoria funcionar quando chega por mensagem no celular de quem vai a campo.

Implementação em `frontend/src/features/farms/FarmContext.tsx`.

### 2. A abertura virou "Hoje"

Não um índice do sistema — um painel de pendências, na ordem da urgência:

| Grupo | O que entra |
|---|---|
| Em campo agora | Vistorias em andamento |
| Para fazer | Agendadas para hoje **ou antes** — atrasadas junto, não num grupo separado |
| Próximas | As três seguintes |
| Últimas concluídas | As três últimas |

Grupo vazio não aparece. Uma seção com "nenhum" escrito dentro é ruído com aparência de conteúdo.

**As pendências de cadastro** (`PendingSetup`) resolvem um problema antigo: o sistema tem uma ordem
obrigatória — fazenda, talhão, cultivo, equipe, protocolo — e antes ela só aparecia como botão
desabilitado sem explicação, ou como erro depois de preencher um formulário inteiro. Agora a ordem é
a própria tela, uma pendência por vez, dizendo por que ela bloqueia e levando ao lugar de resolver.

### 3. O talhão virou uma página com abas

`/talhoes/:fieldId` reúne o que eram três telas: **Cultivo**, **Pontos de coleta** e **Vistorias**.
O cabeçalho responde a identidade uma vez — nome, área, o que está plantado e desde quando — e a aba
escolhe o assunto.

A aba vive na URL (`?aba=`) e não em estado: permite mandar "os pontos do Talhão Sul" por mensagem,
e faz o botão de voltar do navegador desfazer a troca de aba, que é o que quem clicou espera.

`/talhoes` ficou sendo só o mapa: desenhar, redesenhar, escolher. Perdeu todos os painéis que a
transformavam num índice disfarçado de mapa.

### 4. O menu encolheu para o trabalho do dia

`Hoje · Talhões · Vistorias`, mais **Ajustes**. Fazendas, Equipe, Protocolos e Pragas e doenças
desceram para o hub de ajustes — continuam a um clique, saem da frente.

No celular esses mesmos destinos viram navegação inferior: o alcance do polegar é a base da tela,
não o topo.

### 5. O vocabulário perdeu o jargão de modelagem

| Era | Virou | Por quê |
|---|---|---|
| Catálogo de alvos | Pragas e doenças | "Alvo" é termo do modelo; ninguém no campo fala assim |
| Plano de amostragem | Pontos de coleta | O produtor marca pontos, não gera planos |
| Protocolo | Protocolo | Mantido: é palavra de agrônomo de verdade |

## Rotas

| Rota | Tela |
|---|---|
| `/` | Hoje — pendências e agenda |
| `/talhoes` | Mapa da propriedade |
| `/talhoes/:fieldId` | O talhão: cultivo, pontos, vistorias |
| `/vistorias` | Todas as visitas da fazenda |
| `/vistorias/:id` | Executar a visita — é o link que abre no celular |
| `/ajustes` | Hub de cadastros de referência |
| `/ajustes/fazendas`, `/ajustes/equipe`, `/ajustes/protocolos`, `/ajustes/catalogo` | Cadastros |

As rotas da organização anterior (`/farms/...`, `/inspections/:id`, `/protocols`, `/targets`)
continuam como redirecionamento — links já foram abertos e salvos, em especial o de execução de
vistoria, que circula por mensagem.

## O que mudou no backend

Só leitura. A navegação por tarefa faz perguntas sobre a **propriedade inteira**, e os filtros
existentes eram todos por talhão ou por cultivo:

- `GET /api/inspections?farmId=` — a agenda da fazenda. Sem isso, a tela Hoje dispararia uma
  requisição por cultivo cadastrado.
- `GET /api/cultivations?farmId=` — "quais talhões estão sem cultivo aberto" é pergunta sobre a
  propriedade. `fieldId` virou opcional; talhão inexistente continua sendo 404.
- `InspectionSummaryViewModel` ganhou **`fieldName`**, pelo mesmo motivo que já carregava
  `responsibleName`: uma lista da fazenda inteira mistura talhões, e "Talhão Sul, 22/09, Ana" é o
  que se lê — um id não situa ninguém.

## Fora desta entrega

- **Identidade visual.** Tipografia, cores e densidade seguem o padrão do MUI. A confusão era de
  organização, não de aparência.
- **Cadência de vistorias.** O agendamento continua avulso; a tela Hoje mostra o que está atrasado,
  mas ninguém propõe a próxima visita.
- **O mapa no celular.** A tela de talhões empilha lista e mapa em telas estreitas, mas desenhar
  contorno com o dedo continua ruim. A execução da vistoria é que foi pensada para o campo.
