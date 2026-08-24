# main.py
from typing import List

import torch
from transformers import AutoModelForSequenceClassification, BertTokenizerFast
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn

# Инициализация FastAPI приложения
app = FastAPI(title="Text Classification API", version="1.0")

# Загрузка модели и токенизатора
model_path = "Model"

try:
    tokenizer = BertTokenizerFast.from_pretrained(model_path)
    model = AutoModelForSequenceClassification.from_pretrained(
        model_path,
        return_dict=True
    )
    model.eval()  # Переводим модель в режим оценки
    print("Модель успешно загружена!")
except Exception as e:
    print(f"Ошибка при загрузке модели: {e}")
    raise

# Определение модели данных для запроса
class TextRequest(BaseModel):
    text: str

# Модель данных для батч-запроса
class BatchTextRequest(BaseModel):
    texts: List[str]

# Определение модели данных для ответа
class PredictionResponse(BaseModel):
    text: str
    label: int
    confidence: float = None  # Опционально можно добавить уверенность

# Модель данных для батч-ответа
class BatchPredictionResponse(BaseModel):
    results: List[PredictionResponse]

@torch.no_grad()
def predict(text: str):
    """
    Функция для предсказания класса текста
    """
    inputs = tokenizer(
        text,
        max_length=512,
        padding=True,
        truncation=True,
        return_tensors='pt'
    )
    outputs = model(**inputs)
    
    # Получаем вероятности
    probabilities = torch.nn.functional.softmax(outputs.logits, dim=1)
    
    # Получаем предсказанный класс
    predicted_label = torch.argmax(probabilities, dim=1).item()
    
    # Получаем уверенность модели
    confidence = probabilities[0][predicted_label].item()
    
    return predicted_label, confidence

@torch.no_grad()
def predict_batch(texts: List[str]):
    """
    Батч-предсказание: токенизация и forward pass одним вызовом на весь список.
    Возвращает список (label, confidence) в порядке входных текстов.
    """
    inputs = tokenizer(
        texts,
        max_length=512,
        padding=True,
        truncation=True,
        return_tensors='pt'
    )
    outputs = model(**inputs)
    
    probabilities = torch.nn.functional.softmax(outputs.logits, dim=1)
    
    confidences, predicted_labels = torch.max(probabilities, dim=1)
    
    return [
        (predicted_labels[i].item(), confidences[i].item())
        for i in range(len(texts))
    ]

# Эндпоинт для предсказания.
# Синхронный def (не async), чтобы torch-инференс не блокировал event loop:
# FastAPI выполняет такие обработчики в отдельном threadpool.
@app.post("/predict", response_model=PredictionResponse)
def predict_endpoint(request: TextRequest):
    """
    Эндпоинт для классификации текста.
    Принимает POST запрос с JSON: {"text": "ваш текст"}
    Возвращает JSON с предсказанным классом и уверенностью
    """
    if not request.text or len(request.text.strip()) == 0:
        raise HTTPException(status_code=400, detail="Текст не может быть пустым")
    
    try:
        label, confidence = predict(request.text)
        return PredictionResponse(
            text=request.text,
            label=label,
            confidence=round(confidence, 4)
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ошибка при предсказании: {str(e)}")

# Батч-эндпоинт для классификации списка текстов.
# Синхронный def — как /predict, чтобы torch-инференс не блокировал event loop.
@app.post("/predict/batch", response_model=BatchPredictionResponse)
def predict_batch_endpoint(request: BatchTextRequest):
    """
    Эндпоинт для батч-классификации.
    Принимает POST запрос с JSON: {"texts": ["текст 1", "текст 2"]}
    Возвращает JSON с результатами в порядке входных текстов
    """
    if not request.texts or any(not t or len(t.strip()) == 0 for t in request.texts):
        raise HTTPException(status_code=400, detail="Список текстов не может быть пустым или содержать пустые тексты")
    
    try:
        predictions = predict_batch(request.texts)
        
        return BatchPredictionResponse(results=[
            PredictionResponse(
                text=text,
                label=label,
                confidence=round(confidence, 4)
            )
            for text, (label, confidence) in zip(request.texts, predictions)
        ])
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ошибка при батч-предсказании: {str(e)}")

# Эндпоинт для проверки работоспособности
@app.get("/health")
async def health_check():
    """
    Эндпоинт для проверки статуса сервиса
    """
    return {"status": "healthy", "model_loaded": True}

# Корневой эндпоинт
@app.get("/")
async def root():
    return {
        "message": "Text Classification API",
        "endpoints": {
            "POST /predict": "Классификация текста",
            "POST /predict/batch": "Батч-классификация списка текстов",
            "GET /health": "Проверка статуса"
        }
    }

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=False  # Для разработки можно установить True
    )