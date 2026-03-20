# Запуск приложения RetailForecast

## Шаг 1: Подготовка БД (если не готова)

```bash
cd D:\RetailForecast

# Применить миграции (если база еще не инициализирована)
dotnet ef database update

# Или создать базу заново
dotnet ef database drop -f
dotnet ef database update
```

## Шаг 2: Запуск Backend

```bash
cd D:\RetailForecast

# Запуск сервера (будет слушать на http://localhost:5066)
dotnet run
```

Backend должен быть готов к работе с сообщением типа:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5066
```

## Шаг 3: Запуск Frontend (в новом терминале)

```bash
cd D:\RetailForecast.Client\RetailForecast.Client

# Установка зависимостей (если еще не установлены)
npm install

# Запуск dev сервера (будет слушать на http://localhost:5173)
npm run dev
```

Frontend будет доступен по адресу:
```
http://localhost:5173
```

## Тестирование

### 1. Регистрация нового пользователя:
1. Откройте http://localhost:5173
2. Выберите "Зарегистрируйтесь"
3. Введите email и пароль
4. Нажмите "Зарегистрироваться"

### 2. Авторизация:
1. Откройте http://localhost:5173
2. Введите email и пароль
3. Нажмите "Войти"
4. Должны перейти на дашборд

### 3. Проверка через API (Postman/curl):

#### Регистрация:
```bash
curl -X POST http://localhost:5066/api/users \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123"}'
```

#### Авторизация:
```bash
curl -X POST http://localhost:5066/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123"}'
```

## Важные изменения

### ✅ Frontend
- Добавлен Axios HTTP клиент
- Создан Context для управления авторизацией
- Форма Auth.jsx теперь работает с реальным API
- Добавлены стили в index.css
- Токен автоматически сохраняется и отправляется со всеми запросами

### ✅ Backend
- Добавлен JWT сервис для генерации токенов
- Создан эндпоинт `/api/users/login` для авторизации
- Добавлена JWT аутентификация в Program.cs
- Добавлены DTO для логина и ответа с токеном

### ✅ База данных
- Требуется PostgreSQL на localhost:5432
- БД: retail_forecast
- Пользователь: postgres
- Пароль: 1234

## Структура ответов

### POST /api/users (Регистрация)
Request:
```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

Response (201):
```json
{
  "id": 1,
  "email": "user@example.com",
  "createdAt": "2026-03-20T17:23:00Z"
}
```

### POST /api/users/login (Авторизация)
Request:
```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

Response (200):
```json
{
  "id": 1,
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## Безопасность

- JWT токены действительны 24 часа
- Пароли хешируются с PBKDF2-SHA256
- Токены отправляются только через HTTPS Header (Authorization: Bearer <token>)
- CORS настроен только на фронтэнд домены

## Обновление JWT Secret

⚠️ Важно: перед развертыванием в production измените `SecretKey` в appsettings.json на что-то безопасное!

В appsettings.json:
```json
"JwtSettings": {
  "SecretKey": "your-new-super-secret-key-with-at-least-32-chars"
}
```

## Troubleshooting

### Ошибка: "Cannot connect to database"
- Убедитесь, что PostgreSQL запущен на localhost:5432
- Проверьте credentials в appsettings.json

### Ошибка: "CORS error"
- Убедитесь, что вы открываете фронтэнд с http://localhost:5173
- Проверьте Program.cs CORS конфигурацию

### Ошибка: "JWT validation failed"
- Убедитесь, что SecretKey в appsettings.json совпадает с тем, что используется фронтэндом
- Проверьте, что токен отправляется в Authorization header

### Ошибка: "Invalid email or password"
- При авторизации проверьте, что пользователь существует и пароль верный
- При регистрации убедитесь, что email еще не зарегистрирован
