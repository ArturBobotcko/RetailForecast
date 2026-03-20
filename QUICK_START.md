# 🚀 Quick Start - Запуск приложения

## ⚡ За 2 минуты до работающего приложения

### Шаг 1: Backend (Terminal 1)
```bash
cd D:\RetailForecast
dotnet run
```
Ждете сообщения: `Now listening on: http://localhost:5000`

### Шаг 2: Frontend (Terminal 2)
```bash
cd D:\RetailForecast.Client\RetailForecast.Client
npm install  # только если первый раз
npm run dev
```
Ждете сообщения: `Local: http://localhost:5173`

### Шаг 3: Тест
1. Откройте http://localhost:5173
2. Зарегистрируйтесь с любым email и пароль (минимум 6 символов)
3. Должны попасть на дашборд

## ✅ Готово!

Система авторизации полностью функциональна.

---

## 📖 Подробнее

- **AUTH_SETUP.md** - Архитектура системы
- **RUN_INSTRUCTIONS.md** - Детальные инструкции
- **IMPLEMENTATION_SUMMARY.md** - Что было изменено

## 🔑 Ключевые компоненты

### Frontend (React 19)
- Форма авторизации/регистрации
- JWT токен в localStorage
- Context API для состояния
- Axios с автоматической подставкой токена

### Backend (.NET 10)
- RESTful API с JWT аутентификацией
- PBKDF2-SHA256 хеширование паролей
- PostgreSQL база данных
- Эндпоинты:
  - `POST /api/users` - Регистрация
  - `POST /api/users/login` - Авторизация

## 🛑 Проблемы?

### "Cannot connect to database"
PostgreSQL должен работать на `localhost:5432` с БД `retail_forecast`

### "CORS error"  
Убедитесь, что вы на `http://localhost:5173`

### "Invalid email or password"
Проверьте, что пользователь существует, или зарегистрируйтесь заново
