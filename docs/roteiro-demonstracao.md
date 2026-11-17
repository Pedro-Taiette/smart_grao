# Roteiro da demonstração

A defesa conta uma história só: **o SmartGrão não espera o produtor achar a cercosporiose — avisa
quando o clima favorece, mostra onde o talhão destoa e manda conferir aquele ponto.** Cada passo
abaixo é uma tela, na ordem em que a história anda. Tempo total: 12 a 15 minutos.

## Antes de começar

Dois cliques em **`start.cmd`**, na raiz: sobe o serviço Python, a API e o frontend e abre o
navegador. Para subir cada parte num terminal, ver [rodando.md](rodando.md).

Conferir antes de entrar na sala:

- `http://localhost:8000/health` responde `ok`;
- **Ajustes → Desempenho da IA** mostra os números do modelo (se aparecer "serviço de análise fora
  do ar", rode `stop.cmd` e `start.cmd` de novo);
- a internet funciona: clima, satélite e o fundo dos mapas vêm de fora.

Dados usados (já estão no banco):

| O quê | Onde |
|---|---|
| Safrinha 2026 encerrada, lavoura real | Fazenda Santa Rita (Sorriso-MT) → **Talhao Demonstracao** |
| Safra em andamento com risco alto hoje, vistoria e estação simulada | Fazenda Santa Rita (Marília) → **Talhão Sul** |

## 1. O problema (1 min, sem tela)

A cercosporiose começa nas folhas de baixo, sob o dossel. Quando dá para ver de longe, já passou da
hora. Uma IA que só olha a foto depende de alguém já desconfiar e acertar a folha. Por isso o sistema
tem três camadas: **quando** olhar (clima), **onde** olhar (satélite) e **o que** é (foto + IA).

## 2. Cadastro do plantio (1 min)

Marília → Talhões → **Talhão Sul** → aba **Plantio**.

- O plantio é sempre milho; não há seletor de cultura nem cadastro de safra — a safra sai da data.
- **Sobre o plantio**: o cadastro pergunta na língua do produtor se o híbrido resiste, o que estava
  plantado antes e como foi o plantio. "Não sei" é aceito, e pode ser respondido depois.

## 3. Os três eixos de uma vez (1 min)

Tela **Hoje**: o cartão do Talhão Sul mostra a decisão em uma frase e os três eixos numerados —
**1 Clima** (quando olhar), **2 Satélite** (onde olhar), **3 Fotos + IA** (o que é). A decisão é o
cruzamento dos três: foto confirmada pesa mais; depois a vistoria sugerida pelo clima, dizendo se o
satélite já sabe onde; depois satélite e clima sozinhos. Nenhum limiar novo: o cruzamento só junta as
frases. Clicar em **Abrir talhão**: a aba **Situação** repete a decisão em cima e tem uma seção por
eixo, na mesma numeração.

## 4. Quando olhar — clima (2 min)

Seção **1 · O clima favorece a doença?**

- O nível de hoje com o porquê: *"N noites úmidas e N dias com temperatura entre 22 e 30 °C nos
  últimos 14 dias"*, mais o que se sabe do plantio.
- O risco dia a dia do ciclo, com a previsão hachurada.
- O aviso de estágio desatualizado: o sistema não adivinha o estágio, pede — e o botão **Informar
  estágio** abre o formulário direto.
- Com risco alto, a decisão vira **Vistorie agora**, com o botão **Vistoriar agora**.
- Falar das fontes: faixa de 22–30 °C, UR ≥ 90% e janela V4 em diante vêm de Paul & Munkvold; os
  cortes entre níveis são decisão de projeto, e a tela diz que o índice **não é calibrado**.

## 5. Safra passada — clima e satélite com dados reais (3 min)

Sorriso → **Talhao Demonstracao** → aba **Situação**.

- **Risco dia a dia** da safrinha 2026: alto de fevereiro ao começo de maio, baixo com a seca. Clima
  real, da reanálise.
- Seção **2 · Onde a lavoura destoa?**: 39 passagens do Sentinel-2, só 18 com leitura — fevereiro e março quase
  todos encobertos. *"Saber que o satélite não viu é informação"*, e é por isso que o satélite não é
  condição para a vistoria.
- A curva de NDRE: plantio, dossel fechado em abril, senescência em junho. Clicar numa passagem de
  abril e mostrar as áreas no mapa.
- Dizer o limite: a 20 m por pixel o satélite vê vigor, não lesão. Aponta **onde** olhar.
- As imagens são buscadas sozinhas ao abrir o talhão (se a última passagem tem mais de 5 dias, o
  intervalo de revisita do Sentinel-2). A primeira busca de uma safra leva de 1 a 2 minutos: abrir o
  talhão antes de entrar na sala.

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

## 7. O que é — foto e IA (3 min)

Abrir a vistoria em andamento de 02/11 → **Parada 1**.

- Contagem por folha: *"Cercosporiose (folhas de baixo): 20%"*. Para mostrar o registro, **Registrar
  parada 2**: o GPS liga sozinho e a contagem é por toque — *"das 10 plantas, quantas tinham
  mancha?"*, de 0 a 10 ou **Não olhei**. O sistema converte para percentual. Feche com **Cancelar**
  para não mexer nos dados da demonstração.
- **Foto: folhas de baixo** abre a câmera no celular; a foto tem id gerado no aparelho, então reenviar depois de
  cair a rede não duplica.
- A foto passa pela fila e o modelo responde: *"Suspeita de cercosporiose — confirme"*, 64%.
- **Mostrar onde o modelo olhou**: o mapa de oclusão. Aqui o modelo olha também o fundo — é o viés
  que a pesquisa previu, e o motivo de existir a revisão.
- Revisar: escolher o nome e confirmar ou corrigir. Essa revisão é a verdade de campo.

Na aba **Situação**, a seção **3 · O que a IA viu nas fotos?** junta todas as fotos do plantio — de
campo e da estação sentinela **simulada**, marcada como tal —, as que esperam revisão primeiro. O botão
**Revisar fotos** da decisão abre a primeira suspeita de cercosporiose.

## 8. O sistema medindo a si mesmo (2 min)

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

## 9. O que não está validado (1 min, dizer sem ser perguntado)

- O índice climático não foi calibrado com ocorrências brasileiras.
- O satélite não foi comparado com doença confirmada.
- O modelo foi medido em fotos dos EUA, da África do Sul e de Bangladesh.
- Sem lavoura no semestre, nenhuma das três camadas tem validação agronômica — o sistema está pronto
  para gerá-la na próxima safra, porque cada revisão alimenta o aferimento.
