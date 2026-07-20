# main.py
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

# Определение модели данных для ответа
class PredictionResponse(BaseModel):
    text: str
    label: int
    confidence: float = None  # Опционально можно добавить уверенность

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

# Эндпоинт для предсказания
@app.post("/predict", response_model=PredictionResponse)
async def predict_endpoint(request: TextRequest):
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