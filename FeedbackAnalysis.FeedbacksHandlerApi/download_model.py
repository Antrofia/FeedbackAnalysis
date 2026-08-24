# download_model.py
# Скачивает модель тональности в локальную папку Model/
# (раньше это делал Docker-образ при сборке).
#
# Запуск: python download_model.py
import os

from transformers import AutoModelForSequenceClassification, BertTokenizerFast

MODEL_ID = "blanchefort/rubert-base-cased-sentiment-rurewiews"
MODEL_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Model")


def main():
    print(f"Скачивание модели {MODEL_ID} в {MODEL_DIR} ...")

    model = AutoModelForSequenceClassification.from_pretrained(MODEL_ID)
    tokenizer = BertTokenizerFast.from_pretrained(MODEL_ID)

    model.save_pretrained(MODEL_DIR)
    tokenizer.save_pretrained(MODEL_DIR)

    print("Модель успешно скачана!")


if __name__ == "__main__":
    main()
