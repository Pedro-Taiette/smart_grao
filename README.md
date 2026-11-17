# SmartGrão

Gestão agrícola com mapeamento georreferenciado de talhões, amostragem proporcional de pragas e
diagnóstico por visão computacional.

| Pilar | O que faz | Situação |
|---|---|---|
| 1 — Mapeamento de talhões | o produtor desenha o contorno no mapa; o sistema valida a geometria e calcula a área em hectares | **pronto** |
| Contexto agrícola — fase 1 | safras, cultivos por talhão, plantio, encerramento e estágios observados | implementado; validação funcional pendente |
| 2 — Amostragem e diagnóstico | planos vinculados ao cultivo; coleta de fotos e IA nas próximas fases | malha implementada; coleta e IA a fazer |
| 3 — Drone | ortomosaico sobreposto ao talhão para varredura de manchas | a fazer |

## Stack

.NET 10 (Clean Architecture + DDD) · PostgreSQL + PostGIS no Neon · EF Core + NetTopologySuite ·
React + TypeScript + MUI · Leaflet + Geoman · cliente da API gerado pelo Orval.

## Rodando

Pré-requisitos: .NET SDK 10, Node 20+, Python 3.13, [Git LFS](https://git-lfs.com), um banco
PostgreSQL com PostGIS e `dotnet tool install --global dotnet-ef --version 10.0.9`.

**0. Clone.** Os modelos de visão (`analytics/models/<versão>/model.onnx`) ficam no Git LFS. Ative o
LFS uma vez por máquina antes de clonar:

```powershell
git lfs install
git clone https://github.com/Pedro-Taiette/smart_grao
```

Se o clone foi feito sem o LFS, o `.onnx` chega como um arquivo-texto de poucos bytes e o serviço não
carrega o modelo. Corrija com `git lfs install` e `git lfs pull`.

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

**5. Serviço de análise** (satélite e modelo de imagem) em `http://localhost:8000`:

```powershell
cd analytics/service
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
.venv/Scripts/python -m uvicorn smartgrao_analytics.app:app --port 8000
```

Sem ele, clima e vistoria funcionam; o satélite não atualiza e a análise das fotos falha depois das
retentativas (dá para reenfileirar com o serviço de pé).

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

- [Roteiro da demonstração](docs/roteiro-demonstracao.md) — a defesa passo a passo, com os dados de demonstração
- [Roadmap — cercosporiose no milho](docs/roadmap-cercosporiose.md) — fases 4 a 9 do semestre:
  risco climático, satélite, vistoria dirigida e IA de imagem, com o calendário até a entrega
- [Pesquisa — cercosporiose](docs/pesquisa-cercosporiose.md) — amostragem, índice de risco, espécie,
  Sentinel-2 e conjuntos de imagens, com fontes
- [Fase 4 — Recorte milho + cercosporiose](docs/fase-4-recorte.md) — fatores de risco, protocolo
  semeado e telas só de milho
- [Fase 5 — Risco climático](docs/fase-5-risco-climatico.md) — índice de favorabilidade, clima do
  Open-Meteo e vistoria sugerida
- [Fase 6 — Satélite](docs/fase-6-satelite.md) — Sentinel-2, NDRE e áreas que destoam no talhão
- [Fase 7 — Vistoria dirigida e fotos](docs/fase-7-vistoria-dirigida.md) — 10 paradas nas áreas do
  satélite, fotos da folha com id do celular
- [Fase 8 — IA de imagem](docs/fase-8-ia-imagem.md) — fila de análises, modelo ONNX e mapa de calor
- [Fase 9 — Aferimento](docs/fase-9-aferimento.md) — revisão humana, métricas e antecedência
- [Fase 1 — Safras e cultivos](docs/fase-1-cultivos.md) — modelo, migration, API e roteiro de verificação.
- [Arquitetura](docs/arquitetura.md) — camadas, decisões e o porquê de cada uma
- [Domínio](docs/dominio.md) — regras do talhão e do plano de amostragem, geodésia e sobreposição
- [Amostragem](docs/amostragem.md) — quantos pontos por talhão, e por quê: protocolo MIP-Soja,
  literatura de densidade amostral e o que os sistemas de mercado fazem
- [API](docs/api.md) — endpoints, GeoJSON e catálogo de erros
- [Frontend](frontend/README.md) — organização e regras de desacoplamento
