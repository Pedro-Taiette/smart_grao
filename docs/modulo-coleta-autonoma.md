# Módulo de coleta autônoma — drone e rover (WIP)

**Situação: em desenvolvimento.** Nada aqui coleta dado ainda. A tela mostra o módulo como "Em
desenvolvimento" na aba **Situação** do talhão, sem botão e sem dado.

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

## POC proposta (mock)

Sem hardware nem lavoura, a primeira versão é uma **simulação**, marcada como simulada em toda tela:

1. `simulate_rover.py` pega o plano dirigido de um plantio e "percorre" as paradas em ordem.
2. Em cada parada, envia 1–2 fotos de folha de baixo do conjunto de teste pela rota de dispositivo,
   com a posição da parada e origem *rover*.
3. A vistoria dirigida mostra o trajeto no mapa e as paradas cobertas pelo rover; as fotos entram no
   eixo 3 com a etiqueta "rover · simulado".
4. Para o drone: sobrepor ao mapa do eixo 2 uma imagem de alta resolução de exemplo recortada pelo
   contorno, para mostrar o ganho de detalhe sobre o Sentinel-2 — também marcada como simulada.

Mudanças de backend esperadas: `Rover` em `PhotoSource` (a origem é gravada como texto de até 16
caracteres, então não precisa de migration) e, se o trajeto for mostrado, guardar a sequência de
posições do equipamento.

## Fora do escopo do POC

Navegação real, controle de voo, ortomosaico, autorização de voo e integração com fabricante. Antes
de qualquer hardware real, conferir a regulamentação de drones vigente e se a entrelinha do milho
comporta o rover no estádio de interesse (V4 a R2).
