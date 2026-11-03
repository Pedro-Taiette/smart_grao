# Roadmap — detecção precoce de cercosporiose no milho

> **Plano vigente desde 2026-11-02.** Substitui as fases 4 a 7 do `roadmap.txt` para o semestre. As
> fases 1 a 3 e a reorganização da navegação continuam valendo e já estão em `main`.
>
> **Entrega em 25/11/2026** (margem até 30/11), **sem lavoura disponível** na safra. Cada fase tem um
> corte para a entrega; o resto fica para depois dela. Os números agronômicos vêm de
> [Pesquisa — cercosporiose](pesquisa-cercosporiose.md).

## O recorte

Neste semestre o SmartGrão acompanha uma cultura, o **milho**, e um alvo de ponta a ponta, a
**cercosporiose** (*Cercospora zeina* / *C. zeae-maydis*). O resto do catálogo de alvos e a soja
continuam no modelo, porque estreitar por configuração custa um seed e uma tela; estreitar apagando
custaria refazer o modelo genérico quando o escopo voltar a crescer.

## A ideia central: o sistema decide quando e onde olhar

Um produto em que o produtor fotografa uma folha e a IA responde depende de alguém já desconfiar da
doença e acertar a folha. Para detecção **precoce** isso é pouco: a cercosporiose começa nas folhas
de baixo, sob o dossel, antes de qualquer coisa ser visível de longe.

Também não existe hoje detecção totalmente autônoma e confiável do *início* da doença — nada que
olhe de cima enxerga o terço inferior. Por isso a automação fica onde ela funciona, e a imagem fica
onde ela é insubstituível:

| Camada | Pergunta | Autônoma? | Fonte |
|---|---|---|---|
| 1 — Risco climático | **Quando** olhar | Sim | Clima horário (Open-Meteo) + estádio e manejo do cultivo |
| 2 — Anomalia por satélite | **Onde** olhar | Sim | Sentinel-2, a cada ~5 dias, recortado pelo contorno do talhão |
| 3 — Confirmação por imagem | **O que** é | Não — dirigida pelo sistema | Foto da folha + modelo de visão |

A camada 3 deixa de ser o ponto de partida: o sistema agenda a vistoria, gera os pontos dentro da
zona suspeita e diz quais folhas fotografar. A foto confirma; quem conduziu foi o sistema.

> *O SmartGrão não espera o produtor achar a doença: avisa quando o clima favorece, mostra onde o
> talhão destoa e manda conferir aquele ponto.*

## Arquitetura

```
           ┌────────────────────── backend .NET ──────────────────────┐
Open-Meteo ─► coleta de clima ─► índice de risco ─┐                     │
           │                                     ├─► alerta ─► vistoria sugerida
           │  fila de análises (Postgres) ◄──────┘        (malha na zona) │
           └──────────┬───────────────────────────▲───────────────────┘
                      │ HTTP                      │ resultados
           ┌──────────▼──── serviço Python (analytics/) ─────────────────┐
Sentinel-2 ─► satellite: índices, nuvem, zonas anômalas                  │
   fotos   ─► vision: classificação da folha, mapa de calor (Grad-CAM)   │
           └─────────────────────────────────────────────────────────────┘
```

- **O domínio continua no .NET.** Fila, retentativas, alertas, versões de modelo e o histórico vivem
  no backend atual. O serviço Python é sem estado: recebe uma tarefa, devolve um resultado.
- **Clima fica no .NET**: é JSON simples, e o índice é regra de domínio.
- **Satélite e visão ficam no Python**: raster (`rasterio`, `numpy`) e redes neurais (ONNX) não têm
  equivalente maduro em .NET.
- **Fila no Postgres** (`FOR UPDATE SKIP LOCKED`) em vez de um broker: o volume do protótipo não
  justifica mais uma peça de infraestrutura, e a fila participa das transações do domínio.
- **Toda imagem tem origem** — celular, estação sentinela ou drone. O serviço de visão não distingue
  quem tirou a foto, o que deixa a porta aberta para as fontes autônomas do fim do documento.

## Duas trilhas em paralelo

O modelo de visão é o caminho crítico: coletar dados, treinar e avaliar leva semanas e não depende
das telas. Ele começa junto com a fase 4.

- **Trilha A — produto** (.NET + React): fases 4 a 9 em sequência.
- **Trilha B — modelo** (Python): curadoria de dados e treino desde já; entra no produto na fase 8.

## Situação

Em 2026-11-02, as fases 4 a 9 e a estação sentinela simulada estão implementadas, cada uma com o seu
documento: [4](fase-4-recorte.md), [5](fase-5-risco-climatico.md), [6](fase-6-satelite.md),
[7](fase-7-vistoria-dirigida.md), [8](fase-8-ia-imagem.md), [9](fase-9-aferimento.md). As telas foram
simplificadas para milho e cercosporiose: catálogo, editor de protocolos e malha genérica saíram da
navegação (o código e o banco ficam). O [roteiro da demonstração](roteiro-demonstracao.md) junta tudo
na ordem da defesa. O calendário abaixo fica como registro do plano.

## Calendário até a entrega

A ordem das fases se mantém. As duas trilhas correm juntas: a B tem de entregar o modelo em ONNX até
15/11, para a fase 8 ter o que servir.

| Semana | Trilha A — produto | Trilha B — modelo |
|---|---|---|
| 03–08/11 | Fase 4 (03–04) e fase 5 (05–08) | Baixar os conjuntos, montar as divisões, primeira linha de base |
| 09–15/11 | Fase 6 | Treino e avaliação; ONNX congelado em 15/11 |
| 16–22/11 | Fase 7 (16–18) e fase 8 (19–22) — 20/11 é feriado | Ajustes e Grad-CAM |
| 23–25/11 | Fase 9 (23–24); 25/11 congela o código e ensaia a defesa | — |
| 26–30/11 | Margem. Se a entrega for em 30/11, entra a estação sentinela simulada | — |

**Sem lavoura, a validação é histórica.** Open-Meteo e Sentinel-2 têm arquivo: as camadas 1 e 2 rodam
com dados reais de safras passadas num talhão real, e é isso que a defesa mostra. O modelo de imagem
é medido num conjunto de teste público separado por fonte. Nada disso é validação agronômica, e as
telas dizem isso.

**Ordem de corte, se o calendário apertar:** Grad-CAM → comparação com a passagem anterior na zona
anômala → exportação da fase 9 → recorte por estádio no painel. O recálculo das safras passadas
**não** é cortado: sem lavoura, é ele que demonstra as camadas 1 e 2.

---

## Fase 4 — Recorte milho + cercosporiose

Preparar o que já existe para o novo foco, sem funcionalidade nova de análise.

- Milho como cultura padrão; o seletor de cultura some da tela. Soja e o catálogo amplo saem da
  navegação e continuam no banco.
- Protocolo **"Cercosporiose — detecção precoce"** semeado e publicado: presença e foto obrigatória
  da folha, com a posição da folha (baixeira, abaixo da espiga, folha da espiga). As instruções
  seguem a [pesquisa](pesquisa-cercosporiose.md#1-amostragem-para-detecção): começar pelas folhas
  baixeiras e subir até a da espiga, anotar a folha mais alta com lesão, fotografar a folha inteira
  e um close da lesão contra o sol (é o que mostra o halo amarelo da lesão jovem).
- **Presença** é "planta com ao menos uma lesão compatível", e conta como **suspeita** — a lesão
  jovem não é diagnóstica. É critério de projeto, sem fonte, e está documentado assim.
- Amostra de referência: **10 paradas × 10 plantas**, em zigue-zague, longe da borda. É a única regra
  numérica encontrada (extensão dos EUA, genérica para doença de milho); não há protocolo brasileiro.
- Fatores de risco no cultivo: suscetibilidade do híbrido à cercosporiose, cultura anterior
  (milho sobre milho) e sistema de plantio (direto com palhada ou convencional). Entram como níveis
  ordinais, porque não há peso publicado para eles.

**Critério:** um cultivo de milho é cadastrado com os fatores de risco e recebe uma vistoria de
cercosporiose sem que nenhuma tela mencione soja.

**Trilha B no mesmo período:** reunir CD&S, PlantVillage, Digipathos e PlantDoc; montar a classe
*outras manchas* (helmintosporiose, mancha-branca, ferrugens); definir a divisão treino/teste **por
fonte e local**, nunca aleatória.

## Fase 5 — Risco climático (camada 1)

O sistema passa a saber, todo dia e sem ninguém pedir, quais talhões estão em condição favorável à
doença.

- Coleta diária de temperatura e umidade horárias do Open-Meteo no centroide de cada talhão com
  cultivo de milho ativo: histórico recente e previsão de alguns dias.
- **Índice de favorabilidade climática, não calibrado** — nunca "probabilidade". Usa as variáveis
  de Paul & Munkvold: horas com temperatura entre 22 e 30 °C e horas noturnas com umidade relativa
  ≥ 90%, acumuladas na janela de V4 a V12. Faixa, umidade e janela têm fonte; os **cortes entre os
  níveis** e a hora que define "noite" não têm, e ficam documentados como decisão de projeto. Os
  fatores do cultivo da fase 4 sobem ou descem um nível (heurística a partir da escala da Purdue).
- O período latente (14 a 21 dias) diz quando procurar sintoma depois de uma janela favorável — é
  ele que data a vistoria sugerida.
- O estádio vem dos registros da fase 1. Registro antigo demais faz a tela **pedir a atualização**,
  em vez de o sistema adivinhar o estádio.
- O risco sempre vem explicado: *"11 noites úmidas nos últimos 14 dias, híbrido suscetível, milho
  sobre milho"*. Um número sem motivo não convence o produtor.
- Risco alto gera alerta em "Hoje" e uma **vistoria sugerida**, que só é agendada com a confirmação
  do usuário, seguindo a regra de que automatizar não é decidir em silêncio.
- Recalcular safras passadas com o clima histórico, para mostrar a série de risco de um talhão real.
  **Sem lavoura no semestre, é esta a demonstração da camada 1.**

**Critério:** um talhão real exibe o risco diário com a explicação, e um dia de risco alto produz
uma vistoria sugerida.

## Fase 6 — Anomalia por satélite (camada 2)

O sistema passa a apontar onde, dentro do talhão, a lavoura destoa.

- Busca das cenas Sentinel-2 L2A que cobrem o talhão no Earth Search (Element 84), coleção
  `sentinel-2-c1-l2a`, em COG, sem conta e sem custo. Refletância = DN × 0,0001 − 0,1.
- Máscara de nuvem e sombra pela banda de classificação de cena (SCL: 0, 1, 3, 8, 9, 10). Passagem
  encoberta fica registrada como **"sem leitura"**, e não some — saber que o satélite não viu é
  informação. Em dezembro–fevereiro, espera-se que mais de 70% das passagens no Brasil Central venham
  contaminadas.
- NDVI (B08, B04, 10 m) e NDRE (B8A, B05, 20 m) por pixel dentro do contorno. Estatísticas por
  passagem e série temporal por talhão.
- Zona anômala: pixels que caem em relação à mediana do próprio talhão e à passagem anterior,
  agrupados em polígonos com área mínima. Gravados como `geography(Polygon)`, como o contorno.
  Nenhum estudo usou Sentinel-2 real para cercosporiose, e a evidência em folha começa em 21% de área
  lesionada: a zona aponta onde olhar, não detecta o início.
- Camada no mapa da página do talhão, com a data da passagem.
- Na tela, a zona é *"área que destoa do resto do talhão"* — nunca *"cercosporiose detectada"*.
  O satélite não sabe qual doença é, e nem se é doença.

**Critério:** um talhão real mostra a série de NDRE de uma safra passada e as zonas anômalas
desenhadas no mapa, com as passagens nubladas indicadas.

## Fase 7 — Fotos e vistoria dirigida (camada 3)

As duas camadas autônomas se juntam e levam alguém ao ponto certo.

- Foto ligada à parada e ao alvo, com posição da folha, estádio, GPS e origem. Armazenamento de
  objetos (MinIO no desenvolvimento, R2 ou equivalente no ambiente publicado), com validação de tipo
  e tamanho.
- **Vistoria dirigida:** risco alto + zona anômala geram a sugestão com a malha **dentro da zona**,
  reaproveitando a amostragem existente com o polígono da zona no lugar do talhão. Sem zona (nuvem,
  ou nada destoa), a malha cobre o talhão inteiro.
- Captura pelo navegador do celular, com identificador gerado no cliente, para o reenvio não
  duplicar. A fila local e o modo offline completo do `roadmap.txt` ficam para depois da entrega.

**Critério:** um alerta vira uma vistoria sugerida com pontos na zona; a pessoa fotografa e as fotos
aparecem no histórico da parada. Sem lavoura, a demonstração usa fotos do conjunto de teste.

## Fase 8 — IA de imagem

A trilha B entra no produto.

- Fila persistente de análises no Postgres, com retentativas, falha registrada e um **processador
  simulado** primeiro — a integração é comprovada antes de o modelo existir.
- Serviço Python com o modelo em ONNX. Saída: classe (*cercosporiose (Cercospora spp.)*,
  *saudável*, *outras manchas*), confiança, mapa de calor e versão do modelo. A classe não separa
  *C. zeina* de *C. zeae-maydis*: as duas causam sintoma semelhante e só se distinguem em
  laboratório.
- Resultado gravado como **avaliação** (`Assessment`), separada da contagem humana: o roadmap já
  distinguia evidência coletada de interpretação. Uma foto pode ter várias avaliações — de modelos
  diferentes, ou de um modelo e de uma pessoa.
- Na tela: *"suspeita de cercosporiose — confirme"*. "Não detectado" vale para as folhas
  fotografadas, não para a lavoura.
- Severidade estimada (segmentação das lesões) fica como segunda etapa, se a classificação estiver
  pronta a tempo.

**Critério:** uma foto percorre a fila e o resultado aparece com confiança e mapa de calor; as
métricas do modelo no conjunto de teste ficam registradas junto da versão.

**Trilha B em números.** Treino em torno do Maize In-Field (Craze et al., África do Sul), o único
conjunto de campo com as três classes; CD&S e Seasonal completam a cercosporiose; Digipathos só
entra em *outras manchas*. Linha de base EfficientNet-B0 ou MobileNetV3. Métricas: recall de
cercosporiose e F1 macro, com divisão agrupada e um teste *leave-one-dataset-out*. Referência do que
esperar: um modelo treinado em laboratório cai para 5% de recall de cercosporiose em campo; treinado
em campo, fica em torno de 73% de acurácia. Detalhes e licenças na
[pesquisa](pesquisa-cercosporiose.md#5-conjuntos-de-imagens-trilha-b).

## Fase 9 — Revisão humana e aferimento

O sistema mede o quanto ele próprio acerta.

- Revisor confirma ou corrige cada avaliação do modelo. A revisão é a verdade de campo do
  aferimento e, ao mesmo tempo, dado de treino para a próxima versão.
- Painel de aferimento:
  - **Antecedência** — quantos dias o alerta de clima e a zona do satélite chegaram antes da primeira
    confirmação na folha. É a métrica que define se a detecção foi *precoce*.
  - **Sensibilidade** do modelo de imagem, priorizada: em detecção precoce, o falso negativo é o erro
    caro.
  - Precisão, matriz de confusão, desempenho por estádio e calibração da confiança.
  - Taxa de alarmes falsos de cada camada.
- Exportação das fotos revisadas com os rótulos e a rastreabilidade (talhão, cultivo, visita).

**Critério:** o painel mostra os números calculados a partir de revisões. Sem lavoura acompanhada no
semestre, os números do modelo vêm do conjunto de teste e a **antecedência** fica sem valor, com o
motivo na tela — ela só existe com confirmação em campo. Corte da entrega: revisão, sensibilidade,
precisão e matriz de confusão; o resto fica para depois.

---

## Extras, se houver tempo e recurso

- **Estação sentinela simulada.** No hardware real, Raspberry Pi ou ESP32-CAM com sensor de
  temperatura, umidade e molhamento foliar, fotografando folhas fixas do terço inferior a cada
  poucas horas — a única detecção por imagem verdadeiramente autônoma deste plano. Para a entrega,
  sem hardware nem lavoura, um **simulador** faz o papel da estação: envia leituras de microclima e
  fotos do conjunto de teste pela mesma API, com origem *estação sentinela*. Como a foto já tem
  origem desde a fase 7, o simulador não exige nada novo do backend. Tudo que vem dele aparece
  marcado como **simulado** na tela.
- **Imagens de drone.** Aceitar o envio pela mesma fila, com origem *drone*. Voo, ortomosaico e
  regulamentação ficam fora.
- **Offline completo** da captura em campo.

## Fora do escopo

Outras culturas e alvos na IA, recomendação de aplicação de fungicida, satélite comercial de alta
resolução, autenticação (adiada para o fim, por decisão já tomada).

## Relação com o `roadmap.txt`

| `roadmap.txt` | Esta proposta |
|---|---|
| 4 — Imagens e sincronização | Fase 7, com offline completo rebaixado a extra |
| 5 — Integração com a IA | Fase 8 |
| 6 — Revisão e acompanhamento | Fase 9 |
| 7 — Dados para treinamento | Exportação dentro da fase 9 |
| — | **Novas:** fase 4 (recorte), 5 (clima) e 6 (satélite) |

## Riscos e ressalvas

- **O índice de risco não é previsão calibrada.** O modelo de Paul & Munkvold foi ajustado nos EUA;
  aqui ele é um índice até ser validado contra ocorrências brasileiras.
- **Nuvem.** A estação chuvosa, quando a doença aparece, é a mais encoberta. Várias passagens do
  Sentinel-2 serão perdidas — por isso a camada 2 não pode ser condição para a vistoria.
- **Resolução.** 10–20 m por pixel: o satélite vê a lavoura perdendo vigor, não lesões. Enxerga
  severidade moderada, não o início.
- **Desvio de domínio.** Os conjuntos públicos são de outros países (CD&S, EUA) ou de laboratório
  (PlantVillage). Acurácia neles não é acurácia no campo brasileiro.
- **Números publicados inflados.** Trabalhos com 98% de acurácia em geral misturam fotos da mesma
  planta no treino e no teste. Não servem de meta.
- **Calendário.** A entrega é em 25/11, antes de a doença aparecer (perto do florescimento,
  dezembro a fevereiro), e não há lavoura disponível. As camadas 1 e 2 rodam com dados reais de
  safras passadas e ficam **não validadas** agronomicamente; isso precisa estar dito na defesa.
- **Prazo.** Seis fases e um modelo em pouco mais de três semanas. A ordem de corte está no
  calendário; a fase 6 é a de maior risco técnico, porque estreia o serviço Python.

## Pesquisa

As quatro pendências foram pesquisadas em 2026-11-02 — resultado e fontes em
[Pesquisa — cercosporiose](pesquisa-cercosporiose.md):

- **Amostragem para detecção:** não há protocolo brasileiro com número de pontos e plantas; a regra
  de referência é da extensão dos EUA. Começar pelas folhas baixeiras, antes do pendoamento.
- **Índice de risco:** não há modelo calibrado para o Brasil. Faixa de 22–30 °C, UR ≥ 90% e janela
  V4–V12 têm fonte; cortes e pesos, não.
- **Espécie:** *C. zeina* predomina, e o sintoma não separa as duas espécies.
- **Sentinel-2:** os dois estudos usaram espectros de folha reamostrados, não imagem de satélite.

Continuam em aberto: pontos por hectare, estádio do primeiro sintoma no Brasil, coeficientes de Paul
& Munkvold (texto bloqueado) e a revisita efetiva do Sentinel-2 no Brasil Central.

## Fontes

- [Paul & Munkvold — Regression and ANN Modeling for the Prediction of Gray Leaf Spot of Maize](https://apsjournals.apsnet.org/doi/10.1094/PHYTO-95-0388)
- [Paul & Munkvold — Preplanting Risk Assessment for Gray Leaf Spot of Maize](https://apsjournals.apsnet.org/doi/10.1094/PHYTO.2004.94.12.1350)
- [Scalable Prediction of NCLB and GLS to Predict Fungicide Spray Timing (Agronomy, 2025)](https://doi.org/10.3390/agronomy15020328)
- [HyspIRI simulado vs Sentinel-2 na detecção de cercosporiose (Agronomy, 2019)](https://doi.org/10.3390/agronomy9120846)
- [UAV multispectral e aprendizado de máquina para doenças foliares do milho (Scientific Reports)](https://www.nature.com/articles/s41598-026-53092-4)
- [CD&S Dataset — imagens de campo de doenças do milho](https://arxiv.org/abs/2110.12084)
- [Deep Learning Diagnostics of Gray Leaf Spot under Mixed Disease Field Conditions](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC9330607/)
- [Digipathos — banco de imagens da Embrapa](https://portaldbo.com.br/banco-de-dados-oferece-imagens-com-informacoes-sobre-as-principais-doencas-agricolas/)
- [Embrapa — A Cercosporiose na Cultura do Milho, Circular 24 (PDF)](https://www.infoteca.cnptia.embrapa.br/bitstream/doc/487543/1/Circ24.pdf)
- [Embrapa — Doenças foliares do milho](https://www.embrapa.br/en/web/agencia-de-informacao-tecnologica/cultivos/milho/producao/pragas-e-doencas/doencas/doencas-foliares)
