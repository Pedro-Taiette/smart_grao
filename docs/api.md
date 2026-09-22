# API

Base: `http://localhost:5148` em desenvolvimento. Documentação interativa em `/scalar/v1`,
documento OpenAPI em `/openapi/v1.json` e no arquivo versionado `openapi/SmartGrao.WebApi.json`.

## Endpoints

### Fazendas

| Verbo | Rota | O que faz |
|---|---|---|
| GET | `/api/farms` | lista |
| POST | `/api/farms` | cadastra; sede opcional |
| GET | `/api/farms/{id}` | detalha |
| PUT | `/api/farms/{id}` | atualiza |
| DELETE | `/api/farms/{id}` | exclui; **409** se ainda houver talhões ou safras |
| GET | `/api/farms/{id}/fields` | talhões com contorno em GeoJSON — é o que o mapa consome |

### Talhões

| Verbo | Rota | O que faz |
|---|---|---|
| POST | `/api/fields` | cria a partir do polígono desenhado |
| GET | `/api/fields/{id}` | detalha |
| PUT | `/api/fields/{id}` | renomeia e redesenha o contorno |
| PATCH | `/api/fields/{id}/status?active=` | ativa/desativa preservando o histórico |
| DELETE | `/api/fields/{id}` | exclui; **409** se houver cultivos ou planos |

`GET /api/farms/{id}/fields` aceita `?activeOnly=true`.

### Safras e cultivos

O contexto de cultura pertence ao cultivo, não ao talhão. Os endpoints de cadastro, consulta,
encerramento e estágios estão em [Fase 1 — API](fase-1-cultivos.md#api).

`GET /api/growth-stages?crop={c}` devolve a escala fenológica da cultura — milho e soja estão
transcritas, e o estágio registrado precisa estar nelas. Ver
[Escala fenológica](fase-2-protocolos.md#escala-fenológica).

### Alvos e protocolos

O catálogo de pragas e doenças foliares e os protocolos versionados de vistoria. Um protocolo
declara a sua cultura e só aceita alvos dela — é o que impede uma vistoria de milho de herdar o
MIP-Soja por omissão. Endpoints, regras e códigos de erro em
[Fase 2 — API](fase-2-protocolos.md#api); os alvos e as fontes agronômicas em
[Fase 2 — catálogo de alvos](fase-2-catalogo-alvos.md).

### Equipe e vistorias

A visita ao talhão amarra o cultivo (fase 1), o protocolo (fase 2) e a malha de amostragem: um plano
é caminhado muitas vezes, e cada caminhada é uma vistoria própria. Endpoints, regras e códigos de
erro em [Fase 3 — API](fase-3-vistorias.md#api).

`GET /api/inspections` aceita `farmId` além de `cultivationId`, `responsibleId` e `status` — a
agenda de quem opera é a da propriedade inteira, não a de um talhão. O resumo carrega `fieldName`
pelo mesmo motivo que já carregava `responsibleName`: numa lista que mistura talhões, um id não
situa ninguém. Ver [navegacao.md](navegacao.md).

### Safras e cultivos, por propriedade

`GET /api/cultivations` aceita `fieldId` **ou** `farmId`, ambos opcionais. O recorte por fazenda
existe para a tela de abertura: "quais talhões estão sem cultivo aberto" é pergunta sobre a
propriedade, e respondê-la por talhão faria o painel disparar uma requisição por talhão cadastrado.
Talhão inexistente continua sendo `field.not_found` (404), e não lista vazia.

### Planos de amostragem

| Verbo | Rota | O que faz |
|---|---|---|
| POST | `/api/sampling-plans` | gera a malha para um talhão |
| GET | `/api/sampling-plans/{id}` | detalha o plano **com** os pontos |
| DELETE | `/api/sampling-plans/{id}` | apaga o plano e a malha junto |
| GET | `/api/fields/{id}/sampling-plans` | histórico do talhão, mais recente primeiro, **sem** os pontos |

O corpo do `POST` identifica o talhão e seu cultivo. O espaçamento depende do modo:

```jsonc
{
  "fieldId": "…",
  "cultivationId": "…",
  "mode": "Monitoring",   // ou "Mapping"
  "spacingMeters": null   // obrigatório em Mapping; proibido em Monitoring
}
```

O cultivo precisa pertencer ao talhão e estar aberto. `Monitoring` é exclusivo para soja;
no milho, está disponível `Mapping`. Os planos anteriores à fase 1 retornam `cultivationId: null`.

**Por que o espaçamento é proibido no Monitoramento** e não apenas ignorado: nesse modo a densidade
vem da tabela do MIP-Soja, e aceitar um valor sem efeito faria o cliente acreditar que escolheu a
densidade. A recusa é `validation.failed` (422).

**A bordadura não está no contrato.** Ela é regra de domínio com default por modo — meio espaçamento
no Monitoramento, zero no Mapeamento. Pedi-la ao cliente moveria uma decisão agronômica para fora do
domínio. A fundamentação está em [amostragem.md](amostragem.md).

O plano devolve `targetPointCount` ao lado de `pointCount`, e `fallsShortOfTarget` quando o talhão é
pequeno demais para comportar o que o protocolo pede. `subdivisionRecommended` marca o talhão acima
de 100 ha, onde a fonte manda subdividir em vez de amostrar mais.

Os pontos saem como GeoJSON `Point`, na ordem da caminhada — a malha é gerada em serpentina para que
seguir a sequência seja um caminho contínuo pelo talhão.

---

## Geometria

Entra e sai como GeoJSON `Polygon` (RFC 7946), com posições em **`[longitude, latitude]`** — a ordem
do padrão, inversa de como se fala. É a ordem que o Leaflet-Geoman produz, então o frontend entrega o
desenho sem tradução.

Só o anel externo é aceito; polígono com ilha é recusado. O anel é fechado automaticamente quando
chega aberto.

```json
{
  "farmId": "01a07cfe-728d-72e4-8c3e-6e58655b326f",
  "name": "Talhão Norte",
  "boundary": {
    "type": "Polygon",
    "coordinates": [[
      [-55.7211, -12.5453],
      [-55.71189, -12.5453],
      [-55.71189, -12.53631],
      [-55.7211, -12.53631],
      [-55.7211, -12.5453]
    ]]
  }
}
```

A resposta traz `areaHectares`, `perimeterMeters`, `center` e `vertexCount` — **calculados pelo
servidor**. A área nunca é enviada pelo cliente.

Enums viajam como texto (`"Soybean"`, não `1`): legível no contrato e estável quando alguém reordenar
o enum.

---

## Erros

Falhas voltam como `ProblemDetails` (RFC 7807):

```json
{
  "title": "field.overlaps_another",
  "status": 409,
  "detail": "This outline overlaps another field of the same farm."
}
```

O `title` carrega o **código estável** — é o contrato. O `detail` está em inglês e é para quem
depura; o frontend traduz pelo código e nunca o exibe cru. A tradução vive em
`frontend/src/api/apiErrors.ts`.

### Catálogo de erros

| Código | HTTP | Situação |
|---|---|---|
| `geo.invalid_polygon` | 422 | contorno se cruza (o "nó de gravata" ao desenhar) |
| `geo.polygon_has_holes` | 422 | polígono com anel interno |
| `geo.malformed_geojson` | 422 | posição sem os dois números |
| `geo.unsupported_geojson` | 422 | geometria que não é `Polygon` |
| `geo.empty_boundary` | 422 | sem geometria |
| `geo.insufficient_vertices` | 422 | menos de três vértices distintos |
| `geo.too_many_vertices` | 422 | mais de 5.000 vértices |
| `geo.latitude_out_of_range` | 422 | fora de −90..90 |
| `geo.longitude_out_of_range` | 422 | fora de −180..180 |
| `geo.coordinate_not_finite` | 422 | valor não numérico |
| `field.area_below_minimum` | 400 | menos de 0,1 ha |
| `field.area_above_maximum` | 400 | mais de 50.000 ha — normalmente lat/lon trocadas |
| `field.duplicate_name` | 409 | já existe um talhão com esse nome na fazenda |
| `field.overlaps_another` | 409 | o contorno divide área com um vizinho |
| `field.unknown_crop` | 422 | cultura fora do catálogo |
| `field.not_found` | 404 | |
| `farm.has_fields` | 409 | exclusão bloqueada para não levar o histórico junto |
| `farm.invalid_state` | 422 | UF sem duas letras |
| `farm.not_found` | 404 | |
| `validation.failed` | 422 | falha de forma na entrada |
| `sampling.field_is_inactive` | 409 | talhão fora de operação; não se planeja caminhada para ele |
| `sampling.field_too_narrow_for_edge_buffer` | 400 | descartada a bordadura, não sobra miolo para amostrar |
| `sampling.no_points_fit_the_field` | 400 | nenhum ponto da malha cai dentro do contorno |
| `sampling.grid_too_dense` | 400 | o espaçamento pedido passaria de 10.000 pontos |
| `sampling.spacing_out_of_range` | 422 | fora de 10 m..10.000 m |
| `sampling.spacing_not_finite` | 422 | valor não numérico |
| `sampling.unknown_mode` | 422 | modo fora do catálogo |
| `sampling.not_found` | 404 | |
| `geo.latitude_too_close_to_pole` | 422 | conversão métrica indefinida perto do polo |
| `geo.negative_shrink_distance` | 422 | recuo de contorno negativo |

---

## Roteiro de verificação

`backend/src/SmartGrao.WebApi/SmartGrao.WebApi.http` tem o fluxo completo, incluindo os casos que
**devem** falhar. Resultado da última execução contra o Neon:

| Cenário | Esperado | Obtido |
|---|---|---|
| talhão de 1 km² | ~100 ha | 99,9317 ha |
| nó de gravata | 422 | `geo.invalid_polygon` |
| talhão sobreposto | 409 | `field.overlaps_another` |
| vizinho na divisa | 201 | aceito |
| nome repetido (caixa diferente) | 409 | `field.duplicate_name` |
| área minúscula | 400 | `field.area_below_minimum` |
| excluir fazenda com talhões | 409 | `farm.has_fields` |

Amostragem, verificada contra o Neon num talhão real de 99,93 ha:

| Cenário | Esperado | Obtido |
|---|---|---|
| Monitoramento, 99,93 ha | ≥ 10 pontos (alvo da tabela) | 12 pontos, espaçamento 195,3 m, bordadura 158,1 m |
| Mapeamento, grade de 100 m | ~100 pontos | 100 pontos, bordadura 0 m |
| sequência da malha | contínua de 1 a N | ✓ |
| espaçamento junto com `Monitoring` | 422 | `validation.failed` |
| `Mapping` sem espaçamento | 422 | `validation.failed` |
| espaçamento de 2 m | 422 | `validation.failed` |
| talhão inexistente | 404 | `field.not_found` |
| excluir plano | 204, pontos vão junto | ✓ (10.000 pontos removidos em cascata) |

**Um limite a conhecer:** 100 ha com grade de 10 m dá exatamente 10.000 pontos, que é o teto — e portanto é
aceito, não recusado. `sampling.grid_too_dense` só dispara **acima** disso. Um plano assim é válido
mas pesado: são 10.000 linhas em `sampling_points` e uma caminhada que ninguém faz.
