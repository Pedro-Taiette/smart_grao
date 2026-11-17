# PostGIS com a copia da demonstracao. O script roda so na primeira subida do volume; o reset
# (demo-reset.cmd) apaga o volume e a proxima subida volta a este estado.
FROM postgis/postgis:18-3.6
# A copia ja cria as extensoes (postgis, btree_gist...) e os esquemas delas; o script padrao da
# imagem tentaria criar os mesmos esquemas antes e a carga falharia em "already exists".
RUN rm -f /docker-entrypoint-initdb.d/10_postgis.sh
# A pasta inteira: a copia (20-smartgrao-demo.sql.gz) fica fora do git e vem no pacote da demo.
# Sem ela, o banco sobe vazio e as migrations da API criam o esquema.
COPY docker/db/ /docker-entrypoint-initdb.d/
