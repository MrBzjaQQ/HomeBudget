# Домашний бюджет

Приложение для учёта семейного бюджета: доходы/расходы, категории, аналитика, экспорт в Excel.

**Стек:** Angular 22 (signals, zoneless, Tailwind, Chart.js) · ASP.NET Core 10 Web API · PostgreSQL 16 · ClosedXML · Docker.

## Возможности

- Журнал операций с фильтрами (период, кто, тип, категории, поиск), итогами и пагинацией
- Формы операции: дата не в будущем, суммы до 999 999 999 999,99 ₽, последний выбранный тип/категория запоминаются
- Категории: группы + подкатегории, цвета, архив, порядок
- Аналитика: пир расходов по группам, динамика 6 месяцев, топ-5 категорий
- Экспорт в Excel на сервере: месяц (1 лист) или год (13 листов: 12 месяцев + «Сводка»)
- Аккаунты семьи: добавление, переименование, смена пароля, деактивация (история сохраняется)
- JWT-авторизация (24 ч), BCrypt-пароли, логи в файл (Serilog)

## Быстрый старт (Docker)

```bash
cp .env.example .env
# в .env задайте JWT_KEY (openssl rand -hex 32) и POSTGRES_PASSWORD
docker compose up -d --build
```

Приложение: `http://localhost:8080`. Первый пользователь создаётся автоматически: **admin / admin123** (смените пароль в «Настройках»).

### Резервные копии

```bash
chmod +x scripts/backup.sh
scripts/backup.sh          # дамп в backups/, хранятся 7 последних
```

Cron (ежедневно в 03:00):

```cron
0 3 * * * /opt/budget/scripts/backup.sh >> /var/log/budget-backup.log 2>&1
```

Для копирования дампов в Nextcloud раскомментируйте `rsync`-строку в `scripts/backup.sh`.

Восстановление:

```bash
gunzip -c backups/budget-<stamp>.sql.gz | docker compose exec -T postgres psql -U budget budget
```

## Разработка

Требования: .NET SDK 10, Node 22+, Docker (для PostgreSQL).

1. База данных:

   ```bash
   docker run -d --name budget-pg -e POSTGRES_DB=budget -e POSTGRES_USER=budget -e POSTGRES_PASSWORD=budget -p 5432:5432 postgres:16-alpine
   ```

2. Backend (Swagger: `http://localhost:5080/swagger` в Development):

   ```bash
   cd backend/BudgetApi
   dotnet run --urls http://localhost:5080
   ```

3. Frontend (прокси `/api` → `localhost:5080`):

   ```bash
   cd frontend
   npm install
   npm start          # http://localhost:4200
   ```

Миграции (при изменении схемы):

```bash
cd backend/BudgetApi
dotnet ef migrations add <Name>
```

## Тесты

```bash
cd backend/BudgetApi.Tests && dotnet test     # 34 API-теста (InMemory)
cd frontend && npx ng test                    # 28 тестов (vitest + jsdom)
```

## Структура

```
backend/BudgetApi          Web API: контроллеры, EF Core, JWT, Excel (ClosedXML), Serilog
backend/BudgetApi.Tests    xUnit-тесты API (WebApplicationFactory, InMemory)
frontend                   Angular 22: features (journal, stats, categories, export, settings, …)
scripts/backup.sh          pg_dump + ротация (7 последних)
Dockerfile                 3 стадии: frontend → backend publish → aspnet runtime
docker-compose.yml         postgres 16 + app (порт 8080)
```

## Безопасность

- Доступ только из домашней сети: порты наружу не публикуются (firewall сервера).
- `JWT_KEY` и пароль БД — только в `.env` (не коммитить).
- Пароли — BCrypt; JWT живут 24 часа; неактивные пользователи не могут войти, их история видна.
