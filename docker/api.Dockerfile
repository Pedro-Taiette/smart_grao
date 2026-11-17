# API .NET. As fotos e os mapas de calor da demonstracao vem dentro da imagem, em /data/photos: o
# volume nomeado do compose e criado a partir deles na primeira subida (e volta a eles no reset).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ backend/
# O documento OpenAPI e gerado no build de desenvolvimento para o Orval; aqui nao serve para nada.
RUN dotnet publish backend/src/SmartGrao.WebApi -c Release -o /app -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# O driver do Postgres procura a biblioteca do Kerberos ao conectar; sem ela, o log enche de avisos.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
COPY docker/demo-photos/ /data/photos/
ENV ASPNETCORE_URLS=http://+:8080 \
    Storage__PhotoRoot=/data/photos
EXPOSE 8080
# Migrations sao um passo explicito do sistema; no container ele roda antes de subir. Com o banco da
# demonstracao ja migrado, nao faz nada.
ENTRYPOINT ["sh", "-c", "dotnet SmartGrao.WebApi.dll migrate && exec dotnet SmartGrao.WebApi.dll"]
