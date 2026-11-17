# Servico de analise: satelite (Sentinel-2) e o modelo de imagem (ONNX). O modelo vai junto.
FROM python:3.13-slim
# O rasterio (leitura das imagens do Sentinel-2) precisa da libexpat, que a imagem slim nao traz.
RUN apt-get update && apt-get install -y --no-install-recommends libexpat1 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY analytics/service/requirements.txt service/requirements.txt
RUN pip install --no-cache-dir -r service/requirements.txt
COPY analytics/service/ service/
COPY analytics/models/ models/
ENV SMARTGRAO_MODELS_DIR=/app/models \
    PYTHONUNBUFFERED=1
WORKDIR /app/service
EXPOSE 8000
CMD ["uvicorn", "smartgrao_analytics.app:app", "--host", "0.0.0.0", "--port", "8000"]
