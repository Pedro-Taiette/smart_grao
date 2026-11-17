# SmartGrão em Docker

O jeito mais simples de rodar a demonstração em qualquer computador: só precisa do
[Docker Desktop](https://www.docker.com/products/docker-desktop). Não precisa de .NET, Node,
Python, Git LFS nem da senha do banco.

| Atalho | O que faz |
|---|---|
| **`demo.cmd`** | Sobe tudo e abre o navegador em `http://localhost:8080`. A primeira vez constrói as imagens (alguns minutos); depois, uns 20 segundos |
| **`demo-stop.cmd`** | Desliga, guardando o que foi feito na demo |
| **`demo-reset.cmd`** | Volta a demo ao estado inicial — apaga o que foi feito nela (rover rodado, fotos revisadas, vistorias) |

Sem Windows: `docker compose up -d --build`, e `docker compose down -v` para o reset.

## O que sobe

| Container | O que é |
|---|---|
| `db` | PostGIS 18 com uma **cópia congelada dos dados da demonstração** |
| `api` | A API .NET, com as fotos e os mapas de calor da demo dentro da imagem |
| `analytics` | O serviço Python: satélite (Sentinel-2) e o modelo de imagem (ONNX) |
| `web` | O frontend num nginx, que encaminha `/api` para a API |

Tudo fica num endereço só (`:8080`). Por isso **um celular na mesma rede Wi-Fi** abre o sistema
pelo IP do computador — o `demo.cmd` mostra o endereço. No celular, o GPS não liga sem HTTPS (regra
do navegador); a vistoria oferece "Usar a posição planejada" e segue normal. Se o celular não
conectar, libere a porta 8080 no firewall do Windows.

## Os dados da demonstração

**Ficam fora do repositório**, porque têm nomes reais da equipe e o repositório é público. Vêm num
arquivo à parte, **`smartgrao-demo-data.zip`** (peça a quem tem). Coloque o zip na pasta do projeto,
ao lado do `demo.cmd`, e rode o `demo.cmd`: ele descompacta sozinho. Sem o zip, o `demo.cmd` avisa o
que falta.

O zip contém `docker/db/20-smartgrao-demo.sql.gz` (o banco) e `docker/demo-photos/` (fotos e mapas
de calor); os dois estão no `.gitignore`.

O banco **não é o Neon compartilhado**: é uma cópia local, então ninguém atrapalha a demo de ninguém,
e o reset devolve sempre o mesmo ponto de partida.

- **Fazenda Santa Rita (Marília) → Talhão Sul**: risco moderado, 2 áreas do satélite, fotos de
  campo e da estação simulada, uma vistoria em andamento. É onde o rover roda.
- **Fazenda Santa Rita (Sorriso) → Talhao Demonstracao**: lavoura real; o plantio atual com risco
  alto ("Vistorie agora") e a safrinha 2026 encerrada, com 39 passagens do Sentinel-2.

Na cópia ficaram de fora as vistorias e fazendas de teste e as fotos de rodadas anteriores do rover.

**Ainda precisa de internet** para o fundo dos mapas, para o clima atualizar (Open-Meteo) e para o
satélite buscar passagens novas. Sem internet, o que já está na cópia continua aparecendo.

## Atualizar a cópia

`docker/db/20-smartgrao-demo.sql.gz` (o banco) e `docker/demo-photos/` (as fotos e os mapas de
calor) foram gerados a partir do Neon em 17/11/2026. Para refazer com dados novos (Git Bash, com o
Docker ligado e as credenciais do Neon em `neon.env` no formato `PGHOST=`, `PGDATABASE=`, `PGUSER=`,
`PGPASSWORD=`, `PGSSLMODE=require`):

```bash
export MSYS_NO_PATHCONV=1
# 1. Copia o Neon (só leitura) para um Postgres temporário
docker run --rm --env-file neon.env postgres:18-alpine pg_dump --no-owner --no-privileges --no-comments > neon.sql
docker run -d --name sg-seed -e POSTGRES_USER=smartgrao -e POSTGRES_PASSWORD=smartgrao -e POSTGRES_DB=smartgrao postgis/postgis:18-3.6
docker exec -i sg-seed psql -q -U smartgrao -d smartgrao < neon.sql
# 2. Limpa os dados de teste (revise os ids em docker/curate-demo.sql antes)
docker cp docker/curate-demo.sql sg-seed:/tmp/curate.sql
docker exec sg-seed psql -U smartgrao -d smartgrao -f /tmp/curate.sql
# 3. Novo dump da demo
docker exec sg-seed pg_dump --no-owner --no-privileges --no-comments -U smartgrao -d smartgrao | gzip -9 > docker/db/20-smartgrao-demo.sql.gz
docker rm -f sg-seed
```

As fotos vêm da pasta de fotos da API de quem as enviou (`bin/.../data/photos`): copie para
`docker/demo-photos/` as que ainda existem no banco (`select storage_key from photos` e
`select heatmap_key from assessments`). Depois, refaça o pacote:

```powershell
tar -a -c -f smartgrao-demo-data.zip docker/db/20-smartgrao-demo.sql.gz docker/demo-photos
```

Rode **no PowerShell**, da raiz do projeto: o zip precisa guardar os caminhos `docker/db/...` e
`docker/demo-photos/...`. O `Compress-Archive` não guarda, e o `tar` do Git Bash não gera zip — o
certo é o `tar` do próprio Windows (10 e 11).

## Arquivos

```
docker-compose.yml          os quatro serviços
docker/db.Dockerfile        PostGIS + a cópia da demo
docker/api.Dockerfile       .NET 10: build e runtime, com as fotos da demo
docker/analytics.Dockerfile Python 3.13 + modelo ONNX
docker/web.Dockerfile       build do Vite + nginx
docker/nginx.conf           SPA e proxy de /api (timeout de 5 min para a busca do satélite)
scripts/demo.ps1            o que os atalhos demo*.cmd chamam
```
