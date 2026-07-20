# Сборка .NET приложения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FeedbackAnalysis.ReviewsHandlerApi/FeedbackAnalysis.ReviewsHandlerApi.csproj", "FeedbackAnalysis.ReviewsHandlerApi/"]
RUN dotnet restore "./FeedbackAnalysis.ReviewsHandlerApi/FeedbackAnalysis.ReviewsHandlerApi.csproj"
COPY . .
WORKDIR "/src/FeedbackAnalysis.ReviewsHandlerApi"
RUN dotnet build "./FeedbackAnalysis.ReviewsHandlerApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикация .NET приложения
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FeedbackAnalysis.ReviewsHandlerApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ: .NET runtime + Python с виртуальным окружением
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Полное удаление и переустановка Python
RUN apt-get update && \
    apt-get remove -y python3 python3-pip || true && \
    apt-get autoremove -y && \
    apt-get install -y --no-install-recommends \
    wget \
    build-essential \
    libssl-dev \
    libffi-dev \
    libopenblas-dev \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Установка Python из официального репозитория с перезаписью
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    python3-dev \
    && rm -rf /var/lib/apt/lists/*

# Проверка и создание корректных симлинков
RUN ln -sf /usr/bin/python3 /usr/bin/python
RUN python3 --version

# Копируем и устанавливаем Python-зависимости (устанавливаются в venv)
COPY FeedbackAnalysis.ReviewsHandlerApi/requirements.txt .
#RUN pip install --no-cache-dir -r requirements.txt
RUN python3 -m pip install --upgrade pip || true
RUN python3 -m pip install --no-cache-dir --break-system-packages -r requirements.txt || \
    python3 -m pip install --no-cache-dir --ignore-installed -r requirements.txt || \
    python3 -m pip install --no-cache-dir --user -r requirements.txt

# Устанавливаем huggingface-hub с CLI-инструментом
#RUN python3 -m pip install --no-cache-dir huggingface-hub

# Скачиваем модель в папку /model
RUN hf download hf://blanchefort/rubert-base-cased-sentiment-rurewiews/model.safetensors \
    --local-dir /app/model && \
    hf download hf://blanchefort/rubert-base-cased-sentiment-rurewiews/config.json \
    --local-dir /app/model && \
    hf download hf://blanchefort/rubert-base-cased-sentiment-rurewiews/special_tokens_map.json \
    --local-dir /app/model && \
    hf download hf://blanchefort/rubert-base-cased-sentiment-rurewiews/tokenizer_config.json \
    --local-dir /app/model && \
    hf download hf://blanchefort/rubert-base-cased-sentiment-rurewiews/vocab.txt \
    --local-dir /app/model

# Копируем Python-скрипт
COPY FeedbackAnalysis.ReviewsHandlerApi/reviews_predicate.py .

# Копируем опубликованное .NET приложение
COPY --from=publish /app/publish .

# Копируем скрипт запуска и делаем его исполняемым
#COPY FeedbackAnalysis.ReviewsHandlerApi/entrypoint.sh /app/entrypoint.sh
#RUN chmod +x /app/entrypoint.sh

# Копируем конфигурацию supervisor
COPY FeedbackAnalysis.ReviewsHandlerApi/supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Порты, которые использует ASP.NET
EXPOSE 8080
EXPOSE 8081

# Точка входа
#ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["/usr/bin/supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]