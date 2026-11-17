# Roteiro de treino do modelo 0.2.0 (para o grupo)

Este roteiro leva do clone do repositório até o modelo `analytics/models/0.2.0/`. Siga os passos na
ordem e não pule nenhum. Os resultados até agora estão em [resultados.md](resultados.md). O que é cada
termo está no [glossário](#glossário), no fim.

**Objetivo.** Ter o classificador de folha de milho (cercosporiose / saudável / outras manchas) que
erre menos numa lavoura que ele nunca viu. A melhor medida que temos disso é o **craze_dev**: metade
das fotos do Craze (África do Sul), que nenhum treino de escolha usa.

## Quem faz o quê

| Quem | O que | Passos |
|---|---|---|
| Todos | preparar a máquina e os dados | 1 a 7 |
| **Pessoa A** | DINOv2-S (o melhor até agora), 3 sementes + WiSE | 8A, 9 |
| **Pessoa B** | DINOv2 da Pl@ntNet (pc24_all), 3 sementes + WiSE | 8B, 9 |
| A e B juntas | comparar na tabela, **só pelo craze_dev**, e escolher | 10 |
| Quem venceu | temperatura e TTA no craze_dev | 11 |
| **Uma pessoa só**, combinada antes | avaliar no **craze_final, uma vez** | 12 |
| Quem venceu | teste agrupado, treino de produção, exportação, entrega | 13 a 16 |

## A regra que não pode ser quebrada

- **craze_dev** (parte `test` de `lodo_craze_halves.csv`): use à vontade para comparar e escolher.
- **craze_final** (parte `final`): avaliado **uma vez só**, no passo 12, por **uma pessoa**, com a
  receita já escolhida e anotada. Depois disso não se muda mais nada na receita.
- Quem olhar o craze_final antes (ou duas vezes, ou para escolher entre duas opções) **invalida o
  número**: ele deixa de medir "lavoura nova" e vira mais um conjunto de ajuste. Se acontecer, avise
  o grupo. O número tem que ser reportado como contaminado.
- Nenhuma foto do Craze entra no treino de escolha. As divisões já garantem isso; não edite os CSV.

---

## 1. A máquina

| Item | Mínimo | Testado |
|---|---|---|
| GPU | NVIDIA com 6 GB (DINOv2-S usa ~3 GB; Pl@ntNet ViT-B com lote 32, ~4,5 GB) | RTX 3050 Laptop 6 GB |
| Driver | compatível com CUDA 12.8 (`nvidia-smi` mostra "CUDA Version: 12.8" ou maior) | 617.42 (CUDA 13.4) |
| RAM | 16 GB | 16 GB |
| Disco livre | **40 GB** (imagens ~25 GB, venv ~6 GB, pesos e execuções ~5 GB, folga) | — |
| Python | 3.13 | 3.13 |
| Sistema | Windows 10/11 (PowerShell ou Git Bash) ou Linux | Windows 11 |

Sem GPU NVIDIA dá para rodar os passos 1 a 7 e a avaliação, mas o treino fica lento demais.

## 2. Clonar e criar a sua branch

Onde: qualquer pasta sua.

```
git lfs install
git clone https://github.com/Pedro-Taiette/smart_grao smart_grao
cd smart_grao
git checkout -b treino/<seu-nome>
```

`git lfs install` é uma vez por máquina (instale o [Git LFS](https://git-lfs.com) antes, se o comando
não existir). Se este arquivo (`analytics/TREINO.md`) não existir depois do clone, o PR #6 ainda não
entrou na `main`: rode `git checkout feature/fase-4-recorte-cercosporiose` antes de criar a sua branch.

Nunca trabalhe direto na `main`.

## 3. Criar o ambiente Python (uns 10 min)

Onde: `smart_grao/analytics`.

PowerShell (Windows):
```
cd analytics
py -3.13 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt --extra-index-url https://download.pytorch.org/whl/cu128
```

Git Bash (Windows):
```
cd analytics
py -3.13 -m venv .venv
source .venv/Scripts/activate
python -m pip install -r requirements.txt --extra-index-url https://download.pytorch.org/whl/cu128
```

Linux:
```
cd analytics
python3.13 -m venv .venv
source .venv/bin/activate
python -m pip install -r requirements.txt --extra-index-url https://download.pytorch.org/whl/cu128
```

Deu certo quando termina com `Successfully installed ...`. **Daqui em diante, todo comando roda de
dentro de `analytics/` com o ambiente ativado** (o prompt mostra `(.venv)`). Se fechar o terminal,
ative de novo (só a linha `Activate.ps1` ou `source ...`).

O `requirements.txt` cobre tudo (os pesos do BioCLIP e da Pl@ntNet entram pelo `timm`, sem `open_clip`).

## 4. Conferir a GPU (1 min)

```
python -c "import torch; print(torch.__version__, torch.cuda.is_available(), torch.cuda.get_device_name(0))"
```

Deu certo: `2.11.0+cu128 True NVIDIA GeForce ...`.
Se aparecer `False` ou erro, veja [Erros comuns](#erros-comuns).

## 5. Baixar as imagens (horas; ~20 GB de tráfego)

```
python -m vision.download
```

Baixa os cinco conjuntos do manifesto. Deu certo quando o fim mostra os cinco com `"ok"`:

```
{ "cds": "ok", "craze": "ok", "digipathos": "ok", "plantvillage": "ok", "seasonal": "ok" }
```

Um conjunto que falhou não impede os outros. Rode de novo só ele, por exemplo `python -m vision.download craze`
(o que já baixou é pulado). O Craze vem do Kaggle e às vezes pede login: veja [Erros comuns](#erros-comuns).
Depois que o Seasonal extrair, o zip (`data/raw/seasonal/_zip`, 4,6 GB) pode ser apagado.

Os conjuntos `bdmaize`, `figmaize` e `plantdoc` foram avaliados e **ficaram fora do treino** (motivo
no fim deste arquivo). Não precisa baixar. Se quiser olhar: `python -m vision.download bdmaize figmaize plantdoc`.

**Não faça commit de `data/download_log.json`** se ele mudar.

## 6. Preparar as imagens (uns 10–30 min)

```
python -m vision.manifest
```

Faz as cópias reduzidas em `data/processed` e reescreve `data/manifest.csv`. Depois confira:

```
git status --short data/
```

Deu certo quando **não aparece** `data/manifest.csv` nem `data/manifest.summary.json` na lista.
Isso prova que você tem exatamente as mesmas imagens que o resto do grupo. Se aparecerem como
modificados, **pare**: alguma imagem veio diferente. Rode `git diff --stat data/manifest.summary.json`,
mande o resultado para o grupo e não faça commit desses arquivos.

## 7. Gerar as divisões (1 min)

```
python -m vision.split lodo --holdout craze --halves
python -m vision.split lodo --holdout cds --exclude craze
python -m vision.split lodo --holdout seasonal --exclude craze
python -m vision.split grouped
```

Deu certo quando o primeiro mostra exatamente:

```
"by_split": { "train": 7637, "val": 1137, "test": 1038, "final": 1037 }
```

Por que é igual em toda máquina: quais fotos do Craze são `craze_dev` (`test`) e quais são
`craze_final` (`final`) está fixado em `data/splits/craze_dev_final.csv`, que está no git. A divisão
lê esse arquivo em vez de sortear. O resto (treino/validação dos outros conjuntos) sai do manifesto
com a semente 42, também igual em toda máquina se o passo 6 deu certo. Se aparecer `N imagens fora de
craze_dev_final.csv`, o seu manifesto está diferente: pare e avise.

---

## 8A. Pessoa A — DINOv2-S (uns 10 min por semente + 2 min de WiSE)

Esta é a receita do `s_lpft6`, o melhor até agora: LP-FT (2 épocas só a cabeça, 6 épocas de tudo),
aumento forte, mixup/cutmix, EMA, 224 px. Rode as três sementes, **uma de cada vez** (nunca dois
treinos ao mesmo tempo na mesma máquina):

```
python -m vision.train --split data/splits/lodo_craze_halves.csv --run-name XX_s_lpft6_s42 --seed 42 --arch dinov2_s --epochs 6 --lp-epochs 2 --lp-lr 3e-3 --batch-size 32 --lr 4e-5 --layer-decay 0.75 --drop-path 0.1 --weight-decay 0.05 --ema 0.999 --strong-aug --mixup 0.2 --cutmix 1.0 --select last --eval-test-each-epoch
python -m vision.wise --run runs/XX_s_lpft6_s42 --alphas 0.25 0.5 0.75
```

Troque `XX` pelas suas iniciais. Repita trocando `s42`/`--seed 42` por `s1`/`--seed 1` e `s2`/`--seed 2`.

O que aparece: `treino 7637 | validacao 1137 | teste 1038`, depois uma linha por época com
`"phase": "lp"` (2 vezes) e `"phase": "ft"` (6 vezes), cada uma com `test_macro_f1` (é o craze_dev).
No fim, um resumo com `"test": {... "macro_f1": ...}`. Na RTX 3050 cada época leva ~50 s. Para
comparar: a semente 42 deu dev F1 0,607 (0,620 com WiSE 0,75). A GPU não é determinística, então
outra máquina dá algo como ±0,02.

## 8B. Pessoa B — DINOv2 da Pl@ntNet (uns 30 min por semente + 4 min de WiSE)

Mesmo backbone do DINOv2-B (ViT-B/14 com registradores), mas ajustado pela Pl@ntNet em 1,4 milhão de
fotos de plantas (PlantCLEF 2024). Ainda não foi ajustado aqui (só a rodada congelada). A receita é a
mesma do `b_lpft8`:

```
python -m vision.train --split data/splits/lodo_craze_halves.csv --run-name XX_pn_lpft8_s42 --seed 42 --arch plantclef_b --epochs 8 --lp-epochs 2 --lp-lr 3e-3 --batch-size 32 --lr 3e-5 --layer-decay 0.75 --drop-path 0.1 --weight-decay 0.05 --ema 0.999 --strong-aug --mixup 0.2 --cutmix 1.0 --select last --eval-test-each-epoch
python -m vision.wise --run runs/XX_pn_lpft8_s42 --alphas 0.25 0.5 0.75
```

Repita com `--seed 1` e `--seed 2`. A primeira vez baixa ~370 MB de pesos. Cada época leva ~2,5 min na
RTX 3050. Se faltar memória, use `--batch-size 16 --accum 2` (o lote efetivo continua 32).

Se der tempo depois das 3 sementes, uma variação que vale testar nas duas: `--epochs 4` (o F1 fora do
domínio costuma cair quando se treina mais).

## 9. Anotar o resultado (1 min por execução)

```
python -m vision.report runs/XX_s_lpft6_s42 runs/XX_s_lpft6_s1 runs/XX_s_lpft6_s2 --markdown
```

Copie as linhas que saem (sem o cabeçalho) para a tabela "Rodada 3" de [resultados.md](resultados.md).
Cada execução sai numa linha e cada alfa do WiSE em outra. Só acrescente linhas. Depois:

```
git add resultados.md
git commit -m "Resultados <backbone> sementes 42, 1, 2"
git push -u origin treino/<seu-nome>
```

Abra um PR para a `main`. **Só `resultados.md` (e código, se você mudou) vai para o git.** Nunca
`runs/`, `data/raw`, `data/processed`, `*.pt` nem imagens. O `.gitignore` já bloqueia a maior parte;
confira com `git status` antes do commit.

## 10. Escolher o vencedor (A e B juntas)

Regra, decidida antes de olhar:
1. Para cada backbone, use a **média do dev F1 das 3 sementes**, com e sem WiSE (alfa 0,25/0,5/0,75).
2. Vence a combinação backbone + alfa com a maior média. Se a diferença for menor que 0,01, desempate
   pelo recall de cercosporiose e, depois, pelo tamanho/velocidade (DINOv2-S é 4× menor).
3. Para os próximos passos, use a **semente 42** do vencedor (não a melhor semente, para não escolher
   pela sorte).

Anote a escolha em `resultados.md` (backbone, alfa, comando exato) **antes** do passo 12.

Daqui em diante, `VENC` é o nome da execução vencedora com WiSE gravado. Grave o WiSE escolhido:

```
python -m vision.wise --run runs/<execução semente 42> --save 0.75
```

Troque `0.75` pelo alfa escolhido. Isso cria `runs/<execução>_wise0.75/` (esse é o `VENC`). Se o
vencedor for sem WiSE, `VENC` é a própria execução.

## 11. Temperatura e TTA, só no craze_dev (2 min)

```
python -m vision.evaluate --checkpoint runs/VENC/best.pt --split data/splits/lodo_craze_halves.csv --part test --fit-temperature
python -m vision.evaluate --checkpoint runs/VENC/best.pt --split data/splits/lodo_craze_halves.csv --part test --tta flip
```

O primeiro mostra `"temperature": T`. Anote o T em `resultados.md`. A temperatura não muda a classe
prevista, só deixa a confiança honesta (ECE menor). No s_lpft6 deu T = 1,29.

O segundo mede o TTA (média com a foto espelhada). **O serviço não faz TTA**, então o número oficial
é sem TTA. Anote o ganho só como informação.

## 12. craze_final — UMA vez, UMA pessoa (2 min)

Só depois do passo 10 anotado. Combine no grupo quem roda. Use a temperatura do passo 11:

```
python -m vision.evaluate --checkpoint runs/VENC/best.pt --split data/splits/lodo_craze_halves.csv --part final --temperature T
python -m vision.evaluate --checkpoint runs/b0_lodo_craze/best.pt --split data/splits/lodo_craze_halves.csv --part final
```

O segundo é a linha de base (o EfficientNet-B0 do 0.1.0 treinado sem Craze) na mesma metade, para
comparar na mesma base. O `runs/b0_lodo_craze/best.pt` existe só na máquina do Pedro. Em outra
máquina, treine antes (uns 25 min) com
`python -m vision.train --split data/splits/lodo_craze.csv --run-name b0_lodo_craze --epochs 25`.

Os resultados ficam em `runs/VENC/eval_lodo_craze_halves_final.json` e
`runs/b0_lodo_craze/eval_lodo_craze_halves_final.json`. Copie para `resultados.md` (seção nova
"craze_final") o F1 macro, o recall e a precisão de cercosporiose, o recall de saudável e o ECE dos
dois, com a data e quem rodou. **Não rode de novo.**

## 13. Teste agrupado do 0.2.0 (mesmo tempo do passo 8)

O `metrics.json` precisa do teste agrupado, comparável ao 0.1.0, de um treino que não viu essas
fotos. Repita a receita vencedora (mesmos argumentos do passo 8, semente 42) trocando só a divisão e
o nome:

```
python -m vision.train --split data/splits/grouped.csv --run-name XX_grouped_<receita> <mesmos argumentos do passo 8, sem --split e --run-name>
python -m vision.wise --run runs/XX_grouped_<receita> --save 0.75
```

O resultado fica em `runs/XX_grouped_<receita>_wise0.75/eval_test.json` (ou `runs/XX_grouped_<receita>/eval_test.json`
sem WiSE).

## 14. Treino de produção, com todas as fotos (mesmo tempo do passo 8, ~1,4×)

```
python -m vision.split all
python -m vision.train --split data/splits/all.csv --run-name prod_<receita> <mesmos argumentos do passo 8, sem --split, --run-name e --eval-test-each-epoch>
python -m vision.wise --run runs/prod_<receita> --save 0.75
```

Agora entra tudo, inclusive o Craze inteiro. Não há validação nem teste: o número de épocas é o mesmo
da receita escolhida e o resultado medido é o dos passos 12 e 13.

## 15. Exportar para models/0.2.0 (uns 5 min)

```
python -m vision.export --run runs/prod_<receita>_wise0.75 --version 0.2.0 --split data/splits/grouped.csv --n-check 0 --temperature T --test-eval runs/XX_grouped_<receita>_wise0.75/eval_test.json --ood-eval runs/VENC/eval_lodo_craze_halves_final.json --variants
```

Grava em `models/0.2.0/`:
- `model.onnx` (fp32), `model.fp16.onnx` e `model.int8.onnx` (quantização dinâmica);
- `labels.json`, com o pré-processamento e a temperatura (aplicada dentro do grafo; o serviço só faz softmax);
- `metrics.json`, com `test` (passo 13), `out_of_domain` (passo 12, no mesmo formato do 0.1.0), a
  conferência ONNX × PyTorch, e para cada arquivo o tamanho, o tempo de CPU (1 imagem e lote de 64)
  e a concordância de classe e a diferença de probabilidade do fp16 e do int8 contra o fp32;
- `train_config.json`.

Deu certo quando imprime `"argmax_agreement": 1.0` na conferência. Se a diferença passar de 1e-3,
o script para com `ONNX diverge do PyTorch`. Nesse caso, avise o grupo.

Os dois candidatos usam 224 px e a normalização do ImageNet, como o 0.1.0, então o serviço não
precisa mudar o pré-processamento. Tamanho do `model.onnx`: DINOv2-S ~87 MB (fp16 ~44, int8 ~22); Pl@ntNet ViT-B
~345 MB. Tempo medido num i5-13450HX: DINOv2-S ~27 ms por foto e ~1,9 s no lote de 64 (o 0.1.0:
4 ms e 0,3 s).

## 16. Entregar

O `models/0.2.0/` vai para o **Git LFS**. Quem entrega:

```
git lfs install
```

Crie na raiz do repositório um `.gitattributes` com:

```
analytics/models/**/*.onnx filter=lfs diff=lfs merge=lfs -text
analytics/models/**/*.onnx.data filter=lfs diff=lfs merge=lfs -text
```

Depois:

```
git add .gitattributes analytics/models/0.2.0 analytics/resultados.md
git status
git commit -m "Modelo 0.2.0"
git push
```

Confira no `git status` que o `models/0.1.0/model.onnx` **não** aparece como modificado. Ele já está
no git sem LFS. Se aparecer, decida com o grupo entre convertê-lo também (`git add --renormalize`) ou
trocar o padrão para `analytics/models/0.2.0/*.onnx`.

**Vai para o git:** código de `vision/`, `resultados.md`, `models/0.2.0/` (pelo LFS),
`data/splits/*.summary.json` e `data/splits/craze_dev_final.csv`.
**Não vai:** `data/raw`, `data/processed`, `runs/`, `*.pt`, `data/splits/*.csv` gerados, imagens.

---

## Erros comuns

| Erro | O que fazer |
|---|---|
| `torch.cuda.is_available()` dá `False` | Rode `nvidia-smi`. Se não existir, instale o driver da NVIDIA. Se existir, reinstale o torch com CUDA: `python -m pip install --force-reinstall torch==2.11.0+cu128 torchvision==0.26.0+cu128 --extra-index-url https://download.pytorch.org/whl/cu128` |
| `CUDA out of memory` | Feche outros programas que usam a GPU. Use `--batch-size 16 --accum 2` (mesmo lote efetivo). Se ainda faltar, acrescente `--grad-checkpointing`. |
| `The paging file is too small`, `WinError 1455` ou `MemoryError` (Windows) | Falta memória para os processos que leem imagens. Use `--workers 2`, feche programas e **nunca rode dois treinos ao mesmo tempo**. Se continuar, aumente o arquivo de paginação do Windows. |
| Kaggle pede login (status `manual` no log do craze) | Crie a chave em kaggle.com → Settings → API → Create New Token. Salve o `kaggle.json` em `%USERPROFILE%\.kaggle\` (Windows) ou `~/.kaggle/` (Linux) e rode `python -m vision.download craze`. |
| OSF devolve erro 429 (CD&S) | É limite de taxa. O script já espera e tenta de novo. Se falhar, rode `python -m vision.download cds` mais tarde. |
| Disco cheio | Apague `data/raw/seasonal/_zip` (4,6 GB) depois de extrair. Os pesos baixados ficam em `~/.cache/huggingface`. Cada execução em `runs/` ocupa 0,1–0,7 GB: apague as que não estão na tabela. |
| Download do Hugging Face lento ou com erro 429 | Tente de novo. Com conta no HF, defina `HF_TOKEN` (PowerShell: `$env:HF_TOKEN="..."`; Bash: `export HF_TOKEN=...`). |
| `N imagens fora de craze_dev_final.csv` | O seu manifesto difere do grupo (passo 6). Não continue e avise. |
| `--select val precisa de validacao` | Você usou `all.csv` sem `--select last`. |
| `a temperatura se ajusta no craze_dev` | Você tentou `--fit-temperature` com `--part final`. Isso é proibido; use o T do passo 11. |

## O que já foi decidido (e por quê)

- **Divisão do Craze.** É feita por planta, a mesma unidade do grupo do manifesto, estratificada por
  classe. As duas metades têm 534/391/113 e 534/391/112 (cercosporiose/outras/saudável).
- **Dados extras avaliados e rejeitados**:
  - `figmaize` (figshare 29298275): as 1.592 imagens de milho são cópia exata do PlantVillage, mesmo md5.
  - `bdmaize` (Mendeley sb4wwjs8bz): folha destacada sobre banner, fundo uniforme, não é campo. Tem só
    182 fotos distintas em 664, e o "Gray Leaf Spot" tem só 12 fotos, várias com cara de minador.
  - `plantdoc`: imagens da web. Cerca de 40% do GLS é duvidoso, e a pasta de leaf blight tem fotos de
    GLS, o que troca o rótulo justamente da classe-alvo.
  - NLB da Cornell (Wiesner-Hanks 2018): não baixado. São 10 GB, e é uma classe só de um local, o que
    ensinaria um atalho ("foto de Aurora = outras manchas").
  - Nenhum serve para treino com rótulo confiável. O PlantDoc curado à mão poderia virar um teste
    extra fora do domínio.
- **Épocas.** Fora do domínio o F1 cai quando se treina mais, mesmo com a validação subindo. Por
  isso as receitas usam número fixo de épocas (`--select last`), e não a parada pela validação.
- **BioCLIP 2** (ViT-L, `--arch bioclip2`). Foi o melhor congelado (dev F1 0,538, e o melhor nos dois
  folds auxiliares). O fine-tune parcial (`b2_lpft6`, 12 de 24 blocos congelados, ~10 min por época
  na 3050) foi interrompido na época 4 de 8, com dev F1 0,58. Ficou fora do roteiro do grupo porque
  pesa ~1,2 GB e é lento na CPU do serviço. A receita está no histórico de `resultados.md`, caso
  alguém com mais GPU queira retomar.

## Arquivos do pacote

| Módulo | Para quê |
|---|---|
| `vision.split` | divisões (`grouped`, `lodo`, `--halves`, `--exclude`, `all`) |
| `vision.probe` | rodada congelada: `extract` (features em `runs/features`) e `fit` (logística) |
| `vision.train` | treino (LP-FT, layer decay, EMA, aumento forte, mixup/cutmix, `--freeze-blocks`) |
| `vision.wise` | WiSE-FT: interpola o fim do LP (`lp.pt`) com o modelo ajustado |
| `vision.evaluate` | avalia um checkpoint numa parte (`--tta`, `--temperature`, `--fit-temperature`) |
| `vision.report` | linha da tabela de `resultados.md` |
| `vision.export` | ONNX, conferência, fp16/int8, tempo de CPU, `metrics.json` |
| `vision.model` | backbones disponíveis (`BACKBONES`): `efficientnet_b0`, `dinov2_s`, `dinov2_b_reg4`, `plantclef_b`, `bioclip`, `bioclip2` |

## Glossário

- **craze_dev**: metade das fotos do Craze (África do Sul). Nenhum treino usa essas fotos. É com elas
  que se compara e escolhe. Na divisão, é a parte `test`.
- **craze_final**: a outra metade. Também fica fora de todo treino, e ninguém olha antes do fim. É
  avaliada uma vez, com a receita escolhida, e é o número que vai para o relatório. Na divisão, é a
  parte `final`.
- **F1 macro**: nota de 0 a 1 que mistura acertos e erros de cada classe e tira a média das três
  classes. Uma classe rara (saudável) pesa o mesmo que uma comum.
- **Recall de cercosporiose**: de todas as folhas que tinham cercosporiose, quantas o modelo achou.
  É o erro caro: deixar passar a doença.
- **Precisão de cercosporiose**: de todas as vezes que o modelo disse "cercosporiose", quantas
  estavam certas.
- **ECE**: o quanto a confiança do modelo mente. Se ele diz 90% e acerta 60%, o ECE é alto. Menor é
  melhor.
- **Temperatura**: um número que divide a saída do modelo para a confiança ficar honesta. Não muda a
  classe escolhida.
- **LP-FT**: primeiro treina só a última camada com o resto congelado (LP, *linear probe*), depois
  libera tudo com passo pequeno (FT, *fine-tuning*). Estraga menos o que o modelo já sabia, e isso
  ajuda fora do domínio.
- **WiSE**: média dos pesos entre o modelo do fim do LP e o modelo ajustado. Alfa 0 é só o LP, alfa 1
  é só o ajustado. Costuma generalizar melhor que as duas pontas.
- **TTA**: avaliar a mesma foto também espelhada e tirar a média. Custa 2× mais na hora de usar.
- **Semente**: número que fixa o sorteio do treino. Sementes diferentes dão resultados um pouco
  diferentes, e a média de 3 é mais confiável que uma só.
- **LFS** (Git Large File Storage): guarda arquivos grandes (os `.onnx`) fora do histórico normal do
  git. O repositório só leva um ponteiro para eles.
