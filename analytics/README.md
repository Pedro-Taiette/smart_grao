# Analytics — trilha B (modelo de visão)

Classificador de foto de folha de milho em três classes, exportado em ONNX para o serviço de
inferência da fase 8 do [roadmap](../docs/roadmap-cercosporiose.md):

| Índice | Classe | O que entra |
|---|---|---|
| 0 | `cercosporiose` | *Cercospora* spp. (sem separar *C. zeina* de *C. zeae-maydis*) |
| 1 | `saudavel` | folha sem sintoma foliar |
| 2 | `outras_manchas` | helmintosporiose, mancha-branca, ferrugem comum, polissora, bipolaris, northern leaf spot, ferrugem-branca |

Os conjuntos, as licenças e o porquê de cada escolha estão na
[pesquisa](../docs/pesquisa-cercosporiose.md#5-conjuntos-de-imagens-trilha-b). Imagens e pesos não
entram no git.

**Modelo 0.2.0 (em andamento):** o roteiro passo a passo para o grupo (protocolo craze_dev/craze_final,
quem faz o quê, comandos, erros comuns) está em [TREINO.md](TREINO.md), e os resultados em
[resultados.md](resultados.md).

## Ambiente

Python 3.13 e GPU NVIDIA com CUDA 12.8 (testado numa RTX 3050 6 GB). No Windows:

```powershell
cd analytics
py -3.13 -m venv .venv
.\.venv\Scripts\python -m pip install -r requirements.txt --extra-index-url https://download.pytorch.org/whl/cu128
```

Todos os comandos abaixo rodam de dentro de `analytics/` com o Python do `.venv`.

## Passo a passo

```powershell
python -m vision.download                 # baixa os 5 conjuntos (~20 GB de tráfego, ~24 GB no disco)
python -m vision.manifest                 # data/manifest.csv + cópias de 512 px em data/processed
python -m vision.split grouped            # data/splits/grouped.csv
python -m vision.split lodo --holdout craze
python -m vision.train --split data/splits/grouped.csv --run-name b0_grouped --epochs 25
python -m vision.train --split data/splits/lodo_craze.csv --run-name b0_lodo_craze --epochs 25
python -m vision.export --run runs/b0_grouped --version 0.1.0 --split data/splits/grouped.csv
python -m vision.gradcam --checkpoint runs/b0_grouped/best.pt --image foto.jpg --out mapa.png
```

Um conjunto só: `python -m vision.download craze`. O zip do Seasonal (4,6 GB, em `data/raw/seasonal/_zip`) pode ser apagado depois da extração. O resultado de cada download fica em
`data/download_log.json`; uma falha é registrada e o próximo conjunto segue.

Teste rápido do pipeline: `--epochs 2 --max-steps-per-epoch 10` no treino.

## Conjuntos

| Pasta | Origem | O que é baixado | Domínio |
|---|---|---|---|
| `plantvillage` | GitHub spMohanty, clone esparso | só `raw/color/Corn_(maize)___*` (3.852) | lab |
| `cds` | OSF s6ru5 | só `Dataset_Original` (1.571; o artigo cita 1.597) | campo, EUA |
| `seasonal` | Mendeley vy629dngm8 | zip de 4,6 GB; extrai GLS, saudável e ferrugem comum, sem as imagens aumentadas | campo, Bangladesh |
| `craze` | Kaggle, via `kagglehub` | tudo (2.355 imagens + `Database.csv`) | campo, África do Sul |
| `digipathos` | Redape doi:10.48432/XA1OVL | só milho, 5 doenças de *outras manchas* | lab, Brasil |

**Kaggle.** Em 02/11/2026 o `kagglehub` baixou o Maize In-Field sem login. Se passar a pedir
credencial, o status do `craze` vira `manual` no log: crie a chave em kaggle.com → Settings → API,
salve em `%USERPROFILE%\.kaggle\kaggle.json` (ou defina `KAGGLE_USERNAME` e `KAGGLE_KEY`) e rode
`python -m vision.download craze` de novo.

## Decisões que mudam os números

**Rótulos.**
- Craze é multirrótulo. GLS presente → `cercosporiose`, mesmo com outra doença junto: é a classe que
  importa detectar. Sem GLS, qualquer de NCLB, PLS, CR ou SR → `outras_manchas`. Só
  `NoFoliarSymptoms` → `saudavel`. Ficam fora as imagens só com *Other* (rasgo, inseto) ou
  *Unidentified*, e `NoFoliarSymptoms + Other` (256 no total).
- Seasonal: saem os arquivos do Augmentor (`*_original_*_<uuid>`), `resized_*`, `rsz_*` e as classes
  bacteriana e virose, que não cabem nas três.
- Digipathos só entra em `outras_manchas`, com no máximo 300 recortes por doença (sem o limite, os
  recortes de laboratório seriam metade da classe).

**Grupos (a unidade que não pode cruzar treino e teste).**
- Craze: planta pelo nome nas fotos CIMG (`..._1-140-1` e `..._1-140-2` são a planta 140); as demais,
  sessão de captura.
- CD&S e Seasonal: sessão de captura = câmera + janela de 5 minutos do horário EXIF (ou do nome
  `IMG_AAAAMMDD_HHMMSS`).
- PlantVillage: o `leaf-map.json` do repositório não cobre o milho. O grupo é o prefixo do arquivo +
  faixa de 50 números; nas culturas com leaf-map, ~11% das folhas cruzam uma faixa. Vazamento
  residual possível, só dentro do PlantVillage.
- Digipathos: os recortes não dizem de que foto saíram, então o grupo é a doença inteira e o conjunto
  é **só treino** (nunca validação nem teste).
- Duplicatas exatas (md5) são removidas (668, quase todas no Seasonal); quase duplicatas (dHash de
  256 bits, distância ≤ 10) unem os grupos.

**Divisão.** `grouped`: cada conjunto dividido por grupo em ~72/14/14, estratificado por classe.
`lodo_craze`: o Craze inteiro vira teste e os demais formam treino e validação. O aumento de dados
existe só no carregamento do treino (`vision/data.py`); nada aumentado vai para o disco.

**Treino.** EfficientNet-B0 (timm, ImageNet), 224 px, fine-tuning completo, AdamW 3e-4 com cosseno,
amostragem balanceada por classe (e, dentro da classe, por conjunto na raiz do tamanho), early
stopping pelo F1 macro da validação (paciência 5).

**Inferência.** Imagem inteira redimensionada para 224 × 224 (sem recorte central, para não cortar
lesão na borda), RGB, `/255`, normalização do ImageNet. Tudo isso está em `labels.json`.

## Saídas

- `runs/<execução>/`: `best.pt`, `history.json`, `eval_val.json`, `eval_test.json`, `summary.json`
  (fora do git).
- `models/<versão>/`: `model.onnx`, `labels.json`, `metrics.json` (métricas do teste e da validação,
  mais a conferência ONNX × PyTorch) e `train_config.json`. O 0.1.0 (~16 MB) está no git comum; do
  0.2.0 em diante os `.onnx` vão pelo Git LFS (ver [TREINO.md](TREINO.md#16-entregar)).
- `eval_*.json` traz recall e precisão por classe, F1 macro, matriz de confusão, calibração (ECE em
  15 faixas) e tudo isso também **por conjunto e por domínio** (campo × lab).

## Como ler os números

- O teste `grouped` mistura laboratório e campo. O número que importa é o de **campo**
  (`by_domain.campo`) e o do **Craze** (`by_dataset.craze`); o de laboratório é otimista.
- O `lodo_craze` mede o modelo num país e em câmeras que ele não viu. É o mais próximo de "foto de
  uma lavoura nova" que dá para medir sem lavoura própria — e ainda não é o campo brasileiro.
- Referência (Craze et al., 2022): treinado em laboratório, 5% de recall de cercosporiose em campo;
  treinado em campo, ~73% de acurácia. Números de 98–99% da literatura não servem de meta.

## Linha de base 0.1.0 (02/11/2026)

`models/0.1.0` = EfficientNet-B0 da execução `b0_grouped` (melhor época 15 de 20, parada antecipada).
ONNX conferido com o onnxruntime em 64 imagens do teste: diferença máxima de probabilidade 1,4e-6,
mesma classe em 100%.

**Divisão `grouped`, teste (1.435 imagens):**

| Recorte | n | Acurácia | F1 macro | Recall cerco. | Precisão cerco. | ECE |
|---|---|---|---|---|---|---|
| Tudo | 1.435 | 0,919 | 0,919 | 0,902 | 0,867 | 0,023 |
| Campo (CD&S + Craze + Seasonal) | 888 | 0,896 | 0,896 | 0,893 | 0,891 | 0,027 |
| Laboratório (PlantVillage) | 547 | 0,956 | 0,936 | 0,944 | 0,770 | 0,035 |
| **Craze** (campo, África do Sul) | 298 | **0,738** | **0,671** | **0,818** | 0,783 | 0,065 |

CD&S e Seasonal passam de 0,97 de acurácia, mas cada um é um local e poucas sessões: são fáceis
demais para medir generalização. No Craze, o saudável é o ponto fraco (recall 0,53).

**Leave-one-dataset-out, teste = Craze inteiro (2.075 imagens), treino sem nenhuma foto do Craze:**
acurácia 0,609, F1 macro 0,481, recall de cercosporiose 0,632 (precisão 0,666), recall de saudável
0,10, ECE 0,159. A validação do mesmo treino dava F1 0,977. Esse é o tamanho da troca de domínio:
**o número de validação nos conjuntos conhecidos não diz nada sobre uma lavoura nova**, e a confiança
do modelo fica mal calibrada fora do domínio.

## 0.2.0: o que mudou até agora

O Craze foi dividido por planta em `craze_dev` (escolha) e `craze_final` (avaliado uma vez, no fim),
com a atribuição fixa em `data/splits/craze_dev_final.csv`. Na mesma base (craze_dev), a linha de base
B0 dá F1 macro 0,460. O melhor até agora, DINOv2-S com LP-FT e WiSE 0,75, dá 0,620, com recall de
cercosporiose 0,85, recall de saudável 0,43 e ECE 0,06. Os detalhes estão em [resultados.md](resultados.md).
