# Fase 7 — Fotos e vistoria dirigida (camada 3)

As duas camadas autônomas se juntam e levam alguém ao ponto certo, que volta com a foto da folha.
Ver o [roadmap](roadmap-cercosporiose.md).

**Critério:** um alerta vira uma vistoria sugerida com pontos na zona; a pessoa fotografa e as fotos
aparecem no histórico da parada.

## Vistoria dirigida

- **Novo modo de amostragem `Directed`** — **10 paradas**, a única regra numérica encontrada para
  doença foliar de milho (10 paradas × 10 plantas, South Dakota State; ver a
  [pesquisa](pesquisa-cercosporiose.md#1-amostragem-para-detecção)).
- As paradas vão para dentro das **áreas que destoam** da passagem de satélite mais recente com
  área marcada, dos **últimos 21 dias**. Dividem-se pela área de cada zona, com ao menos uma por
  zona. Numa zona pequena demais para a malha, a parada vai para um ponto interno dela.
- Sem zona recente (nuvem, ou nada destoa), as 10 paradas cobrem o **talhão inteiro**. A camada 2 não
  é condição para a vistoria, como o roadmap pede.
- O plano guarda a data da passagem (`zone_date`); a escolha da caminhada mostra *"10 paradas nas
  áreas do satélite de 05/04"* ou *"10 paradas pelo talhão inteiro"*.
- O fluxo: o alerta de risco alto (fase 5) abre o agendamento já na aba de vistorias; ali, **"Gerar
  10 paradas onde o satélite viu a lavoura destoar"** cria o plano e o seleciona. A pessoa confirma
  o agendamento — nada é marcado sozinho.

## Fotos

- **`Photo`** — ligada à parada (`Observation`), ao alvo e à **folha** (folhas de baixo ou folha da
  espiga, conforme o protocolo). Copia da parada o estádio, o GPS e a precisão, e guarda a
  **origem** (`Phone`, `Station`, `Drone`) e se é **simulada**.
- **Id gerado no cliente** (`crypto.randomUUID()`): reenviar a mesma foto depois de uma falha
  devolve a que já chegou, em vez de duplicar. Um id já usado por outra parada é recusado.
- Validação: JPEG, PNG ou WebP, até 10 MB.
- **Armazenamento** atrás de `IPhotoStorage`. No desenvolvimento, disco local
  (`Storage:PhotoRoot`, padrão `data/photos` ao lado da API, fora do git). Armazenamento de objetos
  (MinIO ou R2) troca só a implementação.
- Foto de dispositivo (estação sentinela, drone) entra por outra rota, presa ao cultivo e sem parada.
  É o que a estação simulada vai usar.

## API

| Método | Rota | Uso |
|---|---|---|
| POST | `/api/cultivations/{id}/directed-plan` | Gera o plano dirigido |
| POST | `/api/photos` (multipart) | Foto de uma parada: `id`, `inspectionId`, `observationId`, `targetId`, `organ`, `takenAt`, `file` |
| POST | `/api/photos/device` (multipart) | Foto de estação ou drone: `id`, `cultivationId`, `source`, `isSimulated`, `organ`, `takenAt`, `latitude`, `longitude`, `file` |
| GET | `/api/photos?inspectionId=&cultivationId=` | Fotos da vistoria ou do cultivo |
| GET | `/api/photos/{id}/content` | O arquivo |

Erros: `photo.*` (ver `SmartGraoErrors.Photo`).

## Telas

- **Agendar vistoria**: o botão do plano dirigido, e a caminhada descrita pela origem das paradas.
- **Vistoria**: cada parada mostra as fotos (*"Fotos (1 de 3 pedidas)"*), a folha de cada uma e o
  botão **Tirar foto**, que abre a câmera do celular (`capture="environment"`). Falha de envio deixa a
  foto na fila da tela, com **Tentar de novo** usando o mesmo id.
- As contagens da parada dizem a folha: *"Cercosporiose (folhas de baixo): 20%"*.

## Correção de passagem

Horário com fuso diferente de UTC (`2026-11-02T10:00:00-03:00`) derrubava a gravação da parada com
500 — o PostgreSQL só aceita `timestamptz` em UTC pelo Npgsql. A observação e a foto agora convertem
para UTC na entrada. O frontend já mandava em UTC; outro cliente, como a estação simulada, não
mandaria.

## Banco e migração

Migration: `PhotosAndDirectedPlans`. **Aplicada** no ambiente configurado. Cria `photos` (chaves
estrangeiras `Restrict`: a foto é evidência) e acrescenta `sampling_plans.zone_date`.

## Verificação

```powershell
dotnet test backend/SmartGrao.slnx
npm --prefix frontend run build
```

200 testes de domínio; novos em `Sampling/DirectedPlanTests.cs` e `Photos/PhotoTests.cs`.

Contra o banco, na Fazenda Santa Rita (Marília): plano dirigido de 10 paradas pelo talhão inteiro
(não há passagem recente com zona), vistoria de cercosporiose agendada e iniciada, parada com 20% de
plantas atacadas nas folhas de baixo, foto enviada, **reenviada com o mesmo id sem duplicar**,
listada e servida. A tela da vistoria mostra a miniatura na parada.

Sem lavoura no semestre, as fotos da demonstração vêm do conjunto de imagens da trilha B.
