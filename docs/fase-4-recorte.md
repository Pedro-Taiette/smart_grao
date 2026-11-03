# Fase 4 — Recorte milho + cercosporiose

Prepara o que já existe para o foco do semestre, sem análise nova. Ver o
[roadmap](roadmap-cercosporiose.md) e a [pesquisa](pesquisa-cercosporiose.md).

**Critério:** um cultivo de milho é cadastrado com os fatores de risco e recebe uma vistoria de
cercosporiose sem que nenhuma tela mencione soja.

## Modelo e regras

- **Fatores de risco (`CercosporaRiskFactors`)** — três respostas sobre o cultivo: reação do híbrido
  (resistente → suscetível), o que veio antes (milho sobre milho ou rotação) e o sistema de plantio
  (direto ou convencional). Cada uma aceita "não sei", que é o padrão: inventar "suscetível" criaria
  um risco que ninguém declarou.
- São **níveis ordinais, não pesos**: a literatura ordena esses fatores, mas não publica
  multiplicador. O índice da fase 5 os usa para subir ou descer um nível.
- Só valem para **milho**; num cultivo de outra cultura, um fator conhecido é recusado.
- Diferente do resto do contexto do cultivo, que é imutável, **os fatores se corrigem depois** — até
  num ciclo encerrado. A reação do híbrido costuma vir do catálogo da semente depois do cadastro, e
  o recálculo de safras passadas da fase 5 precisa dela.
- **Órgão `LowerLeaves`** — folhas de baixo, até a imediatamente abaixo da espiga. É onde a doença
  começa; a folha da espiga continua sendo a da avaliação de severidade.

## Protocolo semeado

`cercosporiose_deteccao` v1, **publicado**, milho, alvo `cercospora_zeina`:

| Onde olhar | Unidade | Fotos | O que a instrução pede |
|---|---|---|---|
| Folhas de baixo | % de plantas atacadas | 2 | 10 plantas por parada, da base até a folha abaixo da espiga; lesão de 1–3 mm com halo amarelo contra a luz, ou retangular limitada pelas nervuras; foto da folha inteira e close contra o sol |
| Folha da espiga | % de plantas atacadas | 1 | Nas mesmas plantas, se a lesão já chegou à folha da espiga |

"Atacada" é **planta com ao menos uma lesão compatível** — critério de projeto, sem fonte, porque
nenhuma fonte define presença para detecção. A lesão jovem não é diagnóstica, e a instrução diz isso
à pessoa em campo. Sem nível de referência: os limiares publicados são de decisão de fungicida, fora
do escopo.

Os dois itens com o mesmo alvo em órgãos diferentes dão a altura da doença na planta: só nas folhas
de baixo é início; na folha da espiga, ela já subiu.

## Banco e migração

Migration: `CercosporaFocus`. **Aplicada** no ambiente configurado.

- `cultivations` ganha `risk_hybrid_reaction`, `risk_crop_sequence` e `risk_tillage`, com `Unknown`
  nos cultivos existentes.
- Insere o protocolo e os dois itens com ids fixos (`b1000000-…-000000000001`, `…101`, `…102`),
  com `ON CONFLICT DO NOTHING`. O rollback só remove o protocolo se nenhuma vistoria o usou.

## API

| Método | Rota | Uso |
|---|---|---|
| POST | `/api/cultivations` | Aceita `riskFactors` opcional; ausente é "não sei" nos três |
| PUT | `/api/cultivations/{id}/risk-factors` | Corrige `hybridReaction`, `cropSequence`, `tillage` |

Os cultivos passam a devolver `riskFactors`. Erros: `cultivation.unknown_risk_factor` (422) e
`cultivation.risk_factors_not_applicable` (422).

## Telas

- **Milho é a cultura fixa** (`focusCrop` em `cropLabels.ts`). Some o seletor de cultura do cultivo,
  dos protocolos e do catálogo. Soja e as demais culturas continuam no modelo e no banco.
- O cadastro do cultivo faz as três perguntas na língua do produtor: *"Como o híbrido reage à
  cercosporiose?"*, *"O que estava plantado antes neste talhão?"*, *"Como foi o plantio?"*. A
  segunda já vem preenchida pelo histórico do talhão quando há cultivo anterior.
- O cartão do cultivo resume o risco (*"Híbrido suscetível · milho sobre milho · plantio direto"*) e
  tem o botão **Risco de cercosporiose** para corrigir.
- Na vistoria, o protocolo de cercosporiose é o primeiro publicado de milho e vem selecionado.

## Verificação

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

175 testes de domínio; 8 novos em `Cultivations/CultivationTests.cs`. Contra o banco: o protocolo
aparece publicado com os dois itens, e o endpoint de risco gravou, recusou valor fora do catálogo e
voltou a "não sei" no cultivo existente.

## Pendente

- O banco tem o protocolo `milho_padrao` v3 **publicado**, de testes anteriores. Ele aparece como
  segunda opção na vistoria; aposentá-lo deixa só o de cercosporiose.
