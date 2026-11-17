# Roteiro da demonstração

A defesa conta uma história só: **o SmartGrão não espera o produtor achar a cercosporiose — avisa
quando o clima favorece, mostra onde o talhão destoa e manda conferir aquele ponto.** Cada passo
abaixo é uma tela, na ordem em que a história anda. Tempo total: 12 a 15 minutos.

## Antes de começar

No terminal da pasta do projeto, **`demo.cmd reset`**: sobe tudo em Docker com o starter pack
(`docker/starter-pack.sql`) no estado inicial e abre `http://localhost:8080` (ver
[docker.md](docker.md)). Sem Docker, `start.cmd` sobe o sistema contra o banco compartilhado
([rodando.md](rodando.md)).

Conferir antes de entrar na sala:

- **Ajustes → Desempenho da IA** mostra os números do modelo (se aparecer "serviço de análise fora
  do ar", aperte Enter na janela do `demo.cmd` e rode `demo.cmd` de novo);
- a internet funciona: clima, satélite e o fundo dos mapas vêm de fora;
- abrir uma vez os três plantios abaixo: a primeira busca do satélite de cada um leva de 1 a 2
  minutos, e é melhor que aconteça antes.

Dados do starter pack (pessoas e fazendas fictícias, contornos de lavouras reais):

| O quê | Onde |
|---|---|
| Milho em andamento, dados do plantio em "Não sei", áreas marcadas pelo satélite | Sítio Santa Rita (Marília-SP) → **Talhão Sul** |
| Milho em andamento com os três fatores que agravam o risco | Fazenda Boa Esperança (Sorriso-MT) → **Talhão da Sede** |
| Safrinha 2026 encerrada | Fazenda Boa Esperança → **Talhão da Sede** → safra 2025/2026 |

Clima e satélite não vêm no pacote: o sistema busca sozinho, ao vivo. Por isso o nível de risco do
dia pode mudar de uma demo para outra — o roteiro não depende de um nível exato.

## 1. O problema (1 min, sem tela)

A cercosporiose começa nas folhas de baixo, sob o dossel. Quando dá para ver de longe, já passou da
hora. Uma IA que só olha a foto depende de alguém já desconfiar e acertar a folha. Por isso o sistema
tem três camadas: **quando** olhar (clima), **onde** olhar (satélite) e **o que** é (foto + IA).

## 2. Cadastro do plantio (1 min)

Sítio Santa Rita → Talhões → **Talhão Sul** → aba **Plantio**.

- O plantio é sempre milho; não há seletor de cultura nem cadastro de safra — a safra sai da data.
- **Sobre o plantio**: o cadastro pergunta na língua do produtor se o híbrido resiste, o que estava
  plantado antes e como foi o plantio. "Não sei" é aceito, e pode ser respondido depois.

## 3. Os três eixos de uma vez (1 min)

Tela **Hoje**: em cima, **Vigiado sozinho** × **Pede vistoria agora** em hectares — o valor do
sistema numa linha: em vez de percorrer o talhão todo, só onde clima e satélite apontaram. O cartão do
Talhão Sul mostra a decisão em uma frase e os três eixos numerados —
**1 Clima** (quando olhar), **2 Satélite** (onde olhar), **3 Fotos + IA** (o que é). A decisão é o
cruzamento dos três: foto confirmada pesa mais; depois a vistoria sugerida pelo clima, dizendo se o
satélite já sabe onde; depois satélite e clima sozinhos. Nenhum limiar novo: o cruzamento só junta as
frases. Clicar em **Abrir talhão**: a aba **Situação** repete a decisão em cima e tem uma seção por
eixo, na mesma numeração.

## 4. Quando olhar — clima (2 min)

Fazenda Boa Esperança → **Talhão da Sede** → seção **1 · O clima favorece a doença?**

- O nível de hoje com o porquê: *"N noites úmidas e N dias com temperatura entre 22 e 30 °C nos
  últimos 14 dias"*, mais o que se sabe do plantio — aqui, híbrido suscetível, milho sobre milho e
  plantio direto, os três que agravam.
- O risco dia a dia do ciclo, com a previsão hachurada.
- O estágio: o sistema não adivinha, pede. Passados 14 dias do último registro, avisa que está
  desatualizado, e o botão **Informar estágio** abre o formulário direto.
- Com risco alto, a decisão vira **Vistorie agora**, com o botão **Vistoriar agora**.
- Falar das fontes: faixa de 22–30 °C, UR ≥ 90% e janela V4 em diante vêm de Paul & Munkvold; os
  cortes entre níveis são decisão de projeto, e a tela diz que o índice **não é calibrado**.

## 5. Safra passada — clima e satélite com dados reais (3 min)

Talhão da Sede → plantio da **safrinha 2026** (safra 2025/2026) → aba **Situação**.

- **Risco dia a dia** da safrinha 2026: alto de fevereiro ao começo de maio, baixo com a seca. Clima
  real, da reanálise.
- Seção **2 · Onde a lavoura destoa?**: 39 passagens do Sentinel-2, só 18 com leitura — fevereiro e março quase
  todos encobertos. *"Saber que o satélite não viu é informação"*, e é por isso que o satélite não é
  condição para a vistoria.
- A curva de NDRE: plantio, dossel fechado em abril, senescência em junho. Clicar numa passagem de
  abril e mostrar as áreas no mapa.
- Dizer o limite: a 20 m por pixel o satélite vê vigor, não lesão. Aponta **onde** olhar.
- As imagens são buscadas sozinhas ao abrir o talhão (se a última passagem tem mais de 5 dias, o
  intervalo de revisita do Sentinel-2). A primeira busca de uma safra leva de 1 a 2 minutos — por
  isso abrir antes de entrar na sala.

## 6. Onde olhar — vistoria dirigida (2 min)

De volta ao Talhão Sul → **Vistoriar agora** (na decisão ou na aba **Vistorias**).

- Um clique: o sistema cria as 10 paradas nas áreas que o satélite marcou nas últimas três semanas
  (ou espalhadas pelo talhão se não houver área), inicia a visita e abre a primeira parada. Só
  pergunta quem vai se houver mais de uma pessoa — ou o nome, se não houver ninguém. Com uma visita
  já em andamento, o botão vira **Continuar a vistoria** e não cria outra. **Agendar para outro dia**
  continua ao lado.
- A vistoria abre com as paradas no mapa sobre a imagem; verdes as já visitadas.
- A regra de 10 paradas × 10 plantas vem da extensão dos EUA — não existe protocolo brasileiro de
  detecção, e a pesquisa registra isso.

## 7. Sem ninguém em campo — o rover (2 min)

Talhão Sul → aba **Situação** → **Mandar o rover (simulação)**.

- Dizer antes: *"Por que alguém dependeria da foto do celular? Se a pessoa já está olhando a folha,
  a IA é só segunda opinião. A IA faz sentido quando ninguém está olhando."*
- **Iniciar rota**: o rover sai da base e vai às áreas que o satélite marcou, com distância e "chega
  em" ao vivo. Em cada parada fotografa as folhas de baixo.
- A foto é de exemplo (conjunto público, Bangladesh) — a tela diz —, mas **o envio, a fila e o modelo
  são os de verdade**. Mostrar o palpite ao lado do gabarito do conjunto e **Mostrar onde a IA
  olhou**.
- No fim: *"A IA viu sinal em N fotos"*, e elas já estão no eixo 3 para alguém revisar de longe.
- Dizer o limite: drone vê a copa (reforça o *onde*); só quem anda na entrelinha vê a folha de baixo.

## 8. O que é — a IA e a revisão (2 min)

**Revisar fotos no talhão** (no fim da rota) ou a seção **3 · O que a IA viu nas fotos?** da aba
**Situação**: junta todas as fotos do plantio — de campo e do rover (estas marcadas como **simuladas**) —,
as que esperam revisão primeiro.

- O palpite do modelo: *"Suspeita de cercosporiose — confirme"*, com a confiança.
- **Mostrar onde o modelo olhou**: o mapa de oclusão. Às vezes o modelo olha também o fundo — é o
  viés que a pesquisa previu, e o motivo de existir a revisão.
- Revisar: escolher o nome e confirmar ou corrigir. Essa revisão é a verdade de campo.
- Em campo, a mesma coisa sai da parada da vistoria: **Foto: folhas de baixo** abre a câmera do
  celular; a foto tem id gerado no aparelho, então reenviar depois de cair a rede não duplica.

## 9. O sistema medindo a si mesmo (2 min)

**Ajustes → Desempenho da IA** (o aferimento).

- Modelo no teste público: aponta 90% das folhas com cercosporiose; 87% dos alertas estão certos;
  campo × laboratório separados.
- Por que não 98%: a divisão é por planta e sessão de captura. Trabalhos com 98% misturam a mesma
  planta no treino e no teste.
- **Numa lavoura que o modelo nunca viu**: treinado sem as fotos da África do Sul, acerta 63% das
  cercosporioses de lá, contra F1 0,98 na validação. É o número que mais importa e o motivo de a
  revisão humana existir.
- Revisões: as métricas recalculadas só com o que alguém daqui confirmou.
- **Antecedência**: vazia, com o motivo — sem lavoura, não houve confirmação em campo. É a medida que
  diria se a detecção foi precoce.
- **Exportar fotos revisadas**: o conjunto de treino da próxima versão.

## 10. O que não está validado (1 min, dizer sem ser perguntado)

- O índice climático não foi calibrado com ocorrências brasileiras.
- O satélite não foi comparado com doença confirmada.
- O modelo foi medido em fotos dos EUA, da África do Sul e de Bangladesh.
- Sem lavoura no semestre, nenhuma das três camadas tem validação agronômica — o sistema está pronto
  para gerá-la na próxima safra, porque cada revisão alimenta o aferimento.
