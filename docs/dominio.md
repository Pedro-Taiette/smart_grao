# Domínio

## Agregados

### `Farm` — a fazenda

O guarda-chuva sob o qual os talhões existem. Nome, município, UF e uma **sede opcional**
(`geography(Point,4326)`) que serve para o mapa abrir sobre a terra do produtor em vez de sobre o
oceano.

Os talhões **não** ficam dentro deste agregado. Uma fazenda tem dezenas deles, cada um editado
isoladamente no mapa, e carregar a coleção inteira para renomear um só seria pagar caro por uma
invariante que nenhuma regra exige. O talhão referencia a fazenda por id.

### `Field` — o talhão

A unidade de manejo: o pedaço de terra que se planta, se amostra e se pulveriza como um todo. É a
âncora espacial de todo o resto do sistema.

| Campo | Observação |
|---|---|
| `Geometry` | `geography(Polygon,4326)` — o contorno |
| `AreaHectares` | `numeric(12,4)`, gravada junto com o contorno |
| `PerimeterMeters` | idem |
| `Crop` | enum fechado; o Pilar 2 roteia o modelo de visão por ela |
| `Active` | desativar em vez de excluir |

**Área e contorno nunca se separam.** Não existe setter público para `AreaHectares`: ela é reescrita
dentro de `Redraw`, junto com a geometria. Dois campos que podem discordar acabam discordando.

A área é denormalizada de propósito — é o número que aparece em toda lista e todo relatório, e
recalculá-lo a cada leitura cobraria uma ida ao banco por talhão para reproduzir um valor que só muda
quando o contorno muda.

---

### `SamplingPlan` — o plano de amostragem

A malha georreferenciada que o agrônomo caminha num talhão, com o registro de **como ela foi
derivada**. É o agregado central do Pilar 2.

Referencia o talhão por id, sem navegação: a fronteira de consistência é o plano. Um talhão tem
vários planos ao longo da safra — o MIP pede amostragem semanal — e cada um é um documento fechado do
que foi decidido naquele dia.

| Campo | Observação |
|---|---|
| `Mode` | `Monitoring` ou `Mapping`. Texto no banco: é a chave para interpretar o resto da linha |
| `SpacingMeters` | o que foi usado de fato; no Monitoramento, o que a busca achou |
| `EdgeBufferMeters` | bordadura descartada. Zero no Mapeamento — e isso é regra, não ausência |
| `TargetPointCount` | nulo no Mapeamento, onde a pergunta não tem número alvo |
| `FieldAreaHectares` | instantâneo da área no momento da geração |
| `SubdivisionRecommended` | acima de 100 ha o protocolo manda subdividir em vez de amostrar mais |
| `IsOutdated` | o contorno mudou depois desta marcação |

**Por que o instantâneo da área.** `Field.Redraw` existe, e depois de um redesenho os pontos deste
plano podem cair fora do talhão. Guardando a área da geração, o plano sabe dizer que está defasado
sem carregar o talhão junto.

**Quando o contorno muda.** O `FieldRedrawnEvent` é escutado pelo `MarkSamplingPlansOutdatedHandler`,
que marca as malhas daquele talhão como defasadas. Marca — não apaga nem regera. Apagar destruiria o
roteiro de uma caminhada que pode já ter começado; regerar sozinho trocaria em silêncio pontos que
alguém talvez já tenha visitado. O plano fica onde está, dizendo que envelheceu, e quem decide é o
produtor.

Os eventos são entregues **antes do commit, na mesma transação**. Handler de evento aqui existe para
manter o modelo coerente, não para efeito externo como e-mail: entregar depois deixaria uma janela em
que o contorno já mudou e a malha ainda se diz atual — e se a segunda transação falhasse, a janela
viraria permanente.

**Alvo e contagem real ficam lado a lado.** A malha recortada pelo contorno raramente bate o alvo
exato; devolver só o total apagaria a informação de que este talhão ficou acima ou abaixo do que a
fonte pede. `FallsShortOfTarget` cobre o talhão pequeno demais para comportar o protocolo.

`SamplingPoint` é entidade filha, não raiz. Um ponto não existe sem o plano, e a malha só faz sentido
como conjunto — metade dela não mede metade do talhão, mede coisa nenhuma. Não há `DbSet` para ele.

A derivação da densidade e a razão de a bordadura mudar por modo estão em
[amostragem.md](amostragem.md).

---

## `Boundary` — o objeto de valor que guarda a geometria

Todo polígono que entra no sistema passa por `Boundary.FromCoordinates` ou `Boundary.FromPolygon`, e
sai de lá válido ou não sai. Nenhum agregado aceita um `Polygon` cru.

O que ele recusa:

| Situação | Código |
|---|---|
| contorno se cruza (auto-interseção) | `geo.invalid_polygon` |
| polígono com anel interno | `geo.polygon_has_holes` |
| menos de três vértices distintos | `geo.insufficient_vertices` |
| mais de 5.000 vértices | `geo.too_many_vertices` |
| latitude/longitude fora da faixa | `geo.latitude_out_of_range` etc. |

E o que ele conserta sozinho: **fecha o anel** quando o último ponto não repete o primeiro. O
Leaflet-Geoman entrega o contorno aberto, e exigir que o frontend lembre de fechar seria transferir
uma regra de topologia para a tela.

Vértices vindos do banco são revalidados — um polígono construído fora daqui não passou
necessariamente pelo construtor de `GeoCoordinate`.

### `Shrink` — o recuo da bordadura

`Boundary.Shrink(metros)` devolve o contorno recuado para dentro, que é o que o modo Monitoramento
amostra.

O recuo é pedido em metros mas o polígono vive em graus, e um grau de longitude vale menos metros que
um grau de latitude. Erodir direto em graus produziria uma bordadura mais larga em cima e embaixo do
que dos lados — a 28° sul o recuo leste-oeste sai 12% menor que o pedido. Por isso a longitude é
comprimida por `cos(lat)` antes do buffer, e a compressão é desfeita depois: nesse espaço os dois
eixos têm a mesma escala métrica.

Devolve `null`, e não um polígono vazio, quando a erosão não deixa miolo — talhão estreito demais
some, talhão em ampulheta se parte em lobos soltos. Devolver o pedaço maior descartaria parte do
talhão em silêncio e enviesaria a amostragem para um lado. Não lança porque não é violação de
invariante do `Boundary`: quem chama é que sabe se a ausência de miolo é erro.

### Ordem das coordenadas

GeoJSON usa `[longitude, latitude]`. O Leaflet usa `[latitude, longitude]`. `GeoCoordinate.From`
recebe `(latitude, longitude)`, como se fala.

A troca acontece em dois lugares e em nenhum outro: `GeoCoordinate.FromGeoJsonPair` no backend e
`geo/geoJson.ts` no frontend. Inverter por engano não quebra nada visivelmente — o talhão
simplesmente aparece do outro lado do planeta.

---

## Geodésia

`Polygon.Area` do NetTopologySuite devolve **graus quadrados**, unidade que não converte para
hectare: um grau de longitude vale ~111 km na linha do Equador e ~85 km no Rio Grande do Sul. Um
talhão de 40 ha medido pelo caminho errado erra por dezenas de hectares — o suficiente para
dimensionar a pulverização errada.

`Wgs84Geodesy` aplica a fórmula do **excesso esférico** (Chamberlain & Duquette) sobre a esfera
autálica do WGS84 (raio de igual área, 6.371.007,181 m). Contra o elipsoide o erro fica abaixo de
~0,1% em talhões agrícolas — ordens de grandeza melhor do que a incerteza do próprio traçado feito
com o dedo sobre o mapa. É a mesma família de fórmula que o PostGIS aplica em colunas `geography`.

O valor absoluto é tomado no fim porque o sinal só informa a orientação do anel, e o GeoJSON não
garante horário nem anti-horário. Sem isso, metade dos talhões teria área negativa.

**Como isso é verificado.** Os testes conferem o resultado contra a forma fechada da área de uma zona
esférica, `R² · Δλ · (sen φ₂ − sen φ₁)`, calculada de forma independente — caminho de código
diferente, mesmo número. Há também um teste que fixa por escrito que a área planar do NTS *não* serve
como medida, para que ninguém "simplifique" o cálculo trocando um pelo outro.

Contra o banco real, um quadrado de 1 km de lado em Sorriso-MT resultou em **99,9317 ha** e perímetro
de **3.998,63 m**.

### Limites de área

| Limite | Valor | Motivo |
|---|---|---|
| mínimo | 0,1 ha | abaixo disso o desenho é um clique acidental |
| máximo | 50.000 ha | contorno invertido, lat/lon trocadas ou vértice arrastado para o outro hemisfério — falhas que só aparecem como um número absurdo |

---

## Sobreposição de talhões

**Talhões vizinhos podem dividir a divisa.** É o caso normal de uma fazenda. Se encostar contasse
como conflito, nenhuma fazenda cadastraria o segundo talhão.

**Talhões não podem dividir área.** Não é detalhe cosmético: a amostragem do Pilar 2 contaria o mesmo
ponto duas vezes, e a recomendação de aplicação sairia dobrada na faixa sobreposta.

A distinção é feita em dois passos, no `FieldPlacementGuard`:

1. **Triagem grossa no banco** — `ST_Intersects` sobre o índice GiST. Devolve também os vizinhos que
   apenas encostam. Trazer todos os talhões para a memória funcionaria numa fazenda de demonstração e
   degradaria linearmente numa de verdade.
2. **Confirmação fina no domínio** — `Boundary.OverlapsWith`, que usa a matriz `T********` do
   `Relate`: exige interseção entre os *interiores*, ou seja, área em comum de verdade. Essa
   distinção é regra de negócio, não detalhe de consulta.

---

## Desativar em vez de excluir

Um talhão que já recebeu amostragens e diagnósticos carrega o histórico da lavoura. Excluir a linha
levaria junto o registro de que a ferrugem apareceu ali em duas safras seguidas.

Pela mesma razão, excluir uma fazenda que ainda tem talhões é recusado (`farm.has_fields`) em vez de
apagar em cascata: um clique que levasse tudo junto seria irreversível e silencioso.

---

## Nome único por fazenda

Garantido pelo índice `ix_fields_name_per_farm`, não só pela checagem do caso de uso. O caso de uso
checa para devolver um 409 com código traduzível; o índice é a garantia de verdade, a que duas
requisições simultâneas não escapam.

A comparação ignora caixa: "Talhão Norte" e "talhao norte" colidem.
