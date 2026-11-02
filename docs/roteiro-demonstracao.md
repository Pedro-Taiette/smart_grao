# Roteiro da demonstração

A defesa conta uma história só: **o SmartGrão não espera o produtor achar a cercosporiose — avisa
quando o clima favorece, mostra onde o talhão destoa e manda conferir aquele ponto.** Cada passo
abaixo é uma tela, na ordem em que a história anda. Tempo total: 12 a 15 minutos.

## Antes de começar

Três processos, cada um num terminal:

```powershell
# 1. Serviço Python (satélite e modelo de imagem) — porta 8000
cd analytics/service
.venv/Scripts/python -m uvicorn smartgrao_analytics.app:app --port 8000

# 2. API — porta 5148
dotnet run --project backend/src/SmartGrao.WebApi

# 3. Frontend — porta 5173
npm --prefix frontend run dev
```

Conferir antes de entrar na sala:

- `http://localhost:8000/health` responde `ok`;
- **Aferimento** mostra os números do modelo (se aparecer "serviço de análise fora do ar", falta o 1);
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

## 2. Cadastro do cultivo (1 min)

Marília → Talhões → **Talhão Sul** → aba **Cultivo**.

- O cultivo é sempre milho; não há seletor de cultura.
- **Risco de cercosporiose**: o cadastro pergunta na língua do produtor como o híbrido reage, o que
  estava plantado antes e como foi o plantio. "Não sei" é aceito.

## 3. Quando olhar — clima (2 min)

Aba **Risco de cercosporiose**.

- O nível de hoje com o porquê: *"N noites úmidas e N dias com temperatura entre 22 e 30 °C nos
  últimos 14 dias"*, mais os fatores do cultivo.
- O risco dia a dia do ciclo, com a previsão hachurada.
- O aviso de estádio desatualizado: o sistema não adivinha o estádio, pede.
- **Vale vistoriar agora** — aparece com risco alto. O mesmo alerta está em **Hoje**.
- Falar das fontes: faixa de 22–30 °C, UR ≥ 90% e janela V4 em diante vêm de Paul & Munkvold; os
  cortes entre níveis são decisão de projeto, e a tela diz que o índice **não é calibrado**.

## 4. Safra passada — clima e satélite com dados reais (3 min)

Sorriso → **Talhao Demonstracao** → aba **Risco de cercosporiose**.

- **Risco dia a dia** da safrinha 2026: alto de fevereiro ao começo de maio, baixo com a seca. Clima
  real, da reanálise.
- **Onde o talhão destoa**: 39 passagens do Sentinel-2, só 18 com leitura — fevereiro e março quase
  todos encobertos. *"Saber que o satélite não viu é informação"*, e é por isso que o satélite não é
  condição para a vistoria.
- A curva de NDRE: plantio, dossel fechado em abril, senescência em junho. Clicar numa passagem de
  abril e mostrar as áreas no mapa.
- Dizer o limite: a 20 m por pixel o satélite vê vigor, não lesão. Aponta **onde** olhar.

## 5. Onde olhar — vistoria dirigida (2 min)

De volta ao Talhão Sul → **Agendar vistoria** (ou o botão do alerta).

- A tela pergunta só quem vai e quando. O sistema cria as 10 paradas nas áreas que o satélite marcou
  nas últimas três semanas, ou espalhadas pelo talhão se não houver área.
- A vistoria abre com as paradas no mapa sobre a imagem; verdes as já visitadas.
- A regra de 10 paradas × 10 plantas vem da extensão dos EUA — não existe protocolo brasileiro de
  detecção, e a pesquisa registra isso.

## 6. O que é — foto e IA (3 min)

Abrir a vistoria em andamento de 02/11 → **Parada 1**.

- Contagem por folha: *"Cercosporiose (folhas de baixo): 20%"*.
- **Tirar foto** abre a câmera no celular; a foto tem id gerado no aparelho, então reenviar depois de
  cair a rede não duplica.
- A foto passa pela fila e o modelo responde: *"Suspeita de cercosporiose — confirme"*, 64%.
- **Mostrar onde o modelo olhou**: o mapa de oclusão. Aqui o modelo olha também o fundo — é o viés
  que a pesquisa previu, e o motivo de existir a revisão.
- Revisar: escolher o nome e confirmar ou corrigir. Essa revisão é a verdade de campo.

Na aba de risco do Talhão Sul, o painel **Estação sentinela (simulada)** mostra fotos automáticas
chegando pela mesma rota — e a tela diz que são simuladas.

## 7. O sistema medindo a si mesmo (2 min)

**Aferimento**.

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

## 8. O que não está validado (1 min, dizer sem ser perguntado)

- O índice climático não foi calibrado com ocorrências brasileiras.
- O satélite não foi comparado com doença confirmada.
- O modelo foi medido em fotos dos EUA, da África do Sul e de Bangladesh.
- Sem lavoura no semestre, nenhuma das três camadas tem validação agronômica — o sistema está pronto
  para gerá-la na próxima safra, porque cada revisão alimenta o aferimento.
