# Fase 9 — Revisão humana e aferimento

O sistema mede o quanto ele próprio acerta. Ver o [roadmap](roadmap-cercosporiose.md).

**Critério:** o painel mostra os números calculados a partir de revisões. Sem lavoura acompanhada no
semestre, os números do modelo vêm do conjunto de teste e a antecedência fica sem valor, com o
motivo na tela.

## Revisão

- Na foto, a pessoa diz o que ela mostra: **cercosporiose**, **outra mancha** ou **folha sadia**. A
  revisão vira uma avaliação `Human` que aponta para a avaliação do modelo revisada; revisar de novo
  cria outra, e vale a mais recente.
- O nome de quem revisa fica lembrado no navegador; não há login (autenticação adiada por decisão).
- A revisão é a verdade de campo do aferimento e o rótulo do treino da próxima versão.

## Painel `/aferimento`

- **Modelo no conjunto de teste** — sensibilidade e precisão para cercosporiose, F1 macro, ECE,
  matriz de confusão, e o recorte campo × laboratório e por conjunto de origem. Vem de
  `metrics.json` da versão em uso, pelo serviço de visão.
- **Modelo contra as revisões** — as mesmas medidas, calculadas só sobre fotos revisadas aqui. O
  processador simulado fica fora da conta, e a tela diz quantas fotos foram excluídas.
- **Antecedência** — para cada cultivo com cercosporiose confirmada na folha: quantos dias antes da
  primeira confirmação o índice climático chegou a risco alto e o satélite marcou uma área, olhando
  até 45 dias para trás. Sem confirmação, a tela explica que a antecedência só existe com folha
  confirmada.
- **Exportar fotos revisadas** — CSV com foto, talhão, cultivo, vistoria, parada, folha, estádio,
  hora, GPS, origem, se é simulada, a avaliação do modelo e a revisão.

## API

| Método | Rota | Uso |
|---|---|---|
| POST | `/api/assessments/{id}/review` | `{ label, reviewer, notes }` |
| GET | `/api/calibration` | Modelo, revisões e antecedência |
| GET | `/api/calibration/reviewed-photos.csv` | A exportação |

## Fica para depois da entrega

Taxa de alarmes falsos por camada, desempenho por estádio e curva de calibração na tela. Os dados
para os três já estão gravados.
