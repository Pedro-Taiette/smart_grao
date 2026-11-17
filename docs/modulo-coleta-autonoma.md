# Módulo de coleta autônoma — drone e rover

**Situação:** o **rover tem uma simulação** funcionando (`/talhoes/:id/rover`). O **drone** segue em
desenvolvimento, só como plano. A aba **Situação** do talhão mostra os dois com o rótulo certo
("Simulação" e "Em desenvolvimento").

## Por quê

Os três eixos do SmartGrão já dizem **quando** olhar (clima) e **onde** olhar (satélite). O elo
fraco é o terceiro, **o que é**: depende de alguém caminhar até as paradas e fotografar as folhas. É
o passo que mais custa ao produtor e o que menos acontece na prática. Coleta autônoma fecha o ciclo
sem gente: o satélite marca a área, a máquina vai até ela, a IA analisa a foto, alguém só revisa.

## Drone e rover não fazem a mesma coisa

A cercosporiose começa nas **folhas de baixo, sob o dossel** (ver
[pesquisa](pesquisa-cercosporiose.md)). Isso separa os dois papéis:

| | Drone | Rover |
|---|---|---|
| Onde está | Acima da lavoura | Na entrelinha, entre as plantas |
| O que vê | A copa, com muito mais detalhe que os 20 m/pixel do satélite | As folhas de baixo, de perto |
| Eixo que reforça | **2 — onde olhar**: confirma e recorta as áreas que o satélite marcou | **3 — o que é**: fotos de folha para a IA |
| Limite | Não vê a folha de baixo; regulamentação de voo | Velocidade; terreno; milho alto fecha a entrelinha |

Dizer na banca que o drone "detecta cercosporiose" seria falso pelo mesmo motivo que o satélite não
detecta: ele vê vigor da copa, não a lesão inicial.

## O que já existe e será reaproveitado

- **Origem da foto.** `PhotoSource` já tem `Phone`, `Station` e `Drone`. Rover entra como origem nova.
- **Rota de dispositivo.** `POST /api/photos/device` recebe foto de equipamento fixo ou móvel, com id
  gerado no aparelho (reenviar não duplica). A estação sentinela simulada já usa essa rota.
- **Fila de análise.** A foto entra na mesma fila da IA (fase 8) e aparece no eixo 3 junto das de
  campo, marcada com a origem.
- **Paradas dirigidas.** `POST /api/cultivations/{id}/directed-plan` já calcula as 10 paradas nas
  áreas do satélite (fase 7) — é a rota que o rover percorreria.
- **Simulador de referência.** `analytics/service/simulate_station.py` envia fotos do conjunto de
  teste como se fosse a estação; o simulador do rover segue o mesmo molde.

## O rover simulado (feito)

Um mini-módulo à parte, aberto pelo talhão (**Ver o rover em ação**, ou **Mandar o rover** ao lado
de "Vistoriar agora"). O que é simulado e o que é real:

| Parte | Simulada ou real |
|---|---|
| Rota: base → áreas do satélite → base | **Calculada** das áreas reais do satélite (as das últimas 3 semanas, as maiores primeiro); sem área, pontos espalhados |
| Rover andando no mapa, com rumo, distância e "chega em" | **Simulado** (animação acelerada, ~40 s a volta) |
| Foto da folha de baixo em cada parada | **Simulada**: imagem de exemplo do conjunto de teste público |
| Envio da foto | **Real**: `POST /api/photos/device`, origem `Rover`, `isSimulated = true`, com a posição da parada |
| Fila de análise e modelo de IA | **Reais**: os mesmos das fotos de campo, com o mapa de onde o modelo olhou |
| Revisão | **Real**: as fotos entram no eixo 3 do talhão como "rover · simulada" |

A tela mostra, em cada parada, o palpite da IA ao lado do **gabarito do conjunto** (o rótulo que o
conjunto público dá à foto) e conta quantas a IA acertou. É honesto com a banca: a simulação não
escolhe o resultado, o modelo é que responde.

**Fotos de exemplo:** 11 imagens (6 cercosporiose, 5 sadias) da divisão de **teste** do *Seasonal
Corn Leaf Disease* (CC BY 4.0, Bangladesh), em `frontend/public/rover-samples/` com a atribuição. O
modelo não as viu no treino. São de outra lavoura, e a tela diz isso.

**Backend:** `Rover` entrou em `PhotoSource`. A origem é gravada como texto (até 16 caracteres),
então não houve migration.

Código: `frontend/src/features/rover/` — `route.ts` (paradas e caminho), `useRoverSimulation.ts`
(movimento, paradas, envio) e `pages/RoverPage.tsx` (mapa, câmera, como o rover analisa).

## Próximos passos

- **Drone:** sobrepor ao mapa do eixo 2 uma imagem de alta resolução de exemplo, recortada pelo
  contorno, para mostrar o ganho de detalhe sobre o Sentinel-2 — marcada como simulada.
- **Rover real:** guardar a trilha do equipamento (posições ao longo do tempo) para o mapa seguir um
  rover de verdade, e não uma animação.

## Fora do escopo do POC

Navegação real, controle de voo, ortomosaico, autorização de voo e integração com fabricante. Antes
de qualquer hardware real, conferir a regulamentação de drones vigente e se a entrelinha do milho
comporta o rover no estádio de interesse (V4 a R2).
