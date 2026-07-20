#!/bin/bash
set -e

# Функция для запуска Python-сервиса
start_python_service() {
    echo "Starting sentiment analysis service..."
    /venv/bin/python3 /app/reviews_predicate.py &
    PYTHON_PID=$!
    echo "Python service started with PID: $PYTHON_PID"
}

# Функция для проверки готовности сокета
wait_for_socket() {
    echo "Waiting for sentiment socket..."
    local max_attempts=30
    local attempt=0
    while [ ! -S /tmp/sentiment_socket.sock ] && [ $attempt -lt $max_attempts ]; do
        sleep 1
        attempt=$((attempt + 1))
        echo "Waiting for socket... attempt $attempt/$max_attempts"
    done
    
    if [ -S /tmp/sentiment_socket.sock ]; then
        echo "Socket is ready!"
        return 0
    else
        echo "ERROR: Socket not created after $max_attempts seconds"
        return 1
    fi
}

# Запускаем Python-сервис
start_python_service

# Ждем готовности сокета
wait_for_socket

# Проверяем, что Python-процесс жив
if ! kill -0 $PYTHON_PID 2>/dev/null; then
    echo "ERROR: Python service died unexpectedly"
    exit 1
fi

echo "Starting .NET application..."
# Запускаем .NET приложение в foreground (заменяет текущий процесс)
exec dotnet /app/FeedbackAnalysis.ReviewsHandlerApi.dll