# Pesquisa — cercosporiose no milho

Fundamentação das regras das fases 4 a 9 do [roadmap](roadmap-cercosporiose.md). Responde às quatro
pendências de pesquisa que o roadmap deixou abertas e levanta os conjuntos de imagens da trilha B.
Cada número tem fonte; o que **não** foi confirmado está marcado, em vez de preenchido com um valor
plausível.

Pesquisa feita em 2026-11-02. Onde a fonte foi lida só pelo resumo ou por trecho de busca (texto
completo bloqueado), isso está dito.

## 1. Amostragem para detecção

A regra que o catálogo já tinha (folha da espiga, nota 1–9, a cada 7 dias a partir de 60 DAE) é de
**avaliação de severidade em ensaio**. Para detectar o início numa lavoura comercial, a pesquisa
encontrou:

**Não existe protocolo brasileiro com número de pontos e de plantas para doença foliar de milho.**
Embrapa, IDR-Paraná, Fundação MT/ABC e Epagri não dão esse número. As regras de "5 pontos × 20
plantas" que circulam são de **lagarta-do-cartucho**, não de doença.

| Item | O que as fontes dizem | Confiança |
|---|---|---|
| Início | "Ainda na fase vegetativa", com ênfase no pré-pendoamento (Embrapa). EUA: de V6 em lavoura de risco (SDSU) a V10–V14 (Ohio State, Purdue), ou 2–3 semanas antes do pendoamento (K-State) | Média para "antes do VT"; **baixa** para o estádio V exato |
| Frequência | Semanal (Iowa State); 1–2×/semana depois da primeira lesão (K-State); a cada 4–5 dias com tempo úmido (SDSU) | Alta / média |
| Amostra | **10 paradas × 10 plantas**, em zigue-zague, a ≥10 passos da borda, cobrindo baixada, encosta e topo (SDSU). Uma fonte só, dos EUA, genérica para doença de milho | Baixa–média |
| Folhas | A infecção começa nas folhas baixeiras, perto da palhada (Embrapa Circ. 24). Folha abaixo da espiga como referência de inspeção (Embrapa); folhas baixeiras até a da espiga, inclusive (Iowa State) | Alta para "começar nas baixeiras"; o número de folhas por planta **não tem fonte** |
| Presença | Nenhuma fonte define "presença" para detecção. Incidência = % de plantas com sintoma (SDSU) | Critério de projeto |
| Intervalo | Período latente de **14 dias** (híbrido suscetível) a **21 dias** (moderadamente resistente) (Embrapa Circ. 24) | Alta |

**Onde as fontes divergem:** a extensão dos EUA vê os primeiros sintomas 2–3 semanas antes do
pendoamento; a Epagri, "na fase de floração" (VT a R4).

**Sintoma inicial — o que fotografar.** Lesões puntiformes, cloróticas, com **halo amarelo visível
contra a luz** (Purdue, Virginia Tech, K-State, Embrapa Circ. 24). Em 2–3 semanas viram o retângulo
limitado pelas nervuras, de 1–6 cm × 2–4 mm, que passa de marrom a cinza ao esporular. **A lesão
jovem não é diagnóstica**: confunde com antracnose, olho-de-pavão, ferrugem comum e, sobretudo,
mancha-de-bipolaris raça 0, que também é limitada pelas nervuras. Daí a foto pedida pelo protocolo:
folha inteira e close da lesão contra o sol.

**Limiares de ação** (só contexto; todos são de decisão de fungicida, em híbridos temperados, e
nenhum foi feito para detecção): 50% das plantas com lesão na 3ª folha abaixo da espiga ou acima,
antes do pendoamento (Iowa State, via Purdue); ≤1% de área foliar afetada para iniciar o fungicida
(Embrapa, em entrevista). O limiar "10% das plantas com lesão abaixo da espiga" que aparece em blogs
brasileiros **não foi rastreado até fonte primária**.

**Lacunas:** pontos por hectare; estádio ou DAE do primeiro sintoma no Brasil (safra × safrinha);
protocolo de vistoria sul-africano.

## 2. Índice de risco climático

**Não existe modelo, limiar validado ou índice calibrado para o Brasil.** O sistema calcula um
**índice de favorabilidade climática não calibrado** — nunca uma probabilidade.

**Paul & Munkvold.** O modelo de 2005 (Iowa, 329 casos, R² 0,70–0,75) usa horas diárias com
temperatura entre 22 e 30 °C e horas noturnas com UR ≥ 90%, ambas acumuladas de V4 a V12, mais
temperatura média noturna de V12 a R2, palhada, data de semeadura, resistência do híbrido e
longitude. O de 2004 (pré-plantio) aponta palhada, data de semeadura e resistência como os
preditores mais fortes. **Coeficientes, cortes de classe e a hora que define "noite" não foram
confirmados**: o texto completo da APS está bloqueado. As faixas citadas nos resumos descrevem os
dados de Iowa, não são limiares.

**Brasil.** Embrapa: "temperaturas entre 25 °C e 30 °C e umidade relativa do ar superior a 90% são
consideradas condições ótimas". A Circular 24 (Casela & Ferreira, 2003) é baseada em literatura dos
EUA: germinação em 24 h a 22–30 °C, crescimento do tubo germinativo interrompido com UR < 95%. O
mais perto de um modelo brasileiro é uma dissertação da UNESP (Lima, 2025; safrinha em SP, XGBoost),
sem revisão por pares e sem estádio da planta. Mesquini et al. (2020) apontam a chuva acumulada como
fator determinante das epidemias.

**Cuidado:** o limiar "16–23 °C acima de 800 m" que aparece em sites comerciais é, no texto da
Embrapa, da **ferrugem-comum**. E o "UR ≥ 90%, 17–27 °C" do artigo da Corteva (Agronomy, 2025) é do
modelo de helmintosporiose; o de cercosporiose ali é uma árvore de decisão sem os cortes publicados.

**Números que o sistema fixa, com fonte:**

| Parâmetro | Valor | Fonte |
|---|---|---|
| Faixa de temperatura favorável | 22–30 °C | Bhatia & Munkvold 2002; Paul & Munkvold 2005; Casela & Ferreira 2003 |
| UR das horas noturnas | ≥ 90% | Paul & Munkvold 2005 |
| Janela de acumulação | V4 a V12 | Paul & Munkvold 2005 |
| Período latente | 14–21 dias | Casela & Ferreira 2003 |

**Números que o sistema não fixa como literatura:** cortes entre os níveis do índice, pesos dos
fatores do cultivo, altitude e a hora da "noite". Os cortes entre níveis e a janela noturna são
**decisões de projeto**, registradas como tal na fase 5.

**Fatores do cultivo.** Não há multiplicador publicado. A extensão dos EUA ordena o risco: milho
contínuo com doença no ano anterior e palhada na superfície é o máximo; rotação de 2 anos ou mais é o
mínimo (escala de 5 níveis da Purdue, BP-56). Com ≥ 30% de palhada de lavoura que teve a doença, o
dano é provável se o clima ajudar (UNL G1902). No Brasil, a resistência do híbrido foi medida como
dano: 5–9% de perda em resistentes contra 16–27% em suscetíveis (Brito et al., 2007). Por isso os
fatores entram como **modificadores ordinais**, documentados como heurística.

**Ressalva do dado:** a UR do Open-Meteo é do ar a 2 m, não do dossel.

## 3. Espécie predominante

***C. zeina* predomina no Brasil.** Brunelli et al. (2008, coletas de 2001–2002) acharam as duas
espécies em quase todos os estados; Neves et al. (2015) acharam *C. zeina* em 68 de 75 isolados em
Goiás e 73 de 75 no Paraná (lido pelo resumo). Os textos da Embrapa ainda citam só *C. zeae-maydis*,
e os limiares climáticos acima foram medidos com *C. zeae-maydis* — não há estudo de temperatura ×
molhamento com *C. zeina*.

**Os sintomas não separam as espécies.** "Both species cause similar symptoms on maize"; separar
exige cultura, microscopia ou PCR, e as duas já foram achadas na mesma lesão (Hsieh, 2011). A
diferença na produção de cercosporina (0 de 28 isolados de *C. zeina*) é medida em meio de cultura e
não aparece na folha. **Consequência:** a classe do modelo de visão é *cercosporiose
(Cercospora spp.)*, sem tentar separar espécie.

## 4. Satélite

**Nenhum dos dois estudos citados no roadmap usou imagem de satélite real.** Os dois mediram
espectros de **folhas individuais** com espectrorradiômetro de campo (África do Sul) e os
reamostraram para as bandas do Sentinel-2:

- Dhau et al. (Geocarto, 2018): Random Forest, OA 84% com bandas do Sentinel-2; a banda isolada mais
  importante foi a B5 (705 nm, red-edge). As classes começam em **21% de área foliar lesionada** —
  folhas abaixo disso não viraram classe.
- Sibanda et al. (Agronomy, 2019): "spectral configurations simulated using proximally sensed data";
  OA 0,95 com a configuração do Sentinel-2.

**Não existe trabalho com Sentinel-2 real para cercosporiose**, nem número de severidade mínima
detectável a 10–20 m. Com imagem real há estudos de vírus da risca (MSV) e de lagarta-do-cartucho.
Inferência: se a evidência em folha começa em 21% de lesão, no pixel de 10–20 m isso se mistura com
folha sadia e solo. **O satélite aponta onde o talhão destoa; não detecta o início.** Isso confirma o
papel da camada 2 no roadmap.

**Decisões técnicas da fase 6:**

- **Catálogo:** Element 84 Earth Search, `https://earth-search.aws.element84.com/v1`, coleção
  `sentinel-2-c1-l2a`, em COG, sem conta. A coleção legada `sentinel-2-l2a` tem metadado de offset
  inconsistente. Limite de requisições não documentado.
- **Refletância** = DN × 0,0001 − 0,1. O offset vem nos metadados (`raster:bands`) e tem de ser
  aplicado antes do índice.
- **Máscara SCL:** 0 (sem dado), 1 (saturado), 3 (sombra de nuvem), 8 e 9 (nuvem média e alta),
  10 (cirrus). Mascarar além disso é convenção, não norma. A SCL tem 20 m.
- **NDVI** = (B08 − B04) / (B08 + B04), a 10 m.
- **NDRE** = (B8A − B05) / (B8A + B05), as duas bandas a 20 m, o que evita misturar resoluções. Não
  há convenção única de NDRE na literatura de milho; a variante com B08 está no catálogo Awesome
  Spectral Indices. A escolha fica registrada.
- **Nuvem:** no centro do Mato Grosso a frequência de nuvens costuma passar de 70% entre dezembro e
  fevereiro (Prudente et al., 2020, citado por Bolfe et al.); Bolfe et al. (2023) mediram cobertura
  média de 57,5% de setembro a março em Sorriso-MT. Estimativa, a conferir com a SCL num talhão real:
  sobram 3 a 5 passagens úteis em dezembro–fevereiro.
- Desde março de 2025 o Sentinel-2A, em campanha estendida até o fim de 2027, aumenta a revisita
  sobre a América do Sul. A revisita efetiva no Brasil Central não foi confirmada; dá para medir
  contando os itens do STAC.

## 5. Conjuntos de imagens (trilha B)

| Conjunto | Licença | Cercosporiose | Outras classes | Campo/lab | País | Divisão sem vazamento |
|---|---|---|---|---|---|---|
| [Maize In-Field (Craze et al., 2022)](https://www.kaggle.com/datasets/hamishcrazeai/maize-in-field-dataset) | CC BY-NC-SA 4.0 | 1.084 (771 só GLS) | Helmintosporiose 554, mancha-branca 493, ferrugem comum 300, polissora 39, sem sintoma 285; multirrótulo | Campo | África do Sul | Parcial: câmera e pares de mesma planta pelo nome do arquivo |
| [CD&S (Ahmad et al., 2021)](https://osf.io/s6ru5/) | **Não declarada** no OSF | 524 | Helmintosporiose 511, northern leaf spot 562; sem saudável | Campo | EUA | Não: um local, um mês |
| [Seasonal Corn Leaf Disease](https://data.mendeley.com/datasets/vy629dngm8/1) | CC BY 4.0 | 1.497 | Saudável 1.038, ferrugem comum 129 (descartar as aumentadas) | Campo | Bangladesh | Diz ter local; não verificado |
| [PlantVillage](https://github.com/spMohanty/PlantVillage-Dataset) | Ambígua (CC BY-SA 3.0 ou CC0) | 513 | Ferrugem comum 1.192, helmintosporiose 985, saudável 1.162 | Lab, fundo uniforme | EUA | Só por folha (`leaf_id`) |
| [PlantDoc](https://github.com/pratikkayal/PlantDoc-Dataset) | CC BY 4.0 (imagens raspadas da web) | 68 | Leaf blight 192, ferrugem 116 | Web | — | Não |
| [Digipathos (Embrapa)](https://doi.org/10.48432/XA1OVL) | CC BY-NC 4.0 | **0** | Mancha-branca, helmintosporiose, polissora, ferrugem-branca, bipolaris; sem saudável | Lab, placa com *color checker* | Brasil | Só pela imagem original |

**Não há conjunto aberto brasileiro de campo com cercosporiose de milho.** O Craze é o único que
cobre as três classes em campo; é o eixo do treino e o conjunto de teste separado. CD&S e Seasonal
completam a cercosporiose; Digipathos só serve para *outras manchas*, com viés de laboratório. Evitar
o Kaggle "Corn or Maize Leaf Disease" (PlantVillage + PlantDoc reempacotados).

**Linha de base:** EfficientNet-B0 ou MobileNetV3-Large pré-treinadas no ImageNet, fine-tuning
completo com aumento de dados no treino, exportação ONNX; ConvNeXt-Tiny como teto de comparação.
Métricas: **recall de cercosporiose** e F1 macro, não acurácia. Divisão agrupada por planta, câmera
ou sessão, e um teste *leave-one-dataset-out*.

**O que esperar fora do domínio:**

- VGG16 treinada no PlantVillage: 94,1% no próprio PlantVillage e **55,1% em campo**, com recall de
  cercosporiose de **5,2%**. Treinada em campo: 73,4% (Craze et al., 2022).
- ~99% no PlantVillage e 31,4% em imagens da web (Mohanty et al., 2016).

Os ~98–99% publicados vêm em geral de divisão aleatória ou de aumento de dados feito antes de
dividir, e um classificador acerta 49% do PlantVillage olhando só 8 pixels do fundo (Noyan, 2022).
**Não servem de meta.**

**Não confirmado:** licença definitiva do PlantVillage e dos dados do CD&S; metadado de local do
Craze e do Seasonal.

## Fontes

Amostragem e sintomas:
- [Embrapa — Cultivo do Milho: doenças (PDF)](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/748764/1/Milho-Doencas.pdf)
- [Embrapa — A Cercosporiose na Cultura do Milho, Circular 24 (PDF)](https://www.infoteca.cnptia.embrapa.br/bitstream/doc/487543/1/Circ24.pdf)
- [Embrapa — Circular 83, doenças foliares (PDF)](https://www.infoteca.cnptia.embrapa.br/bitstream/doc/490415/1/Circ83.pdf)
- [Epagri — BT 170, pragas e doenças do milho (PDF)](https://ciram.epagri.sc.gov.br/ciram_arquivos/agroconnect/boletins/BT_PragasDoencasMilho.pdf)
- [South Dakota State — iGrow Corn, cap. 45 (PDF)](https://extension.sdstate.edu/sites/default/files/2019-09/S-0003-45-Corn.pdf)
- [Iowa State — Applying fungicides to corn](https://crops.extension.iastate.edu/encyclopedia/applying-fungicides-corn-stop-look-consider)
- [K-State — Gray Leaf Spot of Corn, MF2341](https://bookstore.ksre.ksu.edu/download/gray-leaf-spot-of-corn_MF2341)
- [Ohio State — PLPATH-CER-05](https://ohioline.osu.edu/factsheet/plpath-cer-05)
- [Purdue — BP-56 (PDF)](https://www.extension.purdue.edu/extmedia/BP/BP-56.pdf)
- [Crop Protection Network — Gray leaf spot](https://cropprotectionnetwork.org/encyclopedia/gray-leaf-spot-of-corn)
- [UNL — G1902](https://extensionpubs.unl.edu/publication/g1902/na/html/view)
- [Virginia Tech — 450-612 (PDF)](https://vtechworks.lib.vt.edu/bitstream/handle/10919/56781/450-612.pdf?sequence=1)

Índice de risco e espécie:
- [Paul & Munkvold 2004 — Preplanting risk assessment](https://doi.org/10.1094/PHYTO.2004.94.12.1350)
- [Paul & Munkvold 2005 — Regression and ANN modeling](https://doi.org/10.1094/PHYTO-95-0388)
- [Bhatia & Munkvold 2002](https://pubmed.ncbi.nlm.nih.gov/30818507/)
- [Peddicord et al. 2025 — Agronomy 15:328](https://doi.org/10.3390/agronomy15020328)
- [Fantin et al. 2008 (PDF)](https://ainfo.cnptia.embrapa.br/digital/bitstream/item/101780/1/Efeito-mancha.pdf)
- [Lima 2025 — dissertação UNESP](https://repositorio.unesp.br/entities/publication/147e6b18-22c1-4a43-916b-5b32c216cbaa)
- [Mesquini et al. 2020 — Summa Phytopathologica](https://www.scielo.br/j/sp/a/qN84XjyVsvfWgNtzVxH55JR/abstract/?lang=pt)
- [Brito et al. 2007 — Fitopatologia Brasileira](https://www.scielo.br/j/fb/a/qcyW6JwB9LdFWqvw7jGjBMj/?format=html&lang=pt)
- [Brunelli et al. 2008 — Genetics and Molecular Biology](http://www.scielo.br/j/gmb/a/s9SYRsq5SjBJGfCsMR5pTkJ/?lang=en)
- [Neves et al. 2015 — Tropical Plant Pathology](https://link.springer.com/article/10.1007/s40858-015-0053-5)
- [Crous et al. 2006 — Cercospora zeina](https://pmc.ncbi.nlm.nih.gov/articles/PMC2104713/)

Satélite:
- [Dhau et al. 2018 — Geocarto](https://www.tandfonline.com/doi/full/10.1080/10106049.2017.1343391) e [tese de Dhau](https://ulspace.ul.ac.za/items/75d96e80-09c6-44cd-947d-76fd50763d80)
- [Sibanda et al. 2019 — Agronomy 9:846](https://doi.org/10.3390/agronomy9120846)
- [Scientific Reports 16:22431 (2026) — UAV multiespectral](https://pmc.ncbi.nlm.nih.gov/articles/PMC13376745/)
- [Earth Search — coleção sentinel-2-c1-l2a](https://earth-search.aws.element84.com/v1/collections/sentinel-2-c1-l2a)
- [Sentinel Hub — Sentinel-2 L2A e SCL](https://docs.sentinel-hub.com/api/latest/data/sentinel-2-l2a/)
- [Awesome Spectral Indices](https://github.com/awesome-spectral-indices/awesome-spectral-indices)
- [Bolfe et al. 2023 — Embrapa (PDF)](https://www.alice.cnptia.embrapa.br/alice/bitstream/doc/1155214/1/AP-Mapping-agricultural-intensification-2023.pdf)
- [CDSE — extensão da campanha do Sentinel-2A](https://dataspace.copernicus.eu/news/2026-10-9-sentinel-2a-extension-campaign-continue-until-end-2027)

Imagens:
- [Craze et al. 2022 — Deep learning diagnostics of GLS under mixed disease field conditions](https://pmc.ncbi.nlm.nih.gov/articles/PMC9330607/)
- [Mohanty et al. 2016](https://arxiv.org/abs/1604.03169)
- [Noyan 2022 — viés de captura no PlantVillage](https://arxiv.org/abs/2206.04374)
- [CD&S — arXiv 2110.12084](https://arxiv.org/abs/2110.12084)
