# Arquitetura

## Camadas

```
SmartGrao.WebApi          controllers, tratamento global de erro, CORS, injeção de dependência
SmartGrao.Infrastructure  DbContext, mapeamentos EF Core, PostGIS
SmartGrao.Application     casos de uso, DTOs, validação de entrada, contrato de persistência
SmartGrao.Domain          entidades, agregados, objetos de valor, regras
```

A dependência aponta sempre para dentro: `WebApi → Infrastructure → Application → Domain`.
O `Domain` não conhece EF Core, ASP.NET nem PostgreSQL.

---

## Decisões

### 1. O `Domain` referencia o NetTopologySuite

Um polígono georreferenciado é parte da linguagem do domínio, e o NTS é um modelo geométrico puro —
não infraestrutura.

A alternativa seria guardar coordenadas soltas e converter só na borda. Isso obrigaria a
reimplementar validade topológica e, pior, tornaria `ST_Intersects`/`ST_Contains` intraduzíveis pelo
EF Core — empurrando a checagem de sobreposição e toda a amostragem do Pilar 2 para a memória da
aplicação.

### 2. Sem repositórios: `ISmartGraoDbContext` expõe os `DbSet`

Com EF Core, o `DbContext` já *é* a unidade de trabalho e o `DbSet<T>` já é um repositório. Uma
camada de repositórios acima disso quase só encaminha chamadas e obriga cada nova forma de consulta a
virar um método novo na interface — o que doía exatamente na consulta espacial de sobreposição, a
mais interessante do Pilar 1.

A interface fica na Application, não no Domain, porque quem precisa dela são os casos de uso.

### 3. `Field.Geometry` é um `Polygon` mapeado direto, não o objeto de valor

O EF Core traduz `ST_Intersects` e `ST_Contains` para SQL a partir do tipo do NTS. Envolvê-lo num
value converter deixaria o predicado espacial intraduzível.

A validação não se perde: nada entra em `Geometry` sem passar por `Boundary`, que é a fábrica que
valida e mede. Ver [Domínio](dominio.md).

Confirmado contra o banco real — a checagem de sobreposição gera:

```sql
ST_Intersects(f.boundary, @boundary_Polygon)
```

resolvido pelo índice GiST `ix_fields_boundary_gist`.

### 4. A área é calculada no domínio, não no banco

Ver [Domínio → Geodésia](dominio.md#geodésia).

### 5. Um handler por caso de uso, sem mediator

Handlers são registrados por convenção (Scrutor): classes terminadas em `CommandHandler` ou
`QueryHandler`. Cada um é injetado direto na ação do controller que o usa, então "localizar usos"
mostra quem chama e o compilador confere os argumentos — nada disso vale para um
`Send(new AlgumCommand(...))` resolvido por reflexão.

Regras que dependem de *outros* agregados não cabem dentro do agregado nem num handler: viram um
colaborador explícito. É o caso do `FieldPlacementGuard` (nome único e ausência de sobreposição).

### 6. O contrato OpenAPI é um arquivo versionado

O backend emite `openapi/SmartGrao.WebApi.json` a cada build. O frontend gera o cliente a partir
desse arquivo com `npm run api`, não de um servidor no ar.

Consequência: gerar não exige a API rodando, e uma mudança de contrato aparece no diff do pull
request e quebra a compilação do frontend — em vez de estourar no navegador.

---

## Erros

O domínio tem um único canal de falha: `DomainException`, que carrega um `Error` estruturado
(código estável + mensagem de desenvolvedor + tipo). Não há tipo `Result` — uma invariante que nunca
pode produzir um objeto inválido é guardada onde o objeto é construído, para que nenhum chamador
consiga pular a checagem esquecendo de inspecionar um retorno.

O `ErrorType` escolhe o status HTTP na borda:

| ErrorType | HTTP |
|---|---|
| `Validation` | 422 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `BusinessRule` | 400 |
| `Forbidden` | 403 |
| `ExternalService` | 502 |

O `GlobalExceptionHandler` traduz para `ProblemDetails` (RFC 7807). Só os 500 viram log de erro: um
422 por polígono auto-interseccionado é o sistema funcionando, e registrá-lo como erro treina todo
mundo a ignorar o log de erros.

Todo erro é declarado uma vez em `SmartGraoErrors` — código, mensagem e tipo juntos — para que um
ponto de chamada referencie um membro em vez de repetir o par código/mensagem, que sempre diverge.
Ver [API → Catálogo de erros](api.md#catálogo-de-erros).
