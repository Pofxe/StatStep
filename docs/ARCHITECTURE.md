# Архитектура Stepik Analytics

## 1. Общая архитектура системы

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           STEPIK ANALYTICS SYSTEM                            │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌────────────────────┐          ┌────────────────────────────────────┐    │
│   │                    │   HTTP   │                                    │    │
│   │   Desktop Client   │─────────▶│         Backend API                │    │
│   │   (Avalonia UI)    │◀─────────│       (ASP.NET Core)               │    │
│   │                    │   JSON   │                                    │    │
│   └────────────────────┘          └───────────────┬────────────────────┘    │
│          │                                        │                          │
│          │                                        │                          │
│          │ MVVM                                   │ EF Core                  │
│          │                                        ▼                          │
│   ┌──────┴──────┐                        ┌───────────────┐                  │
│   │ ViewModels  │                        │  PostgreSQL   │                  │
│   │   Views     │                        │   Database    │                  │
│   │  Services   │                        └───────┬───────┘                  │
│   └─────────────┘                                │                          │
│                                                  │                          │
│                                    ┌─────────────┴─────────────┐            │
│                                    │      Hangfire Server      │            │
│                                    │    (Background Jobs)      │            │
│                                    └─────────────┬─────────────┘            │
│                                                  │                          │
│                                                  │ OAuth2 + REST            │
│                                                  ▼                          │
│                                    ┌───────────────────────────┐            │
│                                    │       Stepik API          │            │
│                                    │    (External Service)     │            │
│                                    └───────────────────────────┘            │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## 2. Потоки данных

### 2.1 Поток синхронизации данных

```
Hangfire Scheduler                  Stepik API Client
      │                                   │
      │ [1] Trigger SyncJob               │
      ▼                                   │
┌─────────────┐                          │
│  SyncJob    │──[2] GetCourseInfo──────▶│
│             │◀───────────────────────────│
│             │                          │
│             │──[3] GetSubmissions──────▶│
│             │◀─────────────────────────│
│             │                          │
│             │──[4] GetReviews──────────▶│
│             │◀─────────────────────────│
└──────┬──────┘                          │
       │                                  │
       │ [5] Process & Aggregate          │
       ▼                                  │
┌─────────────┐                          │
│ Aggregator  │                          │
│             │──[6] Group by Date        │
│             │──[7] Calculate DAU        │
│             │──[8] Calculate Deltas     │
└──────┬──────┘                          │
       │                                  │
       │ [9] UPSERT                       │
       ▼                                  │
┌─────────────┐                          │
│ PostgreSQL  │                          │
│  Database   │                          │
└─────────────┘                          │
```

### 2.2 Поток отображения данных в UI

```
Desktop UI                      Backend API                    Database
    │                               │                              │
    │──[1] Request Metrics─────────▶│                              │
    │                               │──[2] Query Metrics──────────▶│
    │                               │◀─────────────────────────────│
    │                               │                              │
    │                               │──[3] Calculate Summary       │
    │                               │──[4] Build Series            │
    │                               │──[5] Calculate Comparison    │
    │                               │                              │
    │◀──[6] MetricsResponse─────────│                              │
    │                               │                              │
    │──[7] Update ViewModels        │                              │
    │──[8] Render Charts            │                              │
    │──[9] Display KPI Cards        │                              │
    │                               │                              │
```

## 3. Компоненты системы

### 3.1 Backend (ASP.NET Core)

| Компонент | Ответственность |
|-----------|-----------------|
| `AuthController` | JWT-авторизация, регистрация |
| `CoursesController` | CRUD для курсов |
| `MetricsController` | Получение метрик |
| `SyncRunsController` | История синхронизаций |
| `StepikApiClient` | Интеграция со Stepik API |
| `MetricsCollector` | Сбор и агрегация данных |
| `MetricsService` | Бизнес-логика метрик |
| `SyncJob` | Hangfire background job |

### 3.2 Desktop (Avalonia)

| Компонент | Ответственность |
|-----------|-----------------|
| `MainWindow` | Главное окно, навигация |
| `LoginView/ViewModel` | Авторизация |
| `CoursesView/ViewModel` | Список курсов |
| `CourseDashboardView/ViewModel` | Дашборд с метриками |
| `ApiClient` | HTTP-клиент к Backend |

## 4. Эндпоинты Stepik API

### Используемые эндпоинты

| Эндпоинт | Назначение | Примечания |
|----------|------------|------------|
| `GET /api/courses/{id}` | Информация о курсе | learners_count, score |
| `GET /api/submissions?step={id}` | Решения по шагу | Пагинация, status, time |
| `GET /api/course-reviews?course={id}` | Отзывы | score, create_date |
| `GET /api/sections/{id}` | Секции курса | units |
| `GET /api/units/{id}` | Юниты | lesson |
| `GET /api/lessons/{id}` | Уроки | steps |
| `POST /oauth2/token/` | OAuth токен | client_credentials |

### Маппинг полей

```
Stepik API                    →    Локальная модель
─────────────────────────────────────────────────────
courses[0].title              →    Course.Title
courses[0].summary            →    Course.Description
courses[0].cover              →    Course.CoverUrl
courses[0].learners_count     →    MetricsDaily.NewLearners (delta)
courses[0].score              →    MetricsDaily.RatingValue
submissions[].status          →    MetricsDaily.CorrectSubmissions (if "correct")
submissions[].time            →    Группировка по дате
submissions[].user            →    Подсчет DAU (unique users)
course-reviews[].score        →    MetricsDaily.ReviewsAvg (avg)
course-reviews[].create_date  →    Группировка по дате
```

## 5. Ограничения и допущения

### 5.1 Известные ограничения Stepik API

1. **Нет прямого endpoint для DAU** - вычисляется из submissions
2. **Нет endpoint для reputation/knowledge delta** - заглушка в коде
3. **Certificates count** - доступен только общий, не за период
4. **Rate limits** - требуется throttling и backoff

### 5.2 Стратегии обхода

```csharp
// Заглушка для недоступных метрик
public interface IMetricCalculator
{
    Task<int> CalculateReputationDeltaAsync(int courseId, DateTime since);
}

// Feature flag для экспериментальных метрик
if (FeatureFlags.IsEnabled("experimental_knowledge_delta"))
{
    metrics.KnowledgeDelta = await CalculateKnowledgeDelta();
}
```

## 6. Безопасность

### 6.1 Хранение секретов

```
Environment Variables (required):
├── STEPIK_CLIENT_ID          # OAuth Client ID
├── STEPIK_CLIENT_SECRET      # OAuth Client Secret  
├── JWT_SECRET                # JWT signing key
└── ConnectionStrings__DefaultConnection  # PostgreSQL
```

### 6.2 Авторизация

```
Desktop App    →    Backend API    →    Stepik API
   │                    │                   │
   │──JWT Token────────▶│                   │
   │                    │──Bearer Token────▶│
   │                    │◀─────────────────│
   │◀───Response───────│                   │
```

## 7. Мониторинг и логирование

### Serilog конфигурация

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/stepik-analytics-.log" }}
    ],
    "Enrich": ["FromLogContext", "WithCorrelationId"]
  }
}
```

### Hangfire Dashboard

- URL: `/hangfire`
- Мониторинг: jobs, retries, errors
- Защита: localhost-only или API Key
