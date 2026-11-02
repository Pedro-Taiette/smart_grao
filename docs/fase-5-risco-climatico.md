# Fase 5 — Risco climático (camada 1)

O sistema sabe, todo dia e sem ninguém pedir, quais talhões de milho estão em condição favorável à
cercosporiose. Ver o [roadmap](roadmap-cercosporiose.md) e a [pesquisa](pesquisa-cercosporiose.md#2-índice-de-risco-climático).

**Critério:** um talhão real exibe o risco diário com a explicação, e um dia de risco alto produz uma
vistoria sugerida.

## O índice

**Índice de favorabilidade climática, não calibrado** — nunca "probabilidade". Calculado por
`CercosporaRiskIndex`, para cada dia, sobre a janela móvel dos **14 dias anteriores**.

| Regra | Valor | Origem |
|---|---|---|
| Temperatura favorável | 22 a 30 °C | Paul & Munkvold 2005; Casela & Ferreira 2003 |
| Umidade noturna | UR ≥ 90% | Paul & Munkvold 2005 |
| Janela móvel | 14 dias | Período latente mais curto (Casela & Ferreira 2003): o que favoreceu a infecção nas últimas duas semanas é o que pode virar lesão agora |
| Fase acompanhada | V4 a R2 | Acumulação de V4 a V12 e temperatura noturna de V12 a R2 (Paul & Munkvold 2005) |
| Noite | 20h às 6h, hora local | **Decisão de projeto** — a fonte não define a hora |
| Noite úmida | ≥ 6 h com UR ≥ 90% | **Decisão de projeto** |
| Dia favorável | ≥ 6 h entre 22 e 30 °C | **Decisão de projeto** |
| Risco alto | ≥ 7 noites úmidas **e** ≥ 7 dias favoráveis na janela | **Decisão de projeto** |
| Risco moderado | ≥ 4 de cada | **Decisão de projeto** |
| Dados mínimos | 10 dos 14 dias com clima | **Decisão de projeto**; abaixo disso, "sem clima suficiente" |

**Fatores do cultivo** (fase 4) ajustam um nível, como heurística a partir da escala da Purdue:
dois agravantes (híbrido suscetível ou moderadamente suscetível, milho sobre milho, plantio direto)
sobem um nível; dois atenuantes (híbrido resistente ou moderadamente resistente, rotação,
convencional) descem um. **Clima baixo continua baixo**: sem clima favorável não há infecção, por
pior que seja o cultivo.

Em regiões quentes como o Mato Grosso, quase todo dia é "favorável" pela temperatura, e o índice
passa a ser dirigido pela umidade — o que bate com Mesquini et al. (2020), que apontam a chuva como
fator determinante das epidemias.

## Estádio

O estádio vem dos registros da fase 1; o sistema não o adivinha. O último registro até o dia diz se
a lavoura está antes de V4, na janela (V4–R2) ou depois. Registro com mais de 14 dias é tratado
como **desatualizado**, e a tela pede o estádio atual.

## Vistoria sugerida

Risco **alto** hoje, em cultivo em andamento, com estádio na janela — ou desconhecido, ou
desatualizado — sugere vistoria. A sugestão aparece em **Hoje** e na aba de risco do talhão, e só
vira vistoria quando a pessoa confirma: o botão abre o agendamento já com o protocolo de
cercosporiose. Automatizar não é decidir em silêncio.

## Clima

- **Fonte:** Open-Meteo, sem chave, no centroide do talhão, em hora local (`timezone=auto`).
  Reanálise ERA5 (`archive-api`) até 7 dias atrás; previsão (`api.open-meteo.com`) para os últimos
  dias e os 3 próximos. A UR é do ar a 2 m, não do dossel.
- **Tabela `weather_hours`** — uma linha por talhão, data e hora, com a origem (`Archive`,
  `Forecast` e, nas fases seguintes, `Station`). Reatualizar substitui o intervalo.
- **Atualização automática:** `WeatherRefreshWorker` atualiza todo cultivo de milho em andamento ao
  subir a API e a cada `Weather:RefreshIntervalHours` (padrão 6; `0` desliga).
- **Safra passada:** "Buscar o clima da safra" baixa a reanálise do ciclo inteiro e recalcula a
  série. Sem lavoura no semestre, é a demonstração da camada 1.

## API

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/cultivations/{id}/cercospora-risk` | Série diária, nível de hoje, estádio, fatores e se sugere vistoria |
| POST | `/api/cultivations/{id}/weather/refresh` | Busca o clima que falta e devolve a série recalculada |
| GET | `/api/cercospora-alerts?farmId={id}` | Risco de hoje de cada cultivo de milho em andamento da fazenda |

Erros: `risk.not_corn` (422) e `risk.weather_unavailable` (502).

## Telas

- Aba **Risco de cercosporiose** na página do talhão: nível de hoje com a explicação (*"9 noites
  úmidas e 14 dias com temperatura entre 22 e 30 °C nos últimos 14 dias. Híbrido suscetível ·
  plantio direto"*), aviso de estádio, a sugestão de vistoria e o **risco dia a dia** do ciclo, um
  quadrado por dia, com a previsão hachurada. Com mais de um cultivo de milho, escolhe-se a safra.
- **Hoje** ganha o grupo "Risco de cercosporiose" com os talhões em risco moderado ou alto.
- A rampa de cor é ordinal, de um tom, validada contra a superfície clara; o nível sempre vem
  escrito ao lado da cor.

## Banco e migração

Migration: `WeatherHours`. **Aplicada** no ambiente configurado. Cria `weather_hours`, chave
`(field_id, date, hour)`, com exclusão em cascata pelo talhão.

## Verificação

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

190 testes de domínio; 15 novos em `Risk/CercosporaRiskIndexTests.cs`.

Contra o banco e o Open-Meteo:

- **Talhão Sul (FarmLab), safra em andamento:** clima desde 01/09/2026 e previsão até 05/11. O risco
  foi moderado em boa parte de outubro e hoje está baixo. O estádio V6, de 15/09, aparece como
  desatualizado.
- **Talhão Leste (Fazenda Santa Rita, Sorriso-MT), safrinha 2026 de demonstração:** plantio em 10/02,
  encerramento em 01/07, híbrido suscetível, rotação e plantio direto. A reanálise dá risco alto de
  fevereiro até o começo de maio e baixo a partir da seca — o padrão esperado da safrinha.
