# Frontend estatico num nginx, que tambem encaminha /api para a API. Mesmo endereco para tudo: sem
# CORS e, de quebra, um celular na mesma rede abre o sistema pelo IP do computador.
FROM node:24-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
# Endereco da API vazio: as chamadas saem relativas (/api/...) e o nginx encaminha.
RUN printf 'VITE_API_BASE_URL=\n' > .env.production.local && npm run build

FROM nginx:1.29-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
