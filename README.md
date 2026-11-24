# SmartGrão

Acompanhamento do milho para **detectar cedo a cercosporiose**, em três pilares:

| Pilar | Pergunta | Como |
|---|---|---|
| **1 · Clima** | Quando olhar? | Índice de favorabilidade com o clima do Open-Meteo |
| **2 · Satélite** | Onde olhar? | Áreas do talhão com NDRE abaixo do resto, no Sentinel-2 |
| **3 · Fotos + IA** | O que é? | Modelo de imagem nas fotos das folhas, com revisão humana |

O cruzamento dos três vira uma decisão na tela ("Vistorie agora as 2 áreas que o satélite marcou").
**Coleta autônoma:** um rover (simulado, sem hardware) vai até as áreas do satélite, fotografa as
folhas de baixo e manda para a IA — sem depender de alguém ir a campo. O drone segue em
desenvolvimento. Ver [docs/modulo-coleta-autonoma.md](docs/modulo-coleta-autonoma.md).

## Para começar

**Só para a demonstração (qualquer PC com Windows):**

1. Instale o [Docker Desktop](https://www.docker.com/products/docker-desktop) e baixe o projeto
   (`git clone` ou o ZIP do GitHub).
2. Dois cliques em **`demo.cmd`**. Na primeira vez ele monta as imagens (alguns minutos), cria o
   banco e roda o **starter pack** (`docker/starter-pack.sql`: fazendas, talhões reais, pessoas e
   plantios de milho) sozinho. Abre `http://localhost:8080`.
3. Abra uma vez cada plantio: o clima e o satélite são buscados ao vivo e a primeira busca leva de 1
   a 2 minutos.

Daí em diante, basta o **play do grupo `smartgrao` no Docker Desktop** e abrir
`http://localhost:8080`; os dados ficam guardados. `demo.cmd reset` apaga tudo e roda o starter
pack de novo. Ver [docs/docker.md](docs/docker.md).

**Para desenvolver**, no Windows, com .NET 10, Node.js, Python 3.13 e Git LFS:

1. Dois cliques em **`setup.cmd`** — prepara tudo (só precisa uma vez, ou depois de um `git pull`).
2. Dois cliques em **`start.cmd`** — abre o sistema no navegador. Fechar a janela desliga.

Detalhes, problemas comuns e o jeito manual (um terminal por parte) em
[docs/rodando.md](docs/rodando.md). Como usar a tela, em linguagem de produtor, em
[docs/guia-rapido.md](docs/guia-rapido.md).

## Stack

.NET 10 (Clean Architecture + DDD) · PostgreSQL + PostGIS no Neon · EF Core + NetTopologySuite ·
React + TypeScript + MUI · Leaflet + Geoman · cliente da API gerado pelo Orval · serviço de análise
em Python (FastAPI + ONNX Runtime).

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
analytics/   treino do modelo de imagem e o serviço de análise (satélite + IA)
scripts/     setup, start, stop e demo — chamados pelos .cmd da raiz
docker/      imagens da demo (API, análise, frontend), o nginx e o starter pack (.sql)
docs/        decisões de arquitetura, regras de domínio e referência da API
```

## Documentação

- [Docker](docs/docker.md) — a demo inteira em containers, com o starter pack e o reset
- [Rodando](docs/rodando.md) — os scripts de um clique, problemas comuns e o passo a passo manual
- [Guia rápido](docs/guia-rapido.md) — como usar o sistema, do talhão à foto revisada
- [Coleta autônoma — rover e drone](docs/modulo-coleta-autonoma.md) — papel de cada um, o rover
  simulado (o que é real e o que é simulado) e os próximos passos
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
