# SmartGrão

Sistema de gestão agrícola inteligente: mapeamento georreferenciado de talhões, amostragem
proporcional de pragas e diagnóstico por visão computacional.

## Roadmap

| Fase | Entrega | Situação |
|---|---|---|
| 1 | Fundação Clean Architecture + mapeamento de talhões (Pilar 1) | **concluída** |
| 2 | Frontend React + Leaflet/Geoman para desenho dos talhões (Pilar 1) | **concluída** |
| 3 | Amostragem georreferenciada e malha de pontos (Pilar 2) | a fazer |
| 4 | Diagnóstico por IA e PoC de ortomosaico de drone (Pilares 2 e 3) | a fazer |

## Arquitetura

```
backend/src/
  SmartGrao.Domain          entidades, agregados, objetos de valor, regras — sem framework
  SmartGrao.Application     casos de uso, DTOs, validação de entrada, contrato de persistência
  SmartGrao.Infrastructure  EF Core + PostGIS
  SmartGrao.WebApi          controllers, tratamento de erro, CORS, DI
backend/tests/
  SmartGrao.Domain.Tests    geodésia, topologia e invariantes do domínio
openapi/                    contrato emitido a cada build do backend
frontend/                   React + MUI; cliente da API gerado pelo Orval (ver frontend/README.md)
```

O `openapi/SmartGrao.WebApi.json` é o contrato entre as duas metades: o backend o emite no build e o
frontend gera o cliente a partir dele com `npm run api`. Uma mudança incompatível aparece no diff do
pull request e quebra a compilação do frontend, em vez de estourar no navegador.

A dependência aponta sempre para dentro: `WebApi → Infrastructure → Application → Domain`.
O `Domain` não conhece EF Core, ASP.NET nem PostgreSQL.

### Convenções

Identificadores em inglês. Comentários em português, porque são para quem vai ler e manter o
projeto. Os termos do negócio aparecem traduzidos no código (`Field` para talhão, `Farm` para
fazenda, `Crop` para cultura), mas os comentários e esta documentação usam o vocabulário do
produtor.

### Três decisões que valem explicação

**Sem repositórios: `ISmartGraoDbContext` expõe os `DbSet`.** Com EF Core, o `DbContext` já *é* a
unidade de trabalho e o `DbSet<T>` já é um repositório. Uma camada de repositórios acima disso quase
só encaminha chamadas e obriga cada nova forma de consulta a virar um método novo na interface — o
que dói exatamente na consulta espacial de sobreposição, a mais interessante do Pilar 1. A
interface fica na Application, não no Domain, porque quem precisa dela são os casos de uso.

**O `Domain` referencia o NetTopologySuite.** Um polígono georreferenciado é parte da linguagem do
domínio, e a biblioteca é um modelo geométrico puro — não infraestrutura. A alternativa, guardar
coordenadas soltas e converter só na borda, obrigaria a reimplementar validade topológica e, pior,
tornaria `ST_Intersects`/`ST_Contains` intraduzíveis pelo EF Core, empurrando a checagem de
sobreposição e toda a amostragem da Fase 3 para a memória da aplicação.

**A área é calculada no domínio, não no banco.** `Polygon.Area` do NetTopologySuite devolve graus
quadrados, unidade que não converte para hectare porque um grau de longitude vale ~111 km no
Equador e ~85 km no Sul. `Wgs84Geodesy` aplica a fórmula do excesso esférico sobre o raio autálico
do WGS84 — mesma família de fórmula que o PostGIS usa em colunas `geography`, com erro abaixo de
0,1% em talhões agrícolas. Os testes conferem o resultado contra a forma fechada da área de uma
zona esférica, calculada de maneira independente.

## Pré-requisitos

- .NET SDK 10
- PostgreSQL com PostGIS (o projeto tem como alvo o Neon)
- `dotnet tool install --global dotnet-ef --version 10.0.9`

## Configuração do banco

A string de conexão fica em `backend/src/SmartGrao.WebApi/appsettings.Development.json`.

Esse arquivo **está no `.gitignore`** — ele carrega a senha do banco e por isso não é versionado.
Ao clonar o repositório ele não vem junto; crie-o com este conteúdo:

```json
{
  "ConnectionStrings": {
    "SmartGrao": "Host=<endpoint>.neon.tech; Database=neondb; Username=<user>; Password=<senha>; SSL Mode=VerifyFull; Channel Binding=Require;"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft.EntityFrameworkCore.Database.Command": "Information" }
    }
  }
}
```

Em produção a string vem da variável de ambiente `ConnectionStrings__SmartGrao`, que tem precedência
sobre o arquivo — nenhum segredo de produção mora no repositório.

No Neon, habilite a extensão uma vez por banco (a migration também a declara):

```sql
CREATE EXTENSION IF NOT EXISTS postgis;
```

Aplique o esquema:

```powershell
cd backend/src/SmartGrao.WebApi
dotnet run -- migrate
```

Migrations são um passo explícito, nunca automáticas na subida da aplicação.

## Executando

```powershell
dotnet run --project backend/src/SmartGrao.WebApi
```

A documentação interativa fica em `/scalar/v1`; o documento OpenAPI em `/openapi/v1.json`.
O arquivo `backend/src/SmartGrao.WebApi/SmartGrao.WebApi.http` tem o roteiro completo, incluindo os
casos que **devem** falhar (polígono auto-interseccionado, talhão sobreposto, nome repetido).

## Testes

```powershell
dotnet test backend/SmartGrao.slnx
```

## A API do Pilar 1

| Verbo | Rota | O que faz |
|---|---|---|
| GET | `/api/farms` | lista as fazendas |
| POST | `/api/farms` | cadastra, com sede opcional para centralizar o mapa |
| GET | `/api/farms/{id}` | detalha |
| PUT | `/api/farms/{id}` | atualiza |
| DELETE | `/api/farms/{id}` | exclui; recusa (409) se ainda houver talhões |
| GET | `/api/farms/{id}/fields` | talhões com contorno em GeoJSON — é o que o mapa consome |
| POST | `/api/fields` | cria a partir do polígono desenhado |
| GET | `/api/fields/{id}` | detalha |
| PUT | `/api/fields/{id}` | renomeia, troca a cultura e redesenha o contorno |
| PATCH | `/api/fields/{id}/status?active=` | ativa/desativa preservando o histórico |
| DELETE | `/api/fields/{id}` | exclui |

Geometrias entram e saem como GeoJSON `Polygon` (RFC 7946), com posições em
`[longitude, latitude]` — a ordem do padrão, inversa de como se fala. É a ordem que o
Leaflet-Geoman produz, então o frontend entrega o desenho sem tradução.

### Erros

As falhas voltam como `ProblemDetails` (RFC 7807), com o código estável em `title` e a mensagem
técnica em `detail`. O frontend traduz pelo código e nunca exibe o `detail` cru.

| Código | Status | Situação |
|---|---|---|
| `geo.invalid_polygon` | 422 | contorno se cruza (o "nó de gravata" ao desenhar) |
| `geo.polygon_has_holes` | 422 | polígono com anel interno |
| `geo.malformed_geojson` | 422 | posição sem os dois números |
| `field.area_below_minimum` | 400 | menos de 0,1 ha |
| `field.area_above_maximum` | 400 | mais de 50.000 ha — normalmente lat/lon trocadas |
| `field.duplicate_name` | 409 | já existe um talhão com esse nome na fazenda |
| `field.overlaps_another` | 409 | o contorno divide área com um vizinho |
| `farm.has_fields` | 409 | exclusão bloqueada para não levar o histórico junto |

## Regras de domínio já implementadas

- Polígono topologicamente válido, sem auto-interseção e sem ilhas.
- Anel fechado automaticamente quando o desenho chega aberto.
- Coordenadas dentro das faixas de latitude e longitude, revalidadas mesmo vindas do banco.
- Área entre 0,1 ha e 50.000 ha; área e perímetro nascem junto com o contorno e nunca divergem
  dele — não existe setter público para a área.
- Nome único por fazenda, garantido por índice, não só pela checagem do caso de uso.
- Talhões vizinhos podem dividir a divisa; sobreposição de área é recusada. A triagem grossa roda
  no banco pelo índice GiST e a confirmação fina fica no domínio (`FieldPlacementGuard`).
- Desativar em vez de excluir, para que a área saia da operação sem levar embora o histórico de
  diagnóstico da lavoura.
