# RetailForecast - Система Авторизации

## Описание

Реализована полная система авторизации и регистрации с использованием JWT токенов.

### Компоненты:

#### Frontend (D:\RetailForecast.Client\RetailForecast.Client)
- **Auth.jsx** - Форма авторизации/регистрации
- **Dashboard.jsx** - Дашборд после авторизации
- **services/api.js** - Axios клиент с автоматической подставкой токенов
- **context/AuthContext.jsx** - Context для управления состоянием авторизации
- **index.css** - Стили формы и дашборда

#### Backend (D:\RetailForecast)
- **Services/JwtService.cs** - Генерация JWT токенов
- **Controllers/UsersController.cs** - Эндпоинты:
  - `POST /api/users` - Регистрация (создание пользователя)
  - `POST /api/users/login` - Авторизация
  - `GET /api/users` - Получить всех пользователей
  - `GET /api/users/{id}` - Получить пользователя по ID
  - `PUT /api/users/{id}` - Обновить пользователя
  - `DELETE /api/users/{id}` - Удалить пользователя
- **Services/UserService.cs** - Бизнес логика:
  - `HashPassword()` - Хеширование пароля (PBKDF2-SHA256)
  - `VerifyPassword()` - Проверка пароля
  - `CreateAsync()` - Создание пользователя
  - `LoginAsync()` - Авторизация пользователя
  - `UpdateAsync()` - Обновление пользователя
  - `DeleteAsync()` - Удаление пользователя

## Установка и запуск

### Backend

1. Восстановите зависимости:
```bash
cd D:\RetailForecast
dotnet restore
```

2. Примените миграции базы данных:
```bash
dotnet ef database update
```

3. Запустите сервер (по умолчанию на http://localhost:5000):
```bash
dotnet run
```

### Frontend

1. Установите зависимости:
```bash
cd D:\RetailForecast.Client\RetailForecast.Client
npm install
```

2. Запустите dev сервер (по умолчанию на http://localhost:5173):
```bash
npm run dev
```

## Поток авторизации

### Регистрация:
1. Пользователь вводит email и пароль
2. Форма валидирует данные (email должен содержать @, пароль минимум 6 символов)
3. Отправляется запрос: `POST /api/users`
4. Бэкэнд хеширует пароль и сохраняет пользователя в БД
5. После регистрации автоматически выполняется логин
6. Получается JWT токен и сохраняется в localStorage
7. Пользователь редирректится на /dashboard

### Авторизация:
1. Пользователь вводит email и пароль
2. Отправляется запрос: `POST /api/users/login`
3. Бэкэнд проверяет пароль против хеша
4. Если верно, возвращается JWT токен
5. Токен сохраняется в localStorage
6. Пользователь редирректится на /dashboard

## Конфигурация

### JWT Settings (appsettings.json)
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-change-in-production",
    "Issuer": "RetailForecast",
    "Audience": "RetailForecastClient",
    "ExpirationHours": 24
  }
}
```

### CORS (Program.cs)
Фронтэнд может обращаться с:
- http://localhost:5173
- https://localhost:5173

### API Примеры

### Регистрация
```bash
POST http://localhost:5066/api/users
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123"
}

Response (201):
{
  "id": 1,
  "email": "user@example.com",
  "createdAt": "2026-03-20T17:23:00Z"
}
```

### Авторизация
```bash
POST http://localhost:5066/api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123"
}

Response (200):
{
  "id": 1,
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## Безопасность

- ✅ Пароли хешируются с использованием PBKDF2-SHA256 с солью
- ✅ JWT токены имеют срок действия (по умолчанию 24 часа)
- ✅ Токены отправляются в Authorization header: `Bearer <token>`
- ✅ CORS настроен только на фронтэнд домены
- ✅ Клиент автоматически подставляет токен во все запросы

## Структура проекта

```
RetailForecast.Client/
├── src/
│   ├── components/
│   │   ├── Auth.jsx
│   │   └── Dashboard.jsx
│   ├── context/
│   │   └── AuthContext.jsx
│   ├── services/
│   │   └── api.js
│   ├── App.jsx
│   ├── main.jsx
│   └── index.css
│   
RetailForecast/
├── Controllers/
│   └── UsersController.cs
├── Services/
│   ├── UserService.cs
│   └── JwtService.cs
├── DTOs/
│   └── User/
│       ├── CreateUserRequest.cs
│       ├── LoginRequest.cs
│       └── AuthResponse.cs
└── Program.cs
```

## Дополнительно

- Используется React 19 с hooks
- Axios для HTTP запросов с перехватчиками
- Context API для управления состоянием авторизации
- Vite как dev сервер
- .NET 10 с Entity Framework Core
- PostgreSQL как БД
