# AGENTS.md

Приложение «Домашний бюджет»: Angular 22 (signals, zoneless, Tailwind 4, Chart.js) + ASP.NET Core 10 Web API (Clean Architecture) + PostgreSQL 16. Требования: .NET SDK 10, Node 22+, Docker (для БД). Спецификация продукта — `ТЗ_ Домашний бюджет …md` в `docs/` (русский).

**Навигация по проекту — файлы BRIEF.md**: корень `BRIEF.md` (общая архитектура и дерево), `backend/BRIEF.md` (+ по BRIEF.md в каждом проекте backend и в `frontend`, `frontend/src`, `frontend/src/app`, `docs`). Начинайте знакомство с `BRIEF.md` в корне.

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

Миграции EF (схема применяется автоматически при старте API, см. ниже). Контекст и миграции живут в `Budget.Infrastructure`, запускайте CLI из `BudgetApi` (startup-проект):

```bash
cd backend/BudgetApi && dotnet ef migrations add <Name> --project ..\Budget.Infrastructure
cd backend/BudgetApi && dotnet ef migrations has-pending-model-changes --project ..\Budget.Infrastructure
```

## Как это устроено

- **Нет `.sln`** — в `backend/` пять независимых проектов Clean Architecture: `Budget.Domain` (сущности) → `Budget.Application` (сервисы, DTO, контракты) → `Budget.Infrastructure` (EF Core, миграции, репозитории, BCrypt, Excel) → `BudgetApi` (контроллеры, JWT, Program.cs); `BudgetApi.Tests` (xUnit). dotnet-команды запускаются из каталога проекта.
- **Контроллеры не касаются DbContext** — только интерфейсы сервисов `I*Service` из `Budget.Application`; данные — через `IUnitOfWork` (реализация в `Budget.Infrastructure`).
- **Один контейнер = API + SPA**: `Program.cs` через `MapFallback` раздаёт `wwwroot` (содержимое `frontend/dist/budget-web/browser` после `npm run build`); без сборки фронтенда SPA отвечает 404.
- **Старт API**: `MigrateDatabaseAsync()` + `SeedDatabaseAsync()` (создаёт `admin/admin123`, категории, счета) — пропускается только в среде `Testing`.
- **Провайдер БД переключается конфигурацией** `Database:Provider` (`Npgsql` по умолчанию / `InMemory` в тестах) — переключение в `Budget.Infrastructure/DependencyInjection.cs`. Npgsql работает со snake_case-именованием (`UseSnakeCaseNamingConvention`).
- **Идентификаторы миграций менять нельзя** (в живой БД они в `__EFMigrationsHistory`); новые миграции создаются в `Budget.Infrastructure`.
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
