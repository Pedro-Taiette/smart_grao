# Resultados da trilha B (modelo 0.2.0)

Uma linha por execução, **sempre no `craze_dev`** (parte `test` de `data/splits/lodo_craze_halves.csv`).
O `craze_final` não aparece aqui até a avaliação única do fim (ver [TREINO.md](TREINO.md), passo 12).

Como gerar a linha de uma execução sua (cola direto na tabela):

```
python -m vision.report runs/<sua_execucao> --markdown
```

Nome da execução: `<iniciais>_<backbone>_<receita>_s<semente>`, por exemplo `pt_s_lpft6_s1`. Cada pessoa
só acrescenta linhas; não apague nem edite a linha de outra pessoa.

Colunas: `val F1` = validação dos conjuntos conhecidos (só diz se o treino funcionou); `dev F1` = F1 macro
no craze_dev (a métrica que escolhe); recall/precisão de cercosporiose e recall de saudável no craze_dev;
ECE = erro de calibração no craze_dev (menor é melhor).

## Linha de base

| execução | val F1 | dev F1 | recall cerco | precisão cerco | recall saudável | ECE |
|---|---|---|---|---|---|---|
| b0_lodo_craze (modelo do 0.1.0 treinado sem Craze) | 0,977 | 0,460 | 0,620 | 0,658 | 0,071 | 0,172 |

## Rodada 1 — backbone congelado + regressão logística (`vision.probe`)

C escolhido pelo F1 no craze_dev. `cds` e `seasonal` = F1 macro nos folds auxiliares (treino sem Craze,
teste no conjunto inteiro; o CD&S não tem saudável, o Seasonal sem Craze só tem saudável de laboratório
no treino, por isso os números são baixos).

| backbone | px | dev F1 | recall cerco | precisão cerco | recall saudável | ECE | cds F1 | seasonal F1 |
|---|---|---|---|---|---|---|---|---|
| efficientnet_b0 | 224 | 0,420 | 0,635 | 0,582 | 0,071 | 0,371 | 0,329 | 0,126 |
| dinov2_s | 224 | 0,518 | 0,800 | 0,633 | 0,195 | 0,264 | 0,401 | 0,109 |
| dinov2_s | 448 | 0,523 | 0,742 | 0,653 | 0,177 | 0,236 | 0,545 | 0,140 |
| dinov2_b_reg4 | 224 | 0,497 | 0,729 | 0,629 | 0,159 | 0,211 | 0,397 | 0,109 |
| dinov2_b_reg4 | 448 | 0,521 | 0,678 | 0,670 | 0,186 | 0,311 | 0,571 | 0,188 |
| plantclef_b (Pl@ntNet pc24_all) | 224 | 0,431 | 0,668 | 0,599 | 0,053 | 0,363 | 0,444 | 0,142 |
| bioclip (ViT-B/16) | 224 | 0,424 | 0,562 | 0,574 | 0,133 | 0,413 | 0,455 | 0,102 |
| bioclip2 (ViT-L/14) | 224 | 0,538 | 0,637 | 0,638 | 0,345 | 0,235 | 0,457 | 0,238 |

## Rodada 2 — fine-tuning (`vision.train`, 224 px, semente 42, RTX 3050 6 GB)

| execução | val F1 | dev F1 | recall cerco | precisão cerco | recall saudável | ECE |
|---|---|---|---|---|---|---|
| s_ft_plain (DINOv2-S, FT simples 12 épocas) | 0,975 | 0,501 | 0,582 | 0,691 | 0,186 | 0,253 |
| s_lpft (DINOv2-S, LP-FT 2+12, LP lr 1e-3; fim, do histórico) | 0,969 | 0,586 | 0,787 | – | 0,239 | – |
| **s_lpft6** (DINOv2-S, LP-FT 2+6) | 0,947 | 0,607 | 0,833 | 0,688 | 0,327 | 0,059 |
| s_lpft6 WiSE a=0.25 | 0,852 | 0,543 | 0,886 | 0,606 | 0,443 | 0,109 |
| s_lpft6 WiSE a=0.5 | 0,928 | 0,596 | 0,860 | 0,644 | 0,496 | 0,076 |
| **s_lpft6 WiSE a=0.75** (melhor até agora) | 0,942 | 0,620 | 0,846 | 0,678 | 0,425 | 0,062 |
| b_lpft8 (DINOv2-B reg4, LP-FT 2+8) | 0,969 | 0,551 | 0,734 | 0,711 | 0,168 | 0,105 |
| b_lpft8 WiSE a=0.75 | 0,962 | 0,560 | 0,777 | 0,686 | 0,203 | 0,086 |
| b2_lpft6 (BioCLIP 2, 12 de 24 blocos congelados) — **interrompido na época 4 de 8**: fim do LP | 0,884 | 0,573 | 0,777 | – | 0,416 | – |
| b2_lpft6 — época 3 (1ª de FT) | 0,895 | 0,584 | 0,770 | – | 0,407 | – |

Observações:
- Fora do domínio o F1 sobe nas primeiras épocas e depois cai enquanto a validação continua subindo:
  treinar mais deixa o modelo melhor nos conjuntos conhecidos e pior numa lavoura nova. Por isso as
  receitas usam poucas épocas e número fixo (`--select last`), não parada pela validação.
- Temperatura ajustada no craze_dev para s_lpft6: T = 1,29 (ECE 0,059 → 0,027 no próprio dev).
- Uma semente só por linha: diferenças menores que ~0,02 no dev F1 podem ser ruído.
- Os folds auxiliares (cds, seasonal) só foram medidos na rodada 1.

## Rodada 3 — grupo (DINOv2-S × Pl@ntNet), ver TREINO.md

| execução | val F1 | dev F1 | recall cerco | precisão cerco | recall saudável | ECE |
|---|---|---|---|---|---|---|
