# Amostragem: fundamentação técnica

> Este documento existe para responder a uma pergunta que precede o código: **quantos pontos de
> amostragem um talhão deve ter, e por quê?** A regra implementada no Pilar 2 sai daqui, e não de
> uma fórmula escolhida por conveniência.

Levantamento feito em setembro de 2026. As fontes estão no fim.

---

## O que a literatura diz

### 1. Existe protocolo oficial, e ele não é linear na área

O MIP-Soja da Embrapa recomenda uma quantidade de pontos que **satura** e, a partir de certo
tamanho, troca de estratégia: em vez de amostrar mais, manda subdividir a área.

| Área | Pontos de amostragem |
|---|---|
| 1–9 ha | 6 |
| 10–29 ha | 8 |
| 30–99 ha | 10 |
| > 100 ha | dividir a área em talhões e repetir o procedimento |

A tabela está transcrita literalmente do material de Corrêa-Ferreira (Embrapa Soja). Vale reparar que
a faixa acima de 100 ha **não** traz um número de pontos: traz uma instrução de subdividir. Não é
uma observação sobre precisão, é o protocolo trocando de estratégia.

Há ainda a regra alternativa de **no mínimo 1 ponto a cada 10 ha**, com a ressalva de que quanto
mais pontos e menor o intervalo entre amostragens, melhor a precisão do monitoramento.

O método de campo é o **pano-de-batida**: um pano branco de 1 m × 1,20 m colocado entre as fileiras,
sobre o qual uma fileira de plantas é sacudida para que os insetos caiam e sejam contados.

**Consequência para o projeto:** uma regra do tipo "1 ponto a cada N hectares, sem teto" diverge do
protocolo já na faixa dos 30 ha, e a partir de 100 ha propõe uma caminhada que o protocolo
explicitamente não pede.

### 2. Para *mapear* a infestação, a densidade é uma ordem de grandeza maior

Um estudo de monitoramento georreferenciado de lagartas desfolhadoras em soja (48 ha em Júlio de
Castilhos/RS, safra 2008/09) comparou três grades regulares:

| Grade | Pontos | Densidade | Resultado |
|---|---|---|---|
| 50 × 50 m | 181 | ~3,8/ha | mapas temáticos mais detalhados; menor índice de variação |
| 71 × 71 m | 96 | ~2,0/ha | adequada |
| 100 × 100 m | 47 | ~1,0/ha | precisão reduzida, mas ainda viável para caracterizar a distribuição |

As três grades serviram para caracterizar a distribuição espacial. A conclusão prática do estudo é
um *trade-off* explícito: quanto menor a malha, mais acuradas as avaliações — e maior o tempo e o
custo do monitoramento.

### 3. A tensão central: decidir *se* aplica ≠ decidir *onde* aplica

É o achado mais importante deste levantamento.

| Objetivo | Densidade de referência | Origem |
|---|---|---|
| Decidir **se** aplica (limiar de dano) | ~1 ponto / 10 ha | MIP-Soja |
| Decidir **onde** aplica (mapa de mancha) | ~1 a 4 pontos / ha | estudo de densidade amostral |

Uma diferença de ~10× que não vem do tamanho do talhão, e sim da **pergunta que a amostragem
responde**. Tratar as duas com a mesma fórmula produz ou um mapa inútil, ou uma caminhada que
ninguém faz.

O SmartGrão tem as duas ambições — Pilar 2 (diagnóstico ponto a ponto) e Pilar 3 (manchas de
infestação em larga escala). Por isso a densidade aqui é derivada do **propósito**, não fixada.

### 4. Para decisão, o estado da arte nem usa número fixo

Planos de **amostragem sequencial** param quando a decisão já está estatisticamente resolvida, em
vez de percorrer um número pré-definido de pontos.

Para percevejos, um plano binomial com limiar de contagem de 3 percevejos por 25 redadas atingiu
≥99% de probabilidade de decisão correta (≤1% de decisão incorreta) com média de **12 a 18** unidades
amostrais — abaixo dos planos fixos comparados. Há trabalho equivalente recente para lagartas.

**Consequência para o projeto:** a malha fixa é um bom ponto de partida, mas há um caminho de
evolução barato e academicamente forte — o app parar de pedir pontos quando a decisão já está clara.

### 5. Para doenças, frequência pesa mais que densidade

No caso da ferrugem asiática, as recomendações giram em torno de **quando e onde olhar**, não de
quantos pontos uniformes percorrer:

- amostragem semanal a partir do segundo/quarto trifólio completamente desenvolvido;
- coleta de 20 folíolos centrais dos pecíolos inseridos na haste principal;
- monitoramento no mínimo **duas vezes por semana** em período crítico;
- atenção maior às primeiras semeaduras e a locais com maior acúmulo de umidade;
- os primeiros sintomas aparecem no **baixeiro** — folhas mais baixas, mais úmidas e menos
  iluminadas.

**Consequência para o projeto:** o plano de amostragem precisa de **cadência**, não ser um evento
único. E a amostragem dirigida por risco (baixada, bordadura úmida, talhão semeado primeiro) é mais
promissora, para doença, do que aumentar a densidade da grade.

### 6. A bordadura não é ruído de borda — é onde a praga está

A prática de "não amostrar na bordadura" costuma ser justificada por condições atípicas de borda.
Não é esse o motivo. O material de Corrêa-Ferreira traz um caso real (Coamo) que mostra a razão de
verdade, sob o título *"Pontos de amostragem mal distribuídos na lavoura"*:

- amostragem feita ao longo do carreador e da mata: **média de 67 percevejos/m**;
- caminhando para o interior, o nível populacional cai: **67 → 43 → 18 → 5 → 0 percevejos/m**;
- legenda da fonte: *"População de percevejos concentrada na bordadura"*.

A infestação **entra** pela borda — os percevejos migram da mata, do banhado e das áreas úmidas
vizinhas, colonizam a bordadura e só depois avançam para dentro. Ou seja, a borda não dá uma medida
ruim: dá uma medida **verdadeira de um lugar não representativo**, com uma ordem de grandeza de
diferença em relação ao interior.

**Consequência para o projeto — e ela é diferente para cada modo:**

| Modo | O que fazer com a borda | Por quê |
|---|---|---|
| **Monitoramento** | descartar uma faixa | A pergunta é *"qual a média do talhão?"*. Um ponto na borda contamina a média e dispara aplicação em área inteira que não precisava |
| **Mapeamento** | **manter, sem descarte** | A pergunta é *"onde aplicar?"*. O gradiente 67→0 **é o sinal**; apagar a borda apaga justamente a zona que justifica a aplicação em faixa |

Tratar a bordadura como um recorte único aplicado sempre seria, no modo Mapeamento, descartar o dado
mais informativo que o método produz.

Vale ainda registrar o que a fonte critica: pontos **mal distribuídos**, agrupados ao longo de um
acesso. Uma grade regular resolve isso por construção — a distribuição uniforme não é conveniência
de implementação, é resposta a uma falha documentada de amostragem manual.

---

## O que o mercado faz

| Sistema | Abordagem |
|---|---|
| **Climate FieldView** | pin manual de scouting + imagem de satélite de saúde da lavoura para dirigir onde olhar |
| **Cropwise Protector** (Syngenta) | agrega coletas ao nível do talhão, compara com o limiar e colore o que está acima; mantém timeline dos eventos de scouting |
| **Strider** | identifica hotspots de infestação para aplicação localizada; comunica até 15% de redução no uso de defensivos |
| **Aegro (MIP)** | planejamento do monitoramento, definição de quais pontos verificar, registro em campo com foto |

Dois padrões se repetem:

1. **A grade é andaime, não produto.** O valor entregue é dirigir a caminhada (por imagem ou
   histórico), agregar contra o limiar e produzir o mapa de mancha para aplicação localizada.
2. **Nenhum deles faz diagnóstico automático por foto no ponto.** É exatamente a lacuna que o Pilar 2
   do SmartGrão ocupa — e o que transforma a nossa malha georreferenciada em mapa de severidade sem
   depender de um agrônomo classificando manualmente.

---

## A regra adotada no SmartGrão

Duas derivações de densidade sobre o **mesmo** serviço de domínio: grade regular sobre a caixa
envolvente do talhão, recortada por `Contains`. Só muda como o espaçamento nasce.

### Modo Monitoramento — decidir *se* aplica

Alinhado ao MIP-Soja:

```
   1–9 ha   ->  6 pontos
 10–29 ha   ->  8 pontos
 30–99 ha   -> 10 pontos
 > 100 ha   -> 10 pontos + aviso de que o talhão deveria ser subdividido
```

Como o sistema tem a geometria do talhão, o aviso acima de 100 ha pode evoluir para uma sugestão de
subdivisão de verdade, em vez de um texto.

A grade não produz a contagem exata do alvo: recortada pelo contorno, ela cai onde cai. A busca é
pelo **maior espaçamento cuja malha ainda entrega pelo menos o número de pontos da tabela**, e o
plano grava o alvo e a contagem real lado a lado.

Aceitar um excedente, em vez de descartar pontos até bater o número, é deliberado. Todo critério de
descarte que consideramos ou introduz viés espacial (cortar os últimos da ordem, cortar os mais
próximos da borda) ou depende de um desempate arbitrário — nenhum tem respaldo em fonte. E o
protocolo estabelece um número mínimo de pontos, não um teto. Se o excedente se mostrar grande em
talhões de formato irregular, existe caminho publicado para contagem exata sem descarte:
*spatial coverage sampling* por estratificação k-means (Walvoort, Brus & de Gruijter), que põe um
ponto no centroide de cada um de N estratos compactos. Fica registrado como alternativa a adotar
com medida na mão, não por preferência.

### Modo Mapeamento — decidir *onde* aplica

Alinhado ao estudo de densidade amostral, com os três espaçamentos validados:

```
50 m  ->  ~3,8 pontos/ha   (mais detalhe, mais caminhada)
71 m  ->  ~2,0 pontos/ha
100 m ->  ~1,0 ponto/ha    (padrão sugerido)
```

Um talhão de 100 ha em grade de 100 m gera ~100 pontos.

### A bordadura

Decorre direto do item 6 acima:

```
Monitoramento -> descarta uma faixa de spacing / 2
Mapeamento    -> não descarta nada
```

**Por que `spacing / 2` e não um número em metros.** A distância de bordadura em metros não está
confirmada em fonte primária (ver "Em aberto"), e preencher a lacuna com um valor plausível é
exatamente o que este documento existe para evitar. `spacing / 2` não é um chute: sai da geometria
da própria amostragem. Um ponto de uma grade regular representa uma célula de lado igual ao
espaçamento; um ponto colado na divisa teria metade da sua célula fora do talhão. Erodindo o
contorno por meio espaçamento, todo ponto retido representa uma célula inteiramente dentro da área.

Como aferição de sanidade — não como fonte —, num talhão de 10 ha com 6 pontos isso dá cerca de
64 m de bordadura, mesma ordem de grandeza dos 30–50 m que a prática de campo costuma citar.

A regra é um **default do domínio**, não um parâmetro exigido de quem chama. Se a distância viesse
da API ou de configuração, uma regra agronômica passaria a ser decidida fora do domínio — e no caso
da configuração ainda sairia do código versionado, disfarçada de ajuste de infraestrutura.

### Por que não a regra que quase adotamos

A proposta inicial era "1 ponto a cada 5 ha, mínimo 5, máximo 60". Ela falha em três frentes:
é o dobro da densidade do protocolo na faixa intermediária, inventa um teto de 60 que não aparece
em nenhuma fonte, e — o principal — usa uma densidade só para duas perguntas que a literatura trata
separadamente por um fator de 10.

---

## Em aberto

- **Distância mínima da bordadura, em metros.** Continua sem fonte primária. O *porquê* da bordadura
  ficou resolvido (item 6), mas o número não: o material de Corrêa-Ferreira mostra o gradiente
  67→43→18→5→0 percevejos/m sem rótulos de distância no texto — devem estar apenas na imagem do
  slide. O Documentos 143 da Embrapa é relato experimental de safra, não protocolo, e não traz o
  número. A página do MIP-Soja do IDR-Paraná diz que a quantidade e a localização dos pontos devem
  garantir amostragem representativa, sem quantificar. Os "30 m" que circulam vêm de material de
  divulgação (Aegro, LinkedIn), não de publicação Embrapa.
  **Cuidado com um falso positivo:** aparece "30 a 50 metros" em fonte Embrapa, mas referente ao
  **espaçamento entre armadilhas de feromônio** — outra pergunta, número parecido.
  Enquanto não houver fonte, vale a regra derivada `spacing / 2`.
- **Cadência do plano.** Semanal para MIP, duas vezes por semana para ferrugem em período crítico.
  Falta decidir se a cadência é atributo do plano de amostragem ou configuração por cultura.
- **Amostragem sequencial.** Fora do escopo da Fase 3, mas é a evolução natural do modo
  Monitoramento.
- **Amostragem dirigida por risco.** Para doença, provavelmente vale mais que grade uniforme. Depende
  do Pilar 3 (imagem) para identificar as zonas de risco.

---

## Fontes

**Protocolo e recomendação oficial**

- [Corrêa-Ferreira, B. S. — *Métodos de amostragem de pragas da soja*, Embrapa Soja (PDF)](https://www.embrapa.br/documents/1355202/1529289/M%C3%A9todos+de+amostragem+de+pragas+da+soja+-+Beatriz+S.+Corr%C3%AAa-Ferreira.pdf/a376b149-6056-eee1-16d5-e5ed5cca8ec9) — **é a fonte da tabela de pontos por área e do caso Coamo sobre a bordadura**
- [Monitoramento da lavoura — Embrapa, Agência de Informação Tecnológica](https://www.embrapa.br/en/agencia-de-informacao-tecnologica/cultivos/soja/producao/manejo-integrado-de-pragas/monitoramento-da-lavoura)
- [Percevejos fitófagos — Embrapa Agropecuária Oeste](https://pragas.cpao.embrapa.br/views/praga.php?id=27) — colonização a partir da bordadura
- [Manejo Integrado de Pragas na Cultura da Soja — Embrapa, Documentos 143 (PDF)](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1098927/1/DOC14320182.pdf)
- [Manejo Integrado de Pragas na Soja (MIP-Soja) — IDR-Paraná](https://www.idrparana.pr.gov.br/Pagina/Manejo-Integrado-de-Pragas-na-Soja-MIP-Soja)
- [Ferrugem asiática da soja: manejo e prevenção — Embrapa Soja](https://www.embrapa.br/en/web/soja/ferrugem)

**Densidade amostral e distribuição espacial**

- [Densidade amostral aplicada ao monitoramento georreferenciado de lagartas desfolhadoras na cultura da soja — Ciência Rural (SciELO)](https://www.scielo.br/j/cr/a/dMrfxvGPwJK49pnxbtMcd9k/?format=html&lang=pt)
- Walvoort, D. J. J.; Brus, D. J.; de Gruijter, J. J. — *An R package for spatial coverage sampling
  and random sampling from compact geographical strata by k-means*, Computers &amp; Geosciences, 2010
  (pacote `spcosa`). Referência da alternativa de contagem exata; citada de segunda mão, ainda não
  lida na íntegra.

**Amostragem sequencial e tamanho de amostra**

- [Presence–Absence Sampling Plans for Stink Bugs (Hemiptera: Pentatomidae) in the Midwest Region of the United States — PubMed](https://pubmed.ncbi.nlm.nih.gov/33885759/)
- [Feasible Sequential Sampling Plans for Robust Decision-Making in Caterpillar Control in Soybean Crops — Journal of Applied Entomology](https://onlinelibrary.wiley.com/doi/abs/10.1111/jen.70145)
- [Sampling for Plant Disease Incidence — Madden & Hughes, Phytopathology (PDF)](https://apsjournals.apsnet.org/doi/pdf/10.1094/PHYTO.1999.89.11.1088)
- [An Effective Sample Size for Predicting Plant Disease Incidence in a Spatial Hierarchy — Phytopathology](https://apsjournals.apsnet.org/doi/10.1094/PHYTO.1999.89.9.770)

**Sistemas de mercado**

- [Work smarter with Field Health Imagery and scouting tools — Climate FieldView](https://climate.com/en-ca/resources/blog/work-smarter-with-field-health-imagery-and-scouting-tools.html)
- [Scouting for Pests, Progress and Profit — Syngenta Thrive (Cropwise Protector)](https://syngentathrive.com/articles/scouting-for-pests-progress-and-profit/)
- [Strider lança tecnologia móvel de combate a pragas — Agrolink](https://www.agrolink.com.br/noticias/strider-lanca-tecnologia-movel-de-combate-a-pragas-e-reduz-perdas-na-producao-rural_206285.html)
- [MIP — Solução Aegro para monitoramento de pragas e doenças — Orbia](https://www.orbia.ag/produto/65600/RA0659/0/mip-solucao-aegro-para-monitoramento-de-pragas-e-doencas)
