# Fase 2 — Catálogo de alvos e protocolos

Fase 2: o catálogo de pragas e doenças foliares e os protocolos versionados de vistoria, com as
telas para usá-los. Segue sem autenticação, por decisão de escopo.

Os alvos, as unidades e as fontes agronômicas estão em [Fase 2 — catálogo de alvos](fase-2-catalogo-alvos.md),
inclusive o que **não** foi confirmado na pesquisa.

## Modelo e regras

- **Alvo (`MonitoringTarget`)** — praga ou doença foliar. É dado de referência do produto,
  compartilhado por todas as propriedades: não existe alvo "de uma fazenda". O `code` é uma chave
  estável em minúsculas (`spodoptera_frugiperda`), porque é ela que vai rotular imagem e resultado
  de modelo na fase 5; nome comum varia por região e Guid não diz nada.
- **Capacidade de automação** — `ManualRecord`, `UnderValidation`, `AutomationEnabled`, por alvo.
  `ManualRecord` é o zero do enum: alvo cadastrado sem que ninguém tenha pensado nisso nasce sem
  promessa de diagnóstico. Subir de `ManualRecord` direto para `AutomationEnabled` é **recusado** —
  liberar um modelo sem revisão humana é o que transformaria o catálogo amplo numa promessa que
  ninguém verificou. Descer é sempre permitido, em um passo: modelo que regrediu em campo volta
  para revisão no mesmo dia.
- **Protocolo (`Protocol`)** — o que coletar numa vistoria, para uma cultura. Identificado por
  `code` + `Version`. Declara a sua cultura e **só aceita alvos dela**: é isso que impede uma
  vistoria de milho de herdar o MIP-Soja por omissão. A barreira fica no modelo, não numa tela que
  alguém lembre de conferir.
- **Versionamento** — `Draft` se edita; `Published` é imutável; `Retired` sai de uso para vistorias
  novas e continua legível pelo histórico. `CreateNextVersion` parte de uma versão publicada e copia
  os alvos numa v+1 em `Draft`. A vistoria da fase 3 vai apontar para uma versão exata, então editar
  o protocolo nunca reescreve o que foi pedido numa visita já realizada. Não se ramifica de um
  rascunho: daria duas minutas concorrentes da mesma família sem que nenhuma tenha sido usada.
  Publicar exige pelo menos um alvo.
- **Item do protocolo (`ProtocolItem`)** — o alvo dentro da versão: órgão observado, unidade de
  contagem, fotos solicitadas (0 a 5), instruções (até 2.000 caracteres) e nível de referência.
  O mesmo alvo não se repete no mesmo órgão dentro de uma versão.
- **Unidade de contagem** — conjunto fechado de oito valores. É o que permite o protocolo ser
  genérico sem que o número digitado em campo perca significado: "12" só quer dizer alguma coisa
  depois de se saber se são plantas atacadas, insetos por armadilha ou percentual de área foliar
  lesionada. A unidade carrega também o método — contagem em 10 m de fileira e armadilha adesiva
  não são o mesmo trabalho de campo que inspecionar plantas ao acaso.
- **Órgão** — obrigatório, exceto em `InsectsPerTrap`, que não observa a planta. A recíproca também
  vale: contagem por armadilha com órgão é recusada.
- **Nível de referência** — opcional, e sempre com a fonte junto, no mesmo objeto. A pesquisa achou
  valor publicado para só três dos dezoito alvos; a maioria é decidida "em função do nível de dano",
  e as doenças foliares não têm nível de dano econômico de uso corrente. Exigir um número aqui
  obrigaria alguém a inventá-lo. A faixa é validada por unidade: 0–100 em percentuais, 1–9 na nota
  visual, positivo no resto. `Presence` não aceita nível — a cigarrinha é vetor de enfezamento e não
  tem nível de controle estabelecido, então a presença já justifica a ação.

## Banco e migração

Migration: `20260922163702_TargetCatalogAndProtocols`. **Aplicada** no ambiente configurado.

- Cria `monitoring_targets`, `protocols` e `protocol_items`.
- Semeia os **18 alvos de milho** do catálogo, todos em `ManualRecord`, com ids sintéticos e
  estáveis (`a1000000-…-0000000000NN`): a linha precisa sobreviver a uma reaplicação da seed, e um
  Guid sorteado em tempo de build criaria um alvo novo a cada migration.
- `ix_protocol_items_target_per_organ` é SQL explícito, com `NULLS NOT DISTINCT`, porque o órgão é
  nulo na contagem por armadilha: sem isso o PostgreSQL trataria cada nulo como valor próprio e
  deixaria passar duas armadilhas iguais para o mesmo alvo sob requisições concorrentes — o caso que
  a checagem em memória não cobre. Não há como declarar isso pelo modelo do EF, então **essa
  restrição precisa ser preservada em alterações futuras do esquema**, como a de sobreposição de
  ciclos da fase 1.
- A versão de linha do protocolo impede que uma publicação concorrente congele um protocolo do qual
  alguém acabou de remover um alvo — depois de publicado não há como corrigir no lugar.
- O nível de referência é um objeto *owned*: a sua chave é o item que o possui. Ao copiar itens para
  a próxima versão, o nível é recriado por valor; compartilhar a instância faria a persistência
  entender que o nível da v1 mudou de dono ao gravar a v2, e recusaria a operação inteira.
- O rollback derruba as três tabelas, levando junto os protocolos e o catálogo.

Aplicar, a partir da raiz:

```powershell
dotnet run --project backend/src/SmartGrao.WebApi -- migrate
```

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/monitoring-targets?crop={c}&kind={k}` | Listar o catálogo, filtrando por cultura e tipo |
| POST | `/api/monitoring-targets` | Cadastrar alvo com `code`, nomes, `kind` e `crop` |
| PUT | `/api/monitoring-targets/{id}/automation` | Mover o alvo na escada da automação |
| GET | `/api/protocols?crop={c}&status={s}` | Listar versões, sem os alvos |
| GET | `/api/protocols/{id}` | Consultar a versão com os alvos |
| POST | `/api/protocols` | Criar a v1 em rascunho, com `code`, `crop`, `name` |
| POST | `/api/protocols/{id}/versions` | Abrir a próxima versão de um protocolo publicado |
| PUT | `/api/protocols/{id}/name` | Renomear (só em rascunho) |
| POST | `/api/protocols/{id}/items` | Incluir um alvo |
| DELETE | `/api/protocols/{id}/items/{itemId}` | Remover um alvo (só em rascunho) |
| PUT | `/api/protocols/{id}/publication` | Publicar |
| PUT | `/api/protocols/{id}/retirement` | Aposentar |

Os enums trafegam como texto. `organ` é `null` quando `unit` é `InsectsPerTrap`; `referenceLevel`
é `null` ou `{ "threshold": 20, "source": "…" }`. Os endpoints de protocolo devolvem sempre a versão
inteira com os alvos, e a capacidade de automação de cada alvo vem do catálogo na hora da leitura —
não é copiada para dentro do protocolo, senão a tela mostraria o estado do dia da publicação.

Falhas de validação retornam 422, regras de negócio 400, referências inexistentes 404 e conflitos
409. Os códigos `protocol.*` estão no catálogo central do backend e traduzidos no frontend.

### Códigos de erro

| Código | HTTP | Situação |
|---|---|---|
| `protocol.target_from_another_crop` | 422 | alvo de outra cultura num protocolo |
| `protocol.automation_skips_validation` | 400 | automação liberada sem passar pela validação |
| `protocol.not_draft` | 409 | alteração em protocolo publicado ou aposentado |
| `protocol.not_published` | 409 | ramificar ou aposentar o que não está publicado |
| `protocol.empty` | 400 | publicar sem nenhum alvo |
| `protocol.duplicate_item` | 409 | mesmo alvo, mesmo órgão, mesma versão |
| `protocol.duplicate_version` | 409 | `code` + `version` repetidos |
| `protocol.duplicate_target_code` | 409 | código de alvo repetido |
| `protocol.organ_required` | 422 | unidade que observa a planta, sem órgão |
| `protocol.organ_not_applicable` | 422 | contagem por armadilha com órgão |
| `protocol.reference_level_without_source` | 422 | nível sem fonte |
| `protocol.reference_level_not_applicable` | 422 | nível num registro de presença |
| `protocol.reference_level_out_of_range` | 422 | fora da faixa da unidade |
| `protocol.invalid_target_code`, `protocol.invalid_code` | 422 | fora do formato de código estável |
| `protocol.invalid_target_name`, `protocol.invalid_name` | 422 | nome vazio ou longo demais |
| `protocol.invalid_instructions` | 422 | instruções vazias ou acima de 2.000 caracteres |
| `protocol.too_many_photos` | 422 | fora de 0 a 5 |
| `protocol.unknown_target_kind`, `protocol.unknown_unit`, `protocol.unknown_automation` | 422 | valor fora do catálogo |
| `protocol.concurrent_change` | 409 | protocolo alterado durante a operação |
| `protocol.not_found`, `protocol.target_not_found`, `protocol.item_not_found` | 404 | |

## Escala fenológica

Dívida que a [fase 1](fase-1-cultivos.md) deixou explicitamente para cá: o estágio era texto livre
de 32 caracteres, sem validação por cultura.

`GrowthStageCatalog` transcreve as escalas — **milho** por Ritchie, Hanway & Benson (VE, V1–V20, VT,
R1–R6) e **soja** por Fehr & Caviness (VE, VC, V1–V20, R1–R8). Tabela em código, como
`MipSojaSamplingTable`: são escalas científicas fixas, não conteúdo que alguém edita.

- Num cultivo de milho, `V6` é um estágio e "seis folhas" não é. Quem for comparar dois ciclos — ou
  alimentar um modelo na fase 5 — precisa do mesmo código nos dois.
- O que se digita é resolvido para o código canônico: `v6`, `V6` e ` V6 ` gravam `V6`. Gravar os
  três como vieram separaria em três coisas o que é uma só.
- As escalas diferem de verdade: `R8` existe na soja e não no milho, cuja escala reprodutiva termina
  em `R6`; `VT` é pendoamento, e só o milho pendoa.
- O teto vegetativo é V20. O número de folhas varia com o híbrido e a escala não tem fim definido;
  20 cobre com folga o que se observa em campo sem virar uma lista interminável na tela.
- **Cultura sem escala transcrita continua aceitando texto livre.** Inventar uma escala para o
  algodão só para fechar a simetria seria pior do que admitir que ela não está aqui.
- Fora da escala, `cultivation.stage_not_in_scale` (422). Registros gravados antes desta fase
  permanecem como foram digitados: a validação vale para o que entra daqui em diante.

`GET /api/growth-stages?crop={c}` devolve a escala. Lista vazia não é erro — significa cultura sem
escala, e a tela volta ao campo de texto. Sem mudança de esquema: a coluna `stage` segue `varchar(32)`.

Fontes das escalas:

- [Estádios de crescimento do milho — Ritchie, Hanway & Benson, em português (PDF)](https://www.npct.com.br/npctweb/npct.nsf/article/BRS-3137/$File/MF3305BP-CornGrowth-portuguese_FINAL.pdf)
- [Crescimento e desenvolvimento da soja — Fehr & Caviness, em português (PDF)](https://bookstore.ksre.ksu.edu/download/soybean-growth-and-development-in-portuguese-crescimento-e-desenvolvimento-da-soja_MF3339BP)
- [UFSM/PET Agronomia — escala fenológica da soja](https://www.ufsm.br/pet/agronomia/2021/06/29/conhecendo-a-escala-fenologica-da-cultura-da-soja)
- [AgroAdvance — fenologia do milho](https://agroadvance.com.br/blog-fenologia-do-milho/)

## Telas

Catálogo e protocolos são dados de referência do produto, iguais para todas as propriedades. Por
isso ficam na raiz, ao lado de **Fazendas** na barra do topo, e não dentro de uma fazenda.

| Rota | Tela |
|---|---|
| `/targets` | Catálogo de alvos, separado em pragas e doenças foliares, com filtro por cultura e tipo |
| `/protocols` | Protocolos da cultura, agrupados por família de versões |
| `/protocols/{id}` | Uma versão: seus alvos, e as ações que a situação permite |

O diálogo de estágio, na tela de cultivos, deixou de pedir um código digitado: em milho e soja ele
lista a escala como `V6 — 6 folhas totalmente expandidas`. Quem está no campo reconhece a descrição
bem mais facilmente do que lembra como aquilo se escreve.

Decisões de interface, seguindo `frontend/src/features/protocols/protocolLabels.ts` — o arquivo onde
mora o vocabulário, como `samplingLabels.ts` faz na amostragem:

- **A situação da automação aparece em toda parte** — na lista, no protocolo e ao lado de cada alvo,
  com a explicação no *tooltip*. Sem isso, um catálogo de 18 alvos poderia ser lido como "o sistema
  reconhece tudo isso sozinho". É a razão de o rótulo existir.
- **O formulário esconde o campo que seria recusado.** Escolher contagem por armadilha some com
  "Onde olhar"; escolher registro de presença some com o nível de referência. A regra continua sendo
  do backend — a tela só chega na mesma conclusão antes, em vez de deixar a pessoa preencher e levar
  um erro no salvar.
- **O limiar aparece como decisão, não como número solto:** "Age a partir de 20% de plantas
  atacadas", seguido da fonte. Quando não há, a tela diz *"Sem nível de referência publicado — a
  decisão fica com quem avalia"*, em vez de deixar o espaço em branco.
- **As versões de um protocolo aparecem juntas.** O valor do versionamento só fica visível quando se
  vê a v1 publicada continuar ali depois de a v2 existir.
- **Publicar avisa o que significa** antes e depois: a versão não muda mais, e alterar exige abrir a
  próxima. O botão "Abrir próxima versão" já navega para o rascunho criado — abrir a v+1 é sempre
  para continuar nela.

## Verificação

Testes de domínio em `backend/tests/SmartGrao.Domain.Tests/Protocols` (39 casos) e, para a escala
fenológica, em `Cultivations/CultivationTests.cs`.

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

Além dos testes, o fluxo foi exercido contra o banco configurado: catálogo semeado com 18 alvos,
protocolo de milho criado, alvo de soja recusado (`protocol.target_from_another_crop`), armadilha
duplicada recusada pelo índice (`protocol.duplicate_item`), publicação, v2 com os alvos e o nível de
referência copiados, v1 intacta e depois aposentada, e a escada da automação nos dois sentidos.

As telas foram percorridas no navegador contra essa mesma API: catálogo com os 18 alvos separados
por tipo, protocolo com as duas versões juntas, o formulário escondendo "Onde olhar" na armadilha e
o nível de referência no registro de presença, e a recusa do atalho de automação chegando traduzida
na tela.

Esse exercício deixou no banco o protocolo `milho_padrao` (v1 aposentada, v2 em rascunho) e o alvo
de soja `phakopsora_pachyrhizi`. A v2 serve de ponto de partida para o protocolo real de milho; o
alvo de soja é legítimo, mas não faz parte da seed curada.

## Fora desta entrega

- Ligar o protocolo à geração de planos de amostragem e tornar explícita, por protocolo, a
  aplicabilidade das regras atuais do MIP-Soja. Hoje essa restrição vive em
  `cultivation.unsupported_monitoring`, herdada da fase 1.
- Escalas fenológicas das demais culturas. Só milho e soja estão transcritas; nas outras o estágio
  segue como texto livre, e é assim que a tela se comporta.
- Autenticação e isolamento por propriedade, adiados para o fim do roadmap.
