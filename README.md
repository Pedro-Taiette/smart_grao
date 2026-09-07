# SmartGrão

Gestão agrícola com mapeamento georreferenciado de talhões, amostragem proporcional de pragas e
diagnóstico por visão computacional.

| Pilar | O que faz | Situação |
|---|---|---|
| 1 — Mapeamento de talhões | o produtor desenha o contorno no mapa; o sistema valida a geometria e calcula a área em hectares | **pronto** |
| 2 — Amostragem e diagnóstico | malha de pontos proporcional à área; foto no ponto → praga, severidade e confiança | a fazer |
| 3 — Drone | ortomosaico sobreposto ao talhão para varredura de manchas | a fazer |

## Stack

.NET 10 (Clean Architecture + DDD) · PostgreSQL + PostGIS no Neon · EF Core + NetTopologySuite ·
React + TypeScript + MUI · Leaflet + Geoman · cliente da API gerado pelo Orval.

## Rodando

Pré-requisitos: .NET SDK 10, Node 20+, um banco PostgreSQL com PostGIS e
`dotnet tool install --global dotnet-ef --version 10.0.9`.

**1. Banco.** Crie `backend/src/SmartGrao.WebApi/appsettings.Development.json` — ele está no
`.gitignore` porque carrega a senha, então não vem no clone:

```json
{
  "ConnectionStrings": {
    "SmartGrao": "Host=<endpoint>.neon.tech; Database=neondb; Username=<user>; Password=<senha>; SSL Mode=VerifyFull; Channel Binding=Require;"
  }
}
```

No Neon, habilite a extensão uma vez: `CREATE EXTENSION IF NOT EXISTS postgis;`

**2. Esquema.** Migrations são um passo explícito, nunca automáticas na subida:

```powershell
dotnet run --project backend/src/SmartGrao.WebApi -- migrate
```

**3. Backend** em `http://localhost:5148` (documentação interativa em `/scalar/v1`):

```powershell
dotnet run --project backend/src/SmartGrao.WebApi
```

**4. Frontend** em `http://localhost:5173`:

```powershell
cd frontend && npm install && npm run dev
```

## Testes

```powershell
dotnet test backend/SmartGrao.slnx
cd frontend && npm run build
```

## Estrutura

```
backend/     solução .NET (Domain, Application, Infrastructure, WebApi) e testes
frontend/    React + MUI
openapi/     contrato emitido a cada build do backend; é a entrada do Orval
docs/        decisões de arquitetura, regras de domínio e referência da API
```

## Documentação

- [Arquitetura](docs/arquitetura.md) — camadas, decisões e o porquê de cada uma
- [Domínio](docs/dominio.md) — regras do talhão e do plano de amostragem, geodésia e sobreposição
- [Amostragem](docs/amostragem.md) — quantos pontos por talhão, e por quê: protocolo MIP-Soja,
  literatura de densidade amostral e o que os sistemas de mercado fazem
- [API](docs/api.md) — endpoints, GeoJSON e catálogo de erros
- [Frontend](frontend/README.md) — organização e regras de desacoplamento
