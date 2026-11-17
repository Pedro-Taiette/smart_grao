# SmartGrão em Docker

O jeito mais simples de rodar a demonstração em qualquer computador Windows: só precisa do
[Docker Desktop](https://www.docker.com/products/docker-desktop). Não precisa de .NET, Node,
Python, Git LFS nem da senha do banco.

**Dois cliques em `demo.cmd`.** Ele liga o Docker se estiver desligado, sobe tudo e abre o
navegador em `http://localhost:8080`. A primeira vez constrói as imagens (alguns minutos); depois,
uns 20 segundos. **Enter na janela do `demo.cmd` desliga** — o que foi feito na demo fica guardado
para a próxima vez.

Para começar do zero (apaga o que foi feito na demo e volta ao starter pack), no terminal da pasta
do projeto:

```
demo.cmd reset
```

Sem Windows: `docker compose up -d --build`, e `docker compose down -v` para o reset.

## O que sobe

| Container | O que é |
|---|---|
| `db` | PostGIS 18, banco local e vazio na primeira vez |
| `api` | A API .NET. Na subida, aplica as migrations (esquema, catálogo de pragas e doenças, protocolo de cercosporiose) |
| `seed` | Roda o **starter pack** (`docker/starter-pack.sql`) depois que a API subiu, e termina |
| `analytics` | O serviço Python: satélite (Sentinel-2) e o modelo de imagem (ONNX) |
| `web` | O frontend num nginx, que encaminha `/api` para a API |

O banco **não é o Neon compartilhado**: é local, então ninguém atrapalha a demo de ninguém.

Tudo fica num endereço só (`:8080`). Por isso **um celular na mesma rede Wi-Fi** abre o sistema
pelo IP do computador — o `demo.cmd` mostra o endereço. No celular, o GPS não liga sem HTTPS (regra
do navegador); a vistoria oferece "Usar a posição planejada" e segue normal. Se o celular não
conectar, libere a porta 8080 no firewall do Windows.

## O starter pack

`docker/starter-pack.sql`, escrito à mão. Pessoas e fazendas são fictícias; os contornos são de
lavouras reais vistas na imagem de satélite, para o Sentinel-2 ter o que ler. Pode rodar quantas
vezes quiser: cada linha tem id fixo e não duplica.

- **Sítio Santa Rita** (Marília-SP), com Ana Souza (técnica) e Carlos Lima (agrônomo) →
  **Talhão Sul**: milho plantado há uns 2 meses, dados do plantio em "Não sei". É onde o rover roda.
- **Fazenda Boa Esperança** (Sorriso-MT), com João Pereira (produtor) → **Talhão da Sede**: milho
  plantado há uns 2 meses, híbrido suscetível, milho sobre milho, plantio direto — risco alto. Tem
  também a **safrinha 2026 encerrada**, para mostrar a curva do satélite de uma safra inteira.

As datas dos plantios atuais são relativas ao dia em que o banco é criado: o pacote não envelhece.

**Clima e satélite não estão no pacote, de propósito**: o sistema busca os dois sozinho (pilares 1
e 2) — o clima na subida e ao abrir o talhão, o satélite ao abrir o talhão. Por isso a demo
**precisa de internet** (também para o fundo dos mapas). Fotos também não: o rover gera na demo.

Para mudar o pacote, edite o `.sql` e rode `demo.cmd reset`.

## Arquivos

```
demo.cmd                    o atalho (chama scripts/demo.ps1)
docker-compose.yml          os serviços
docker/starter-pack.sql     os dados iniciais da demo
docker/api.Dockerfile       .NET 10: build e runtime
docker/analytics.Dockerfile Python 3.13 + modelo ONNX
docker/web.Dockerfile       build do Vite + nginx
docker/nginx.conf           SPA e proxy de /api (timeout de 5 min para a busca do satélite)
```
