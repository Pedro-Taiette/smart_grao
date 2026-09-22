# Fase 3 — Vistorias e coleta

A visita ao talhão: quem foi, quando, seguindo qual malha e qual protocolo, e o que anotou em cada
parada. Segue sem autenticação, por decisão de escopo.

**Critério do roadmap:** uma vistoria pode ser executada, concluída e repetida, preservando cada
visita.

## Como encaixa no que já existe

A fase 3 não inventa nada novo no mapa — ela amarra o que as fases anteriores construíram:

| Vem de | O que entrega para a vistoria |
|---|---|
| Fase 1 — cultivo | Qual ciclo está sendo acompanhado, e a cultura que valida o estádio |
| Fase 2 — protocolo | O que coletar em cada parada, na unidade de cada alvo |
| Amostragem | Onde são as paradas — a malha georreferenciada |
| Fase 3 — pessoa | Quem vai a campo |

## Modelo e regras

- **Pessoa (`Person`)** — cadastro simples, sem login. A vistoria precisa de um responsável com
  identidade estável muito antes de o sistema ter autenticação. Guardar o nome digitado direto na
  vistoria seria mais rápido hoje e mais caro depois: "João", "João S." e "joao" virariam três
  pessoas, e o histórico precisaria de reconciliação manual quando a autenticação chegar. Pertence
  a uma fazenda — não porque um técnico não possa atender várias, mas porque a fazenda é a âncora de
  acesso de todo o resto, e sair dela agora criaria uma entidade solta justamente no lugar por onde
  o isolamento vai entrar. Quem sai da equipe é **desativado, nunca excluído**.
- **Vistoria (`Inspection`)** — o agregado é a visita, e não o plano. Um plano é caminhado muitas
  vezes — o MIP pede amostragem semanal — e cada caminhada é uma vistoria própria, com as suas
  observações. É isso que faz o critério da fase se sustentar.
- **Aponta para uma versão de protocolo**, não para a família. A versão publicada é imutável, então
  o que foi pedido nesta visita continua legível exatamente como estava no dia, mesmo depois de a v2
  mudar tudo.
- **Situação:** `Agendada` → `Em andamento` → `Concluída`, ou `Não realizada` com motivo.
  Observações só entram em andamento; concluída é imutável. Remarcar só enquanto ninguém foi a
  campo.
- **Concluir exige ao menos uma parada.** Vistoria sem nada registrado não é vistoria concluída — é
  vistoria que não aconteceu, e para isso existe o cancelamento com motivo.
- **Observação** — o GPS gravado é o **efetivo**, onde a pessoa realmente parou, e não a coordenada
  que o plano mandava. Os dois raramente coincidem no mato, e é a diferença entre eles que diz se a
  caminhada seguiu a malha. Por isso a precisão vem junto: sem ela, não se distingue um desvio real
  de um erro do aparelho. Precisão acima de 30 m é **sinalizada, nunca recusada** — bloquear faria a
  pessoa em campo perder o registro por causa do aparelho.
- **Ocorrência fora dos pontos planejados** — observação sem ponto, com GPS próprio. Quem vê uma
  reboleira a caminho do próximo ponto precisa registrá-la onde ela está, e não no ponto planejado
  mais próximo.
- **Um ponto da malha é visitado uma vez por vistoria.** As ocorrências fora da malha são várias por
  visita, de propósito.
- **O estádio segue a escala da cultura**, como na fase 2: num cultivo de milho, "seis folhas" não é
  estádio, e `v6` grava `V6`.

### Contagem por alvo — e por que o zero importa

Cada alvo avaliado numa parada vira uma linha, **inclusive quando nada foi encontrado**. É o que
sustenta a regra do roadmap (linha 97): um resultado negativo precisa indicar quais alvos foram
avaliados, e nunca pode ser lido como "a planta está saudável". Alvo que ninguém olhou simplesmente
não tem linha — e essa ausência é visível na tela.

- A unidade vem do item do protocolo. Como a versão publicada é imutável, alvo e unidade são
  copiados para a contagem: a observação fica legível, e exportável na fase 7, sem carregar o
  protocolo junto.
- **O número manda sobre a detecção.** "3 lagartas, não detectado" é uma contradição que não chega
  ao banco: quem tem unidade de contagem tem `Detected` derivado do valor. Só o registro de presença
  carrega a detecção como informação própria.
- Valor fora da faixa da unidade é recusado — percentual acima de 100, nota fora de 1 a 9.

## Banco e migração

Migration: `20260922181433_InspectionsAndPeople`. **Aplicada** no ambiente configurado.

- Cria `people`, `inspections`, `observations` e `target_counts`.
- Todas as chaves estrangeiras da vistoria são `Restrict`: a vistoria é o registro de que alguém
  esteve naquele talhão naquele dia, e apagar talhão, cultivo, plano, protocolo ou pessoa não pode
  levar esse registro junto.
- `observations.location` é `geography(Point,4326)`, como a malha: "o que foi observado perto daqui"
  precisa virar `ST_DWithin` no banco, e não varredura na memória.
- `ix_observations_point_per_inspection` é único **com os nulos distintos entre si** — o padrão do
  PostgreSQL, e o oposto do que a fase 2 precisou. Aqui é o comportamento desejado: várias
  ocorrências fora da malha por visita.
- `ix_target_counts_target_id_detected` existe para a consulta das fases 5 e 6: "onde este alvo
  apareceu".
- A versão de linha da vistoria impede que iniciar e concluir em duas mãos ao mesmo tempo deixem a
  visita num estado que nenhuma das duas pediu.
- Sem seed: a equipe é cadastrada por fazenda.

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/people?farmId={id}&activeOnly={bool}` | A equipe da fazenda |
| POST | `/api/people` | Cadastrar pessoa |
| PUT | `/api/people/{id}` | Editar nome e função |
| PUT | `/api/people/{id}/status?active={bool}` | Tirar ou devolver à equipe |
| GET | `/api/inspections?cultivationId=&responsibleId=&status=` | Lista e agenda, sem as paradas |
| GET | `/api/inspections/{id}` | A visita com as paradas e contagens |
| POST | `/api/inspections` | Agendar |
| PUT | `/api/inspections/{id}/schedule` | Remarcar (só agendada) |
| PUT | `/api/inspections/{id}/start` | Iniciar |
| POST | `/api/inspections/{id}/observations` | Registrar uma parada |
| PUT | `/api/inspections/{id}/completion` | Concluir |
| PUT | `/api/inspections/{id}/cancellation` | Registrar que não aconteceu, com motivo |

**Sobre os relógios:** o horário de cada parada vem do **cliente**; início e conclusão, do
**servidor**. A parada precisa da hora em que a pessoa esteve no ponto — inclusive sem sinal, que é
o que a fase 4 vai tratar. Abrir e fechar a visita são atos que só existem com conexão, e aí o
relógio confiável é o do servidor.

`samplingPointId` nulo numa observação significa ocorrência fora da malha. `accuracyMeters` nulo
significa que não houve leitura de aparelho.

### Códigos de erro

| Código | HTTP | Situação |
|---|---|---|
| `inspection.protocol_not_published` | 409 | rascunho de protocolo indo a campo |
| `inspection.protocol_from_another_crop` | 422 | protocolo de outra cultura |
| `inspection.plan_from_another_cultivation` | 422 | malha de outro cultivo |
| `inspection.person_inactive` | 409 | responsável fora da equipe |
| `inspection.person_from_another_farm` | 422 | pessoa de outra fazenda |
| `inspection.not_in_progress` | 409 | parada registrada fora da execução |
| `inspection.not_scheduled` | 409 | remarcar depois de iniciada |
| `inspection.already_finished` | 409 | cancelar o que já foi concluído |
| `inspection.no_observations` | 400 | concluir sem nenhuma parada |
| `inspection.cancellation_needs_reason` | 422 | cancelar sem motivo |
| `inspection.duplicate_point_observation` | 409 | mesmo ponto duas vezes na visita |
| `inspection.point_from_another_plan` | 422 | ponto de outra malha |
| `inspection.count_from_another_protocol` | 422 | alvo fora da versão do protocolo |
| `inspection.duplicate_count` | 409 | mesmo alvo contado duas vezes na parada |
| `inspection.presence_takes_no_value` | 422 | número num registro de presença |
| `inspection.count_value_required` | 422 | unidade de contagem sem número |
| `inspection.count_value_out_of_range` | 422 | fora da faixa da unidade |
| `inspection.invalid_accuracy` | 422 | precisão negativa ou não numérica |
| `inspection.invalid_timing` | 422 | conclusão antes do início |
| `inspection.concurrent_change` | 409 | vistoria alterada durante a operação |
| `inspection.not_found`, `inspection.person_not_found` | 404 | |

## Telas

| Rota | Tela |
|---|---|
| `/farms/{farmId}/team` | A equipe da fazenda, separada entre quem está e quem saiu |
| `/farms/{farmId}/fields/{fieldId}/inspections` | As visitas do cultivo em andamento |
| `/inspections/{id}` | Executar a visita: iniciar, registrar paradas, concluir |

A execução fica na raiz, e não sob a fazenda: é o link que alguém abre no celular, no meio do
talhão, sem passar pela navegação.

Decisões de interface — vocabulário em `frontend/src/features/inspections/inspectionLabels.ts`:

- **O GPS é um botão, nunca um campo para digitar.** Digitar coordenada de bota no barro é a forma
  mais rápida de gravar a parada no lugar errado, e uma troca de sinal põe o ponto no outro
  hemisfério.
- **Mas o GPS falhar não custa a coleta.** Se o aparelho não responde, abre-se o caminho manual, com
  a opção de usar a coordenada planejada da parada. É pior do que o GPS efetivo — some a informação
  de quanto a caminhada desviou da malha — e muito melhor do que quem está no talhão voltar depois.
  A parada fica marcada como posição informada à mão.
- **Todo alvo do protocolo já vem marcado como avaliado**, e a pessoa desmarca o que não olhou. É o
  contrário do que seria natural, e de propósito: alvo avaliado que deu nada precisa virar registro,
  senão a visita é lida depois como "estava tudo limpo".
- **O campo de contagem já vem rotulado na unidade do alvo** — "% de plantas atacadas", "Capturas
  por armadilha" — com o lembrete de usar 0 quando avaliou e não encontrou nada.
- **A tela avisa quando faltaram alvos:** "2 alvo(s) do protocolo não foram avaliados nesta parada".
- O seletor de parada esconde as já registradas, e a lista de estádios vem da escala da cultura.

## Verificação

Testes de domínio em `backend/tests/SmartGrao.Domain.Tests/Inspections` (27 casos).

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

O fluxo foi exercido contra o banco configurado e percorrido no navegador: agendar, iniciar,
registrar parada com os três alvos (um deles zero), ocorrência fora da malha com precisão ruim,
recusa de parada repetida, conclusão, e uma segunda vistoria sobre a **mesma** malha preservando a
primeira. O caminho alternativo de GPS foi verificado com a leitura do aparelho indisponível.

## Fora desta entrega

- Foto da parada, coleta offline e sincronização — fase 4. A observação já é o lugar onde a imagem
  vai pendurar.
- Avaliação automática e revisão humana — fases 5 e 6.
- Cadência de vistorias (semanal para MIP, 2x/semana para ferrugem): o agendamento é avulso, e a
  repetição é manual.
- Autenticação e isolamento por propriedade, adiados para o fim do roadmap. Quando chegarem, a
  pessoa ganha o vínculo com o usuário e o histórico permanece válido.
