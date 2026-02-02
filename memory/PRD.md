# Stepik Analytics - PRD (Product Requirements Document)

## Исходная постановка задачи

Система "Stepik Analytics" для сбора статистики с платформы Stepik и отображения в современном светлом десктоп-приложении на Avalonia.

## Архитектура

- **Desktop UI**: Avalonia (.NET 10), MVVM, CommunityToolkit.Mvvm
- **Backend**: ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL
- **Background Jobs**: Hangfire с PostgreSQL storage
- **Charts**: LiveCharts2

## Реализованные компоненты

### Backend (ASP.NET Core)
- [x] AuthController - JWT авторизация
- [x] CoursesController - CRUD курсов
- [x] MetricsController - получение метрик
- [x] SyncRunsController - история синхронизаций
- [x] StepikApiClient - интеграция с OAuth2
- [x] MetricsCollector - сбор и агрегация данных
- [x] MetricsService - бизнес-логика
- [x] SyncJob - Hangfire фоновые задачи
- [x] AppDbContext - EF Core контекст

### Desktop (Avalonia)
- [x] MainWindow - главное окно с навигацией
- [x] LoginView/ViewModel - авторизация/регистрация
- [x] CoursesView/ViewModel - список курсов
- [x] CourseDashboardView/ViewModel - дашборд с KPI и графиками
- [x] ApiClient - HTTP клиент к backend

### Shared DTOs
- [x] CourseDto, CourseCreateDto
- [x] MetricsResponseDto, SummaryDto, SeriesDto
- [x] SyncRunDto
- [x] AuthDto (Login/Register)

### Инфраструктура
- [x] Docker Compose
- [x] Dockerfile для backend
- [x] PostgreSQL схема
- [x] README с инструкциями

## Метрики

| Метрика | Источник | Статус |
|---------|----------|--------|
| Всего решений | submissions API | ✅ |
| Правильные/Неправильные | submissions.status | ✅ |
| Новые ученики | learners_count delta | ✅ |
| Сертификаты | course.certificates_count | ✅ |
| Отзывы | course-reviews API | ✅ |
| Рейтинг | course.score | ✅ |
| DAU | unique users with submissions | ✅ |
| Reputation Delta | Заглушка (API недоступен) | ⚠️ |
| Knowledge Delta | Заглушка (API недоступен) | ⚠️ |

## Backlog

### P0 (Critical)
- [ ] Тестирование с реальными Stepik credentials
- [ ] Добавить миграции EF Core

### P1 (High)
- [ ] Кэширование токена Stepik в Redis
- [ ] Export метрик в CSV/Excel
- [ ] Уведомления о падении метрик

### P2 (Medium)
- [ ] Dark theme
- [ ] Графики распределения оценок
- [ ] Сравнение нескольких курсов

## Дата создания

2025-02-02
