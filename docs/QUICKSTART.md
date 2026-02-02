# Инструкция по запуску Stepik Analytics

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Stepik API credentials

## Шаг 1: Получение Stepik API credentials

1. Перейдите на [https://stepik.org/oauth2/applications/](https://stepik.org/oauth2/applications/)
2. Войдите в свой аккаунт Stepik
3. Нажмите "Register an application"
4. Заполните:
   - **Name**: Stepik Analytics
   - **Client type**: Confidential
   - **Authorization grant type**: Client credentials
5. Сохраните `Client ID` и `Client Secret`

## Шаг 2: Настройка окружения

Создайте файл `docker/.env`:

```bash
cd /path/to/project/docker
cat > .env << EOF
STEPIK_CLIENT_ID=ваш_client_id
STEPIK_CLIENT_SECRET=ваш_client_secret
JWT_SECRET=YourSuperSecretKeyHereMustBeAtLeast32CharactersLong!
EOF
```

## Шаг 3: Запуск PostgreSQL

```bash
cd docker
docker compose up -d postgres

# Проверка
docker compose ps
```

## Шаг 4: Запуск Backend

```bash
cd src/StepikAnalytics.Backend

# Установка зависимостей
dotnet restore

# Настройка переменных окружения
export STEPIK_CLIENT_ID=ваш_client_id
export STEPIK_CLIENT_SECRET=ваш_client_secret

# Запуск
dotnet run
```

Backend будет доступен:
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Hangfire: http://localhost:5000/hangfire

## Шаг 5: Запуск Desktop приложения

```bash
cd src/StepikAnalytics.Desktop
dotnet run
```

## Шаг 6: Использование

1. **Регистрация**: Создайте аккаунт в приложении
2. **Вход**: Войдите под созданным аккаунтом
3. **Добавление курса**: Введите ID курса Stepik или полную ссылку
   - Пример ID: `73`
   - Пример URL: `https://stepik.org/course/73/promo`
4. **Просмотр статистики**: Нажмите "Открыть дашборд"
5. **Синхронизация**: Нажмите "Обновить данные" для обновления

## Альтернативный запуск (Docker Compose)

Полный запуск всей системы:

```bash
cd docker
docker compose up -d
```

## Устранение неполадок

### PostgreSQL не запускается
```bash
docker compose down -v
docker compose up -d postgres
```

### Backend не подключается к БД
Проверьте строку подключения в `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=stepik_analytics;Username=postgres;Password=postgres"
  }
}
```

### Ошибка авторизации Stepik
Проверьте правильность Client ID и Client Secret. Убедитесь, что приложение зарегистрировано как "Confidential" с grant type "Client credentials".

### Desktop не подключается к Backend
Проверьте URL API в `App.axaml.cs`:
```csharp
client.BaseAddress = new Uri("http://localhost:5000/api/");
```

## Очистка данных

```bash
# Удалить все контейнеры и данные
cd docker
docker compose down -v
```
