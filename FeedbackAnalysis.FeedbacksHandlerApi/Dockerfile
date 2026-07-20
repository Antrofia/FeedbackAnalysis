# Сборка зависимостей
FROM python:3.9-slim as builder

WORKDIR /app

# Устанавливаем pip с увеличенным таймаутом
RUN pip install --upgrade pip --timeout 1000

# Копируем только requirements.txt (этот слой будет кэшироваться, если файл не менялся)
COPY FeedbackAnalysis.FeedbacksHandlerApi/requirements.txt .

# Устанавливаем зависимости (этот слой будет пересобираться только при изменении requirements.txt)
RUN pip install --user --no-cache-dir -r requirements.txt --timeout 1000

# Финальный образ
FROM python:3.9-slim

WORKDIR /app

# Копируем установленные зависимости из builder
COPY --from=builder /root/.local /root/.local

ENV PATH=/root/.local/bin:$PATH \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    TRANSFORMERS_CACHE=/app/.cache \
    HF_HOME=/app/.cache

# Создаем директорию для модели
RUN mkdir -p /app/Model /app/.cache

# Скачиваем модель с помощью Python
RUN python -c "from transformers import AutoModelForSequenceClassification, BertTokenizerFast; \
    model = AutoModelForSequenceClassification.from_pretrained('blanchefort/rubert-base-cased-sentiment-rurewiews'); \
    tokenizer = BertTokenizerFast.from_pretrained('blanchefort/rubert-base-cased-sentiment-rurewiews'); \
    model.save_pretrained('/app/Model'); \
    tokenizer.save_pretrained('/app/Model')"

# Копируем основной код (этот слой будет пересобираться при каждом изменении кода)
COPY FeedbackAnalysis.FeedbacksHandlerApi/main.py .

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]