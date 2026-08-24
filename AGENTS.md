# AGENTS.md

Инструкции для ИИ-агентов и разработчиков по работе с репозиторием **FeedbackAnalysis**.

## Обзор проекта

Микросервисное решение для автоматизации обработки отзывов маркетплейсов.

**Основная идея приложения**: поставщик отзывов (сейчас — `FakeFeedbacksService`, генерирующий отзывы из заготовленных шаблонов) отправляет их в DataApi. DataApi сохраняет отзывы и обогащает тональностью через нейросеть (`FeedbacksHandlerApi`): негативные помечаются приоритетными. Оператор работает через ClientUI: вкладка **«Все»** — новые отзывы, требующие реакции; **«⭐ Приоритетные»** — негативные (тональность ≤ −0.25); **«Архив»** — обработанные (`Answered`/`Archived`). Оператор отвечает на отзыв (ответ хранится локально) или отправляет его в архив. Нейросеть — «сортировщик», оператор — точка принятия решений.

Стек:

- **.NET 10** (решение в формате `FeedbackAnalysis.slnx` — XML-вариант .sln)
- **Python 3.9** для ML-сервиса (запускается вручную: `pip install -r requirements.txt`, затем `python download_model.py` и `uvicorn main:app --port 8000`)
- **SQLite** + EF Core (`EnsureCreated()`, миграций нет)

| Сервис | Технология | Назначение | Локальный порт |
|---|---|---|---|
| `FeedbackAnalysis.ClientUI` | ASP.NET Core MVC | Веб-интерфейс оператора: вкладки Все/Приоритетные/Архив, ответы, архивирование | https://localhost:7154 |
| `FeedbackAnalysis.DataApi` | ASP.NET Core Web API + EF Core | REST API хранения/пагинации отзывов, вызов ML, статусы, ответы оператора (SQLite) | https://localhost:8082 |
| `FeedbackAnalysis.FakeFeedbacksService` | ASP.NET Core Web API | Генератор фейковых отзывов из шаблонов; ручной `POST api/generation/run` + opt-in таймер | https://localhost:7503 |
| `FeedbackAnalysis.FeedbacksHandlerApi` | Python FastAPI + BERT | ML-классификация тональности (`POST /predict`, `POST /predict/batch`) | http://localhost:8000 |
| `FeedbackAnalysis.Contracts` | Class library | Общие DTO/модели для всех .NET-сервисов | — |

## Архитектура и модульность

Решение рассчитано на масштабирование — **сохраняйте модульность при любых изменениях**.

Граф ссылок проектов (ProjectReference):

```
ClientUI ───────────► Contracts
DataApi  ──► Contracts
FakeFeedbacksService ──► Contracts
Tests    ──► ClientUI + Contracts + DataApi + FakeFeedbacksService
```

Правила:

- Сервисы общаются **только по HTTP** через конфигурацию `Services:{Имя}` (например, `Services:FeedbacksData`, `Services:FeedbacksHandler`). Никогда не хардкодьте URL — добавляйте ключ в `appsettings.json`; переопределяйте через переменные окружения `Services__{Имя}`.
- Общие модели/DTO размещаются **только** в `FeedbackAnalysis.Contracts`. Дублирование моделей между сервисами запрещено.
- Каждый сервис самодостаточен: свои контроллеры, сервисы, репозитории, DI-регистрация в `Program.cs`.

### Чек-лист добавления нового сервиса

1. Новая папка + `.csproj` в корне решения.
2. Запись проекта в `FeedbackAnalysis.slnx`.
3. Общие DTO — в `Contracts`; ссылка нового проекта только на `Contracts`.
4. Ключи конфигурации в `appsettings.json`; при необходимости — в профиль запуска «Все сервисы (https)» (`FeedbackAnalysis.slnLaunch`).
5. Тестовый проект/папка в `FeedbackAnalysis.Tests`, зеркалирующая структуру сервиса.
6. Если проект .NET — добавить `public partial class Program { }` в `Program.cs` (нужен для интеграционных тестов).

## Команды

```bash
dotnet restore FeedbackAnalysis.slnx
dotnet build FeedbackAnalysis.slnx --no-restore --nologo
dotnet test FeedbackAnalysis.slnx
```

Запуск всех .NET-сервисов локально: профиль «Все сервисы (https)» (`FeedbackAnalysis.slnLaunch`, Visual Studio) или по одному: `dotnet run --project FeedbackAnalysis.DataApi`.
ML-сервис запускается вручную (см. раздел Обзор); перед первым запуском — `python download_model.py`.

## Тестирование — обязательные правила

Фреймворк: **xUnit + Moq + Microsoft.AspNetCore.Mvc.Testing** (проект `FeedbackAnalysis.Tests`).

1. **Каждая новая фича или багфикс сопровождается тестами**: unit-тесты для логики/парсеров, интеграционные (`WebApplicationFactory`) — для контроллеров/API.
2. **Перед завершением задачи всегда прогоняйте** `dotnet test FeedbackAnalysis.slnx` и добивайтесь зелёного прогона.
3. Используйте готовые утилиты из `FeedbackAnalysis.Tests/TestSupport/`:
   - `SqliteTestDb` — держит открытым in-memory SQLite; схема создаётся автоматически благодаря `EnsureCreated()` в конструкторе `EFContext`;
   - `StubHttpMessageHandler` — заглушка HttpClient с записью запросов;
   - `CollectingLogger` — сборщик логов для ассертов.
4. Интеграционные тесты DataApi строятся по образцу `DataApiFactory : WebApplicationFactory<DataApi.Program>` с подменой `DbContextOptions<EFContext>` на общий in-memory SQLite.
5. Структура папок тестов зеркалит продакшн: `ContractsTests/`, `DataApiTests/`, `FakeFeedbacksServiceTests/`, `ClientUITests/`.
6. Новый тестовый проект включать в `FeedbackAnalysis.slnx`.

## Конвенции кода

- `<Nullable>enable</Nullable>` — код должен быть nullable-clean без предупреждений.
- Сериализация в приложениях — **Newtonsoft.Json** (MVC, парсеры, typed clients). В интеграционных тестах допустим System.Text.Json.
- Составной первичный ключ отзыва: `Id = "{Service}:{ServiceId}"` (например, `"wb:12345"`), дедупликация по нему в `DataApi`.
- Статусы ответов — `[Flags]` enum `FeedbackAnswerStatuses` (RequireToAnswer/Answered/Archived/NotHandled); фильтрация побитовым И.
- DI-паттерны: typed HttpClient через `AddHttpClient<>()`; репозитории/UoW — `AddScoped()`; генератор фейковых отзывов — Singleton + opt-in `BackgroundService` (включается ключом конфига `Generator:TimerEnabled`).
- Конфигурация читается через `IConfiguration.GetSection(...)` (без IOptions) — придерживайтесь этого до появления обоснованной причины иначе.
- Схема БД создаётся через `EnsureCreated()` (двойной вызов: в конструкторе `EFContext` и один раз на старте DataApi) — это осознанный дизайн, от него зависят тесты. Не заменяйте на `Migrate()` без обсуждения.
- Пакет `SQLitePCLRaw.bundle_e_sqlite3` запинен на 2.1.13 (CVE-2025-6965) — не обновляйте без проверки уязвимостей.
- Python-сервис не входит в slnx — его зависимости фиксируются в `requirements.txt`, модель скачивается локально скриптом `download_model.py`.

## Известные особенности

- `FeedbacksHandlerApi` называется «Api», но это **Python/FastAPI**, а не .NET — не ищите там csproj.
- Healthcheck-эндпоинты: `GET /healthz` (DataApi, FakeFeedbacksService), `GET /health` (ML-сервис).
- Комментарии в коде и UI — на русском языке; сохраняйте стиль.
