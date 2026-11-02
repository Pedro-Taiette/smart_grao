# Fase 6 — Anomalia por satélite (camada 2)

O sistema aponta onde, dentro do talhão, a lavoura destoa. Ver o [roadmap](roadmap-cercosporiose.md)
e a [pesquisa](pesquisa-cercosporiose.md#4-satélite).

**Critério:** um talhão real mostra a série de NDRE de uma safra passada e as zonas anômalas
desenhadas no mapa, com as passagens nubladas indicadas.

## O que a camada faz — e o que não faz

Nenhum estudo usou Sentinel-2 real para cercosporiose, e a evidência em folha começa em 21% de área
lesionada. A 20 m por pixel o satélite vê a lavoura perdendo vigor, não lesões. Por isso a zona é
sempre **"área que destoa do resto do talhão"**, nunca "cercosporiose detectada", e só serve para
dizer **onde olhar**.

## Serviço Python

`analytics/service/` — FastAPI, sem estado. Recebe o contorno e o período, devolve as passagens.

| Etapa | Decisão | Origem |
|---|---|---|
| Catálogo | Earth Search (Element 84), coleção `sentinel-2-c1-l2a`, COG, sem conta | Pesquisa, seção 4 |
| Grade | Um tile MGRS por talhão (o mais frequente), uma passagem por data | Para as passagens caírem na mesma grade |
| Refletância | DN × escala + offset dos metadados (`raster:bands`; −0,1 na c1) | Metadado da coleção |
| Nuvem | SCL 0, 1, 3, 8, 9, 10 mascarados; passagem com menos de 60% do talhão limpo vira **sem leitura** | Máscara mínima: pesquisa; 60%: **decisão de projeto** |
| Resolução | Tudo a 20 m, a grade da SCL; B04 e B08 agregados pela média | Simplifica a mistura de resoluções |
| NDVI | (B08 − B04) / (B08 + B04) | |
| NDRE | (B8A − B05) / (B8A + B05) | Duas bandas de 20 m; escolha registrada na pesquisa |
| Dossel | Zonas só com NDVI mediano ≥ 0,4 | **Decisão de projeto** — antes disso, o que destoa é solo |
| Zona | Pixels com NDRE ≥ 15% abaixo da mediana do próprio talhão, agrupados (8 vizinhos), área ≥ 0,5 ha, recortados pelo contorno, buracos preenchidos | **Decisão de projeto** |
| Queda | A zona é marcada se o NDRE dela caiu ≥ 0,03 desde a passagem anterior na mesma grade | **Decisão de projeto** |

```powershell
cd analytics/service
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
.venv/Scripts/python -m uvicorn smartgrao_analytics.app:app --port 8000
.venv/Scripts/python -m pytest -q tests
```

`POST /satellite/passes` com `{ polygon, start, end }`. Uma safra inteira leva de 1 a 2 minutos,
porque cada passagem lê a SCL e, se o talhão estiver limpo, quatro bandas.

## Backend

- **`SatellitePass`** — uma passagem por talhão e data: cena, fração limpa, situação (`Ok` ou
  `NoReading`), estatísticas de NDVI e NDRE (média, mediana, p10, p90) e as zonas
  (`AnomalyZone`, `geography(Polygon)`, como o contorno).
- A atualização recomeça **na última passagem lida**, para que a primeira passagem nova tenha com
  quem se comparar. `?full=true` reprocessa a safra inteira — útil quando a regra muda.
- A API chama o serviço por `Analytics:BaseUrl` (padrão `http://localhost:8000/`), com até 10 minutos
  de espera. Serviço fora do ar vira `risk.analytics_unavailable` (502).

| Método | Rota | Uso |
|---|---|---|
| GET | `/api/cultivations/{id}/satellite` | Passagens do ciclo, com estatísticas e zonas |
| POST | `/api/cultivations/{id}/satellite/refresh?full=` | Busca as passagens que faltam |

## Tela

Na aba **Risco de cercosporiose**, abaixo do risco climático, o painel **Onde o talhão destoa**:

- quantas passagens houve, quantas com leitura e quantas sem leitura por nuvem;
- o NDRE mediano do talhão em cada passagem, com as passagens sem leitura como traços cinza;
- o mapa sobre imagem de satélite, com o contorno e as zonas da passagem escolhida (as que também
  caíram desde a passagem anterior aparecem mais fortes).

## Banco e migração

Migrations: `SatellitePasses` e `AnomalyZoneIndex`. **Aplicadas** no ambiente configurado. Criam
`satellite_passes` (único por talhão e data) e `anomaly_zones`, com exclusão em cascata. A segunda
corrige a coluna `index` da zona, que o EF tinha criado como identidade.

## Verificação

- `pytest`: 5 testes da regra de zona (talhão uniforme, mancha grande, mancha pequena, pixels
  mascarados, solo exposto).
- **Talhão Demonstração (Fazenda Santa Rita, Sorriso-MT)**, desenhado sobre uma lavoura real da
  imagem, com a safrinha 2026 de 10/02 a 01/07. Foram 39 passagens: 18 com leitura e 21 sem leitura,
  quase todas entre fevereiro e março. O NDVI mediano vai de 0,12 no plantio a 0,91 em abril e cai
  na senescência de junho, a curva esperada do milho.
- O "Talhão Leste" da mesma fazenda foi desenhado sobre a área urbana de Sorriso. Ali as zonas caem
  em ruas e quarteirões: o algoritmo faz o que deve, mas o talhão não serve para demonstração.

## Limites conhecidos

- Névoa fina que a SCL não marca derruba o NDVI de uma passagem inteira (12/04 no talhão de
  demonstração). Mascarar também a classe 7 da SCL é a próxima coisa a testar.
- A busca é síncrona. Com muitos talhões, ela deve ir para a fila da fase 8.
