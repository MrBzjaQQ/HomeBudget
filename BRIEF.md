# BRIEF: Домашний бюджет

## Бизнес-назначение

Веб-приложение для семейного учёта финансов в домашней сети:

- **Журнал операций** — доходы/расходы по категориям (группа → подкатегория), пагинация, фильтры (период, категория, кто, поиск), сортировка.
- **Аналитика** — сводка по категориям и месяцам (Chart.js).
- **Плановый бюджет** — план по категориям расходов на месяц, сравнение «план/факт».
- **Накопления** — общие счета семьи, записи помесячно по каждому участнику, сводка за 12 месяцев.
- **Категории** — CRUD групп и подкатегорий, архив, цвета, сортировка.
- **Экспорт в Excel** (.xlsx, ClosedXML) — файл за месяц или за год.
- **Пользователи** — учётные записи семьи (активация/деактивация, смена пароля), JWT-авторизация.

Стек: Angular 22 (signals, zoneless, Tailwind 4) + ASP.NET Core 10 Web API (Clean Architecture) + PostgreSQL 16 (EF Core, snake_case).

## Архитектура

```
frontend (Angular SPA)
   │  HTTP/JSON, Bearer JWT, прокси /api → :5080 (dev)
   ▼
BudgetApi (WebApi, контроллеры + JWT + middleware)
   │  вызовы сервисов через интерфейсы
   ▼
Budget.Application (сервисы-фасады: бизнес-логика, валидация, DTO)
   │  контракты IUnitOfWork / IGenericRepository
   ▼
Budget.Infrastructure (EF Core: BudgetDbContext, репозитории, миграции, сидер, BCrypt, Excel)
   │
   ▼
Budget.Domain (сущности: User, Category, Transaction, BudgetItem, SavingsContainer, SavingsEntry)
```

Правило зависимостей: Domain → (не зависит ни от кого); Application → Domain; Infrastructure → Application + Domain; BudgetApi → Application + Infrastructure. Контроллеры не касаются DbContext.

## Дерево проекта

```
├── backend/                # .NET (BRIEF.md в этой папке)
│   ├── Budget.Domain/      # сущности (BRIEF.md)
│   ├── Budget.Application/ # сервисы, DTO, контракты (BRIEF.md)
│   ├── Budget.Infrastructure/ # EF Core, миграции, репозитории (BRIEF.md)
│   ├── BudgetApi/          # Web API: контроллеры, JWT, Program.cs (BRIEF.md)
│   └── BudgetApi.Tests/    # xUnit-тесты API (BRIEF.md)
├── frontend/               # Angular (BRIEF.md в этой папке)
│   └── src/                # BRIEF.md; src/app/ — BRIEF.md
├── docs/                   # Master-спецификация, ТЗ и артефакты Spec-Driven-Design (BRIEF.md)
├── examples/               # эталон Clean Architecture (LotDocumentationStatistics)
├── k8s/, scripts/          # деплой и утилиты
├── Dockerfile, docker-compose.yml  # продакшен: один контейнер (API + SPA + БД отдельно)
└── .opencode/              # агенты и навыки OpenCode для этого проекта
```

## Ключевые договорённости (JSON-контракт)

- Перечисления — строки ВЕРХНИМ регистром: `"kind": "EXPENSE"`.
- Ошибки — `{ "code": "...", "message": "...", "errors": { "поле": "текст" } }`; коды: `ValidationFailed` (400), `Unauthorized` (401), `NotFound` (404), `Conflict` (409), `InternalServerError` (500).
- JWT — 24 часа, claim `sub` = id пользователя; деактивированный пользователь не входит, но его история видна.
- Суммы хранятся положительными (знак выводится из `kind`), `DECIMAL(15,2)`.

## Где находятся BRIEF.md

| Папка | Файл |
| --- | --- |
| корень проекта | `BRIEF.md` (этот файл) |
| backend | `backend/BRIEF.md` |
| backend/Budget.Domain | `backend/Budget.Domain/BRIEF.md` |
| backend/Budget.Application | `backend/Budget.Application/BRIEF.md` |
| backend/Budget.Infrastructure | `backend/Budget.Infrastructure/BRIEF.md` |
| backend/BudgetApi | `backend/BudgetApi/BRIEF.md` |
| backend/BudgetApi.Tests | `backend/BudgetApi.Tests/BRIEF.md` |
| frontend | `frontend/BRIEF.md` |
| frontend/src | `frontend/src/BRIEF.md` |
| frontend/src/app | `frontend/src/app/BRIEF.md` |
| docs | `docs/BRIEF.md` |

## Команды (кратко, подробно — в AGENTS.md)

```bash
# БД
docker run -d --name budget-pg -e POSTGRES_DB=budget -e POSTGRES_USER=budget -e POSTGRES_PASSWORD=budget -p 5432:5432 postgres:16-alpine
# API — порт 5080
cd backend/BudgetApi && dotnet run --urls http://localhost:5080
# Frontend
cd frontend && npm install && npm start
# Тесты
cd backend/BudgetApi.Tests && dotnet test
cd frontend && npx ng test
```
