# Fase 8 — IA de imagem

A trilha B entra no produto. Ver o [roadmap](roadmap-cercosporiose.md), a
[pesquisa](pesquisa-cercosporiose.md#5-conjuntos-de-imagens-trilha-b) e o
[README da trilha B](../analytics/README.md).

**Critério:** uma foto percorre a fila e o resultado aparece com confiança e mapa de calor; as
métricas do modelo no conjunto de teste ficam registradas junto da versão.

## Fila de análises

- **`analysis_jobs`** — uma linha por foto a analisar. Toda foto enviada (fase 7) entra na fila na
  mesma transação.
- **`AnalysisWorker`** reserva a próxima tarefa com `UPDATE … WHERE id = (SELECT … LIMIT 1 FOR UPDATE
  SKIP LOCKED) RETURNING id`: dois workers nunca pegam a mesma foto, e a fila participa das
  transações do domínio sem broker.
- **Retentativas:** espera de 30 s, 1, 2 e 4 minutos; na quinta falha a tarefa fica `Failed` com o
  erro registrado. Tarefa presa em `Running` há mais de 10 minutos (a API caiu no meio) volta para a
  fila ao subir. `POST /api/photos/{id}/analysis` reenfileira — por exemplo, depois de trocar o modelo.
- **Processador simulado** (`Vision:Analyzer = Simulated`): resultado estável por foto, versão
  `simulado-0`, com aviso na tela de que não diz nada sobre a folha. Comprovou a fila antes do
  modelo existir e segue útil em testes.

## Avaliação

- **`Assessment`** — a interpretação da foto, separada da contagem de campo. Do modelo: classe,
  confiança, as três probabilidades, versão e mapa de calor. Da pessoa (fase 9): a classe que ela viu,
  apontando para a avaliação do modelo que revisou. Uma foto pode ter várias.
- Classes: **cercosporiose (*Cercospora* spp.)**, **folha sadia**, **outra mancha**. Não separa
  *C. zeina* de *C. zeae-maydis*: o sintoma não permite.

## Serviço de visão

`analytics/service`, `POST /vision/classify` (multipart) — carrega o ONNX mais recente de
`analytics/models/<versão>/` (ou `SMARTGRAO_MODEL_DIR`), aplica o pré-processamento de `labels.json`
e devolve classe, probabilidades e o mapa em PNG. `GET /vision/model` devolve as métricas da versão.

**O mapa é de oclusão, não Grad-CAM:** o ONNX não tem gradiente. A imagem é dividida em 8 × 8
quadrados; cada um é coberto, e a queda na probabilidade da classe escolhida diz quanto o modelo
dependia dele. São 64 inferências num lote, menos de 1 s por foto na CPU.

Na API, `Vision:Analyzer = Service` (padrão em `appsettings.json`) usa o serviço; o endereço é
`Analytics:BaseUrl`.

## Modelo 0.1.0

EfficientNet-B0 pré-treinada, 224 px, treinada pela trilha B com divisão agrupada por planta e
sessão de captura (nenhuma planta dos dois lados). No conjunto de teste (1.435 fotos):

| | Valor |
|---|---|
| Sensibilidade para cercosporiose | 90% |
| Precisão para cercosporiose | 87% |
| F1 macro | 0,92 |
| ECE (calibração) | 0,023 |
| Sensibilidade em fotos de campo | 89% |
| Sensibilidade em fotos de laboratório | 94% |

Números de conjuntos públicos de outros países — não são acurácia no campo brasileiro, e a tela de
aferimento diz isso. Na foto de demonstração, o mapa mostra o modelo olhando também o fundo, não só as
lesões: é o viés de captura que a pesquisa apontou, e um motivo a mais para a revisão humana.

## Telas

- Cada foto da parada mostra *"Analisando…"*, o resultado (*"Cercosporiose 64%"*) ou *"Análise
  falhou"*. A lista se atualiza sozinha enquanto há foto na fila.
- Abrir a foto mostra *"Suspeita de cercosporiose — confirme"*, as três probabilidades, o modelo, o
  botão **Mostrar onde o modelo olhou** e a revisão. *"Sem sinal"* vale para a folha, não para a
  lavoura.

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/photos?inspectionId=` | Fotos com situação da análise e avaliações |
| POST | `/api/photos/{id}/analysis` | Reenfileira a foto |
| GET | `/api/analysis-jobs?status=` | A fila, para acompanhar falhas |
| GET | `/api/assessments/{id}/heatmap` | O mapa de calor |

## Banco e migração

Migration: `AnalysisQueue`. **Aplicada**. Cria `assessments` e `analysis_jobs`
(índice `ix_analysis_jobs_queue` em situação e próxima tentativa).

## Verificação

- 208 testes de domínio (novos em `Analysis/AnalysisTests.cs`) e 7 do serviço Python.
- Contra o banco: a foto da vistoria de demonstração passou pela fila com o processador simulado e,
  reenfileirada com o serviço, saiu **cercosporiose 64%** pelo modelo 0.1.0, com mapa.
