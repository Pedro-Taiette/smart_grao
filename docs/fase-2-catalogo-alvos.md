# Fase 2 — Catálogo de alvos e protocolos de milho

Proposta de conteúdo agronômico para a fase 2, **antes** de escrever código. O objetivo é um
catálogo genérico: serve a qualquer lavoura de milho do Brasil, não é configurado por fazenda.
Alvos e protocolos são dados de referência do produto, versionados; a propriedade apenas escolhe
qual protocolo usar.

Este documento registra também o que **não** foi confirmado, para não virar número inventado.

> Os 18 alvos abaixo estão implementados e semeados no banco, todos em `ManualRecord`. O modelo, a
> migração e a API estão em [Fase 2 — protocolos](fase-2-protocolos.md).

## Por que o catálogo precisa de alvos nomeados

A estrutura é genérica, mas uma vistoria só tem significado com o alvo definido: é o alvo que fixa
o órgão a observar, a unidade de contagem e a foto solicitada. Lagarta-do-cartucho se conta como
percentual de plantas atacadas no cartucho; cercosporiose se avalia como severidade na folha da
espiga. Sem alvo, o campo de contagem não tem unidade, a foto não tem órgão e a fase 5 não tem
rótulo para a IA.

A cobertura ampla vem do **tamanho do catálogo**, não da ausência de alvos: os 18 alvos abaixo
cobrem a maior parte do que se monitora em milho no país. Todos entram como `RegistroManual`
(roadmap, linhas 90-96); nenhum é apresentado como diagnóstico automático.

## Unidades de contagem

Conjunto fechado de unidades, derivado dos protocolos consultados. É o que permite o protocolo ser
genérico sem perder significado.

| Unidade | Significado | Usada em |
|---|---|---|
| `PercentualPlantasAtacadas` | % de plantas com o dano descrito, sobre as plantas avaliadas | Lagarta-do-cartucho |
| `PlantasAtacadasPor10mFileira` | Contagem de plantas atacadas em 10 m de fileira | Elasmo, rosca, broca-da-cana |
| `InsetosPorNPlantas` | Insetos contados sobre um número fixo de plantas | Percevejo-barriga-verde |
| `InsetosPorPlanta` | Insetos em uma planta | Pulgão-do-milho |
| `InsetosPorArmadilha` | Capturas por armadilha entre trocas | Cigarrinha-do-milho |
| `PercentualAreaFoliarLesionada` | Severidade por escala diagramática | Doenças foliares |
| `NotaEscala1a9` | Nota visual 1 (resistente) a 9 (suscetível) | Cercosporiose e demais, em ensaio |
| `Presenca` | Registro de presença/ausência, sem contagem | Alvos sem nível de controle |

## Pragas propostas (10)

| Alvo | Agente | Órgão observado | O que se conta | Nível de referência |
|---|---|---|---|---|
| Lagarta-do-cartucho | *Spodoptera frugiperda* | Cartucho, folhas, pendão, espiga | Plantas raspadas/perfuradas | ~20% de plantas atacadas; 10% com sintoma inicial em lavoura de alto potencial (≥100 sc) |
| Cigarrinha-do-milho | *Dalbulus maidis* | Cartucho, folhas; armadilha adesiva amarela | Adultos no cartucho ou por armadilha | **Sem nível estabelecido** — vetor de enfezamentos, a presença já justifica ação |
| Percevejo-barriga-verde | *Diceraeus melacanthus*, *D. furcatus* | Base do colmo | Percevejos por planta | 1 percevejo / 10 plantas |
| Pulgão-do-milho | *Rhopalosiphum maidis* | Folhas, pendão | Afídeos por planta | 100 afídeos/planta |
| Lagarta-elasmo | *Elasmopalpus lignosellus* | Colmo da plântula | Plantas murchas/tombadas em 10 m | Por nível de dano; **sem valor único** |
| Lagarta-rosca | *Agrotis ipsilon* | Colo da planta | Plantas cortadas em 10 m | Por nível de dano; **sem valor único** |
| Broca-da-cana | *Diatraea saccharalis* | Colmo | Plantas atacadas em 10 m | Por nível de dano; **sem valor único** |
| Lagarta-da-espiga | *Helicoverpa zea* | Estilo-estigma, grãos | Lagartas por espiga | **Não confirmado** |
| Larva-alfinete / vaquinha | *Diabrotica speciosa* | Raízes (adulto em folhas) | Presença / falhas de estande | **Não confirmado** |
| Corós | *Diloboderus abderus*, *Phyllophaga* spp. | Raízes, plântulas | Falhas de estande | **Não confirmado** |

Pragas de grãos armazenados (*Sitophilus zeamais*, *Sitotroga cerealella*) ficam fora: o produto
monitora a lavoura, não o armazém.

## Doenças foliares propostas (8)

| Alvo | Agente | Avaliação | Escala publicada |
|---|---|---|---|
| Mancha-branca | *Pantoea ananatis* (bactéria; antes atribuída a *Phaeosphaeria maydis*) | Folha da espiga ou imediatamente abaixo (Fe-1) | 8 níveis: 1, 3, 6, 13, 25, 43, 63, 79% |
| Cercosporiose | *Cercospora zeina*, *C. zeae-maydis* | Folha da espiga; severidade cresce após o florescimento | Nota 1–9, a cada 7 dias a partir de 60 DAE |
| Helmintosporiose | *Exserohilum turcicum* | Folha da espiga | 7 níveis: 0,5; 1,0; 2,5; 6,5; 15,5; 30,0; 54,0% |
| Antracnose foliar | *Colletotrichum graminicola* | Folha | 12 níveis, de 0,5% a 100% |
| Mancha-de-bipolaris | *Bipolaris maydis*, *B. zeicola* | Folha | **Não confirmado** |
| Ferrugem-polissora | *Puccinia polysora* | Face superior da folha | **Não confirmado** |
| Ferrugem-comum | *Puccinia sorghi* | Ambas as faces | **Não confirmado** |
| Ferrugem-tropical (branca) | *Physopella zeae* | Ambas as faces | **Não confirmado** |

Enfezamento vermelho e pálido e o raiado fino **não** entram como doença foliar: são sistêmicos,
transmitidos pela cigarrinha. Ficam registrados como consequência do alvo *Dalbulus maidis* e
podem virar alvos próprios depois, se o escopo passar de "foliar".

## O que não foi confirmado

Explicitado para não ser preenchido com chute:

- **Níveis de controle da maioria das pragas.** Só lagarta-do-cartucho, percevejo-barriga-verde e
  pulgão têm valor numérico citado. Os demais são decididos "em função do nível de dano", sem
  tabela única. O protocolo v1 deve deixar o campo de nível de referência **opcional**.
- **A Circular Técnica 208 (MIP na cultura do milho) não foi lida diretamente** — o PDF não pôde
  ser extraído nesta sessão. Os valores de 20% e 10% vêm da página de MIP da Embrapa e de matéria
  derivada. Convém conferir no PDF antes de gravar como dado de referência.
- **Doenças foliares não têm nível de dano econômico de uso corrente.** A decisão combina
  severidade, suscetibilidade da cultivar e estágio. O protocolo registra severidade; não recomenda
  aplicação.
- **As escalas diagramáticas são por doença, de artigos distintos** — não existe uma escala única
  de milho. Quatro das oito doenças não têm escala localizada.
- **A folha da espiga (Fe-1) como folha padrão** foi confirmada para mancha-branca, pela alta
  correlação com a planta inteira. Generalizar para as demais é razoável, mas não foi verificado.

## Fontes

- [Embrapa — Manejo Integrado de Pragas (milho)](https://www.embrapa.br/en/agencia-de-informacao-tecnologica/cultivos/milho/producao/pragas-e-doencas/pragas/manejo-integrado-de-pragas)
- [Embrapa — Circular Técnica 208, MIP na Cultura do Milho (PDF)](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1017489/1/circ208.pdf)
- [Embrapa — Doenças Foliares do milho](https://www.embrapa.br/en/agencia-de-informacao-tecnologica/cultivos/milho/producao/pragas-e-doencas/doencas/doencas-foliares)
- [Embrapa — Doenças na Cultura do Milho, Circular 83 (PDF)](https://www.infoteca.cnptia.embrapa.br/bitstream/doc/490415/1/Circ83.pdf)
- [Embrapa — Mancha-foliar-de-Phaeosphaeria: fungo ou bactéria? (PDF)](https://www.infoteca.cnptia.embrapa.br/bitstream/doc/979784/1/bol79.pdf)
- [Embrapa — Controle da cigarrinha do milho](https://www.embrapa.br/en/controle-da-cigarrinha-do-milho)
- [Embrapa — Avaliação da severidade da cercosporiose (PDF)](https://ainfo.cnptia.embrapa.br/digital/bitstream/item/102078/1/Avaliacao-severidade.pdf)
- [SciELO — Escala diagramática para avaliação da mancha branca em milho](http://www.scielo.br/j/sp/a/PS6HbTHP7rNHtcJnxc7nKDn/?lang=pt)
- [SciELO — Escala diagramática para severidade da helmintosporiose comum em milho](https://www.scielo.br/j/cr/a/5cDvMyVXYXMVLpKnDGnNM7h/?format=html&lang=pt)
- [SciELO — Validação de escala diagramática para a antracnose da folha do milho](https://www.scielo.br/j/sp/a/QLMWNxhdZ4TWrPsPKFC3zcn/?lang=pt&format=html)
- [EPAGRI/CIRAM — Pragas e doenças do milho: diagnose, danos e manejo (PDF)](https://ciram.epagri.sc.gov.br/ciram_arquivos/agroconnect/boletins/BT_PragasDoencasMilho.pdf)
- [AgroAdvance — 12 principais pragas do milho](https://agroadvance.com.br/blog-principais-pragas-do-milho/)
- [AgroAdvance — 4 importantes manchas foliares no milho](https://agroadvance.com.br/blog-manchas-foliares-no-milho/)
