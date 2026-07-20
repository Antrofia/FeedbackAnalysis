#!/usr/bin/env python3
"""
Сервис для определения тональности текста через UDS сокет
Labels:
    0: NEUTRAL
    1: POSITIVE
    2: NEGATIVE
"""

import os
import socket
import json
import sys
import torch
from transformers import AutoModelForSequenceClassification
from transformers import BertTokenizerFast

# Путь к UDS сокету
SOCKET_PATH = '/tmp/sentiment_socket.sock'
model_path = "model"

# Загрузка модели и токенизатора
print("Загрузка модели и токенизатора...")
'''tokenizer = BertTokenizerFast.from_pretrained('blanchefort/rubert-base-cased-sentiment-rurewiews')
model = AutoModelForSequenceClassification.from_pretrained(
    'blanchefort/rubert-base-cased-sentiment-rurewiews', 
    return_dict=True
)'''
tokenizer = BertTokenizerFast.from_pretrained(model_path)
model = AutoModelForSequenceClassification.from_pretrained(
    model_path,
    return_dict=True
)
print("Модель загружена успешно!")

@torch.no_grad()
def predict(text):
    """Предсказание тональности текста"""
    inputs = tokenizer(
        text, 
        max_length=512, 
        padding=True, 
        truncation=True, 
        return_tensors='pt'
    )
    outputs = model(**inputs)
    predicted = torch.nn.functional.softmax(outputs.logits, dim=1)
    predicted = torch.argmax(predicted, dim=1).numpy()
    return int(predicted[0])  # Возвращаем число

def handle_client(conn):
    """Обработка клиентского соединения"""
    try:
        # Получаем данные от клиента
        data = conn.recv(4096).decode('utf-8')
        if not data:
            return
        
        # Парсим JSON с текстом
        try:
            request = json.loads(data)
            text = request.get('text', '')
        except json.JSONDecodeError:
            # Если JSON не пришел, пробуем обработать как простой текст
            text = data.strip()
        
        if not text:
            response = json.dumps({'error': 'Пустой текст'})
            conn.sendall(response.encode('utf-8'))
            return
        
        # Выполняем предсказание
        label = predict(text)
        
        # Отправляем ответ
        response = json.dumps({'label': label})
        conn.sendall(response.encode('utf-8'))
        
    except Exception as e:
        # В случае ошибки отправляем сообщение об ошибке
        error_response = json.dumps({'error': str(e)})
        conn.sendall(error_response.encode('utf-8'))
    finally:
        conn.close()

def start_server():
    """Запуск UDS сервера"""
    # Удаляем старый сокет, если существует
    if os.path.exists(SOCKET_PATH):
        os.remove(SOCKET_PATH)
    
    # Создаем сокет
    server = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    server.bind(SOCKET_PATH)
    server.listen(5)
    
    print(f"Сервер запущен на {SOCKET_PATH}")
    print("Ожидание подключений...")
    
    try:
        while True:
            conn, _ = server.accept()
            handle_client(conn)
    except KeyboardInterrupt:
        print("\nОстановка сервера...")
    finally:
        server.close()
        if os.path.exists(SOCKET_PATH):
            os.remove(SOCKET_PATH)

def test_client():
    """Тестовый клиент для проверки работы сервера"""
    client = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    
    try:
        client.connect(SOCKET_PATH)
        
        # Тестовые тексты
        texts = [
            "Отличный фильм, очень понравился!",
            "Ужасный сервис, никогда сюда не вернусь",
            "В целом неплохо, но есть над чем работать"
        ]
        
        for text in texts:
            print(f"\nОтправка: {text}")
            request = json.dumps({'text': text})
            client.sendall(request.encode('utf-8'))
            
            # Получаем ответ
            response = client.recv(1024).decode('utf-8')
            result = json.loads(response)
            
            if 'label' in result:
                label_map = {0: 'NEUTRAL', 1: 'POSITIVE', 2: 'NEGATIVE'}
                print(f"Ответ: label={result['label']} ({label_map[result['label']]})")
            else:
                print(f"Ошибка: {result.get('error', 'Неизвестная ошибка')}")
            
            # Небольшая задержка между запросами
            import time
            time.sleep(0.5)
            
    except Exception as e:
        print(f"Ошибка клиента: {e}")
    finally:
        client.close()

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == '--test':
        test_client()
    else:
        start_server()