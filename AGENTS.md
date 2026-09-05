# AGENTS.md

Приложение «Домашний бюджет»: Angular 22 (signals, zoneless, Tailwind 4, Chart.js) + ASP.NET Core 10 Web API + PostgreSQL 16. Требования: .NET SDK 10, Node 22+, Docker (для БД). Спецификация продукта — `ТЗ_ Домашний бюджет …md` в корне (русский).

## Команды

Разработка (3 процесса):

```bash
# 1. БД (один раз)
docker run -d --name budget-pg -e POSTGRES_DB=budget -e POSTGRES_USER=budget -e POSTGRES_PASSWORD=budget -p 5432:5432 postgres:16-alpine
# 2. API — ОБЯЗАТЕЛЬНО порт 5080 (frontend-прокси ждёт именно его, не дефолтный 5000)
cd backend/BudgetApi && dotnet run --urls http://localhost:5080
# 3. Frontend — http://localhost:4200, прокси /api → 5080
cd frontend && npm install && npm start
```

Продакшен одним контейнером (API сам раздаёт собранный SPA, порт 8080):

```bash
cp .env.example .env   # задать JWT_KEY (openssl rand -hex 32) и POSTGRES_PASSWORD
docker compose up -d --build
```

Тесты (оба уровня не требуют запущенной БД):

```bash
cd backend/BudgetApi.Tests && dotnet test                       # xUnit, InMemory
cd backend/BudgetApi.Tests && dotnet test --filter "FullyQualifiedName~UserTests"   # один класс
cd frontend && npx ng test                                      # vitest + jsdom
cd frontend && npx ng test --include='**/app.spec.ts'           # один файл
```

Миграции EF (схема применяется автоматически при старте API, см. ниже):

```bash
cd backend/BudgetApi && dotnet ef migrations add <Name>
```

## Как это устроено

- **Нет `.sln`** — в `backend/` два независимых проекта (`BudgetApi`, `BudgetApi.Tests`); dotnet-команды запускаются из каталога проекта.
- **Один контейнер = API + SPA**: `Program.cs` через `MapFallback` раздаёт `wwwroot` (содержимое `frontend/dist/budget-web/browser` после `npm run build`); без сборки фронтенда SPA отвечает 404.
- **Старт API**: `MigrateAsync()` + `DbSeeder` (создаёт `admin/admin123` и категории) — пропускается только в среде `Testing`.
- **Провайдер БД переключается конфигурацией** `Database:Provider` (`Npgsql` по умолчанию / `InMemory` в тестах). Npgsql работает со snake_case-именованием (`UseSnakeCaseNamingConvention`).
- **Swagger** доступен только в `Development` (`localhost:5080/swagger`).
- **JSON-контракт**: перечисления сериализуются строками ВЕРХНИМ регистром (`kind: "EXPENSE"`); ошибки валидации — `{code, message, errors}` (кастомная `InvalidModelStateResponseFactory`).
- JWT — 24 ч, пароли BCrypt; деактивированный пользователь не входит, но его история операций видна.
- Логи — Serilog в `logs/app-.log` (ротация по дням).

## Frontend-особенности

- Тесты — **vitest** (builder `@angular/build:unit-test`), не Karma/Jasmine; в `src/test-setup.ts` подменяется `localStorage`.
- Стиль: 2 пробела, одинарные кавычки в TS (`.editorconfig`), Prettier.
- Компоненты — signals/zoneless; фичи в `src/app/features/` (journal, stats, categories, export, settings …).

## Ограничения

- `.env` (JWT_KEY, POSTGRES_PASSWORD) не коммитить; секреты только в `.env`.
- Порты наружу не публикуются — приложение для домашней сети; не добавлять публичные эндпоинты/CDN без необходимости.
