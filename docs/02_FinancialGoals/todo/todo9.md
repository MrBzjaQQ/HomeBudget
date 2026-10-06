# Todo 9: [corrective] MigrateDatabaseAsync — пропуск миграций для InMemory

- Агент: backend-developer
- Зависит от: нет (независимый фикс)
- Параллельно с: todo7 (UI-проверка ждёт этот фикс)
- Статус: ☑ выполнено (2026-09-30; агент завис перед отчётом, фикс и проверки выполнены/верифицированы менеджером)

## Контекст
На рабочей машине порт-проксирование Docker Desktop для нового postgres-контейнера сломано (stale-состояние; host → 5432/5433 «болотится», контейнер внутри сети docker healthy). Чтобы не трогать Docker-движок (это задевает работающий продакшен-стек `budget-app-1`), API нужно поднять с InMemory-провайдером: `dotnet run --urls http://localhost:5080 --Database:Provider=InMemory`.

Блокер: конфигурационный переключатель `Database:Provider=InMemory` (documented в `backend/Brief.md` и AGENTS.md) сейчас **не работает вне среды Testing**, потому что `MigrateDatabaseAsync` (`backend/Budget.Infrastructure/DependencyInjection.cs:49-54`) вызывает `db.Database.MigrateAsync()` безоговорочно, а InMemory — не реляционный провайдер:

```
System.InvalidOperationException: Relational-specific methods can only be used when the context is using a relational database provider.
   at Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(...)
   at Budget.Infrastructure.DependencyInjection.MigrateDatabaseAsync(...) in DependencyInjection.cs:line 53
```

(В среде Testing миграции и сид в `Program.cs:102-106` пропускаются, поэтому в тестах это не проявляется.)

## Что сделать
1. Изменить `backend/Budget.Infrastructure/DependencyInjection.cs`, метод `MigrateDatabaseAsync` (строки 49-54): применять миграции только для реляционного провайдера — `if (db.Database.IsRelational()) { await db.Database.MigrateAsync(); }` (EF Core 10: `DatabaseFacade.IsRelational()`). Сид (`SeedDatabaseAsync`) менять не надо — он работает и с InMemory (создаёт admin, категории, счета).
2. Прогнать `cd backend/BudgetApi.Tests && dotnet test` (полный набор) — 0 провалов (регрессия; тесты ходят через InMemory + Testing-среду).
3. Проверить вручную: `cd backend/BudgetApi && dotnet run --no-build --urls http://localhost:5080 --Database:Provider=InMemory --Database:InMemoryName=smoke` — API стартует (сейчас падает с InvalidOperationException из контекста), логин `POST /api/auth/login` admin/admin123 → 200 с токеном; `GET /api/goals` с токеном → 200. После проверки процесс остановить.

## Ограничения
- Править только `MigrateDatabaseAsync` в `DependencyInjection.cs` (минимальное изменение, 1–2 строки).
- Не менять `Program.cs`, `SeedDatabaseAsync`, Npgsql-ветку, тесты (если только не понадобится новый тест — см. ниже).
- ID миграций не трогать.

## Результат (критерий приёмки)
- [ ] `dotnet test` (полный набор) — 0 провалов.
- [ ] `dotnet run --no-build --urls http://localhost:5080 --Database:Provider=InMemory` из `backend/BudgetApi` стартует без InvalidOperationException; `POST /api/auth/login` (admin/admin123) → 200; `GET /api/goals` с токеном → 200 (пустой массив).

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано:
  - `backend/Budget.Infrastructure/DependencyInjection.cs` — `MigrateDatabaseAsync`: миграции применяются только если `db.Database.IsRelational()` (InMemory и прочие нереляционные провайдеры пропускаются); XML-doc комментария обновлён. Единственное изменение.
- Итоги тестов: `dotnet test` (полный набор) — 124 / 124 Passed, 0 Failed (проверено менеджером). Смоук: API с `--Database:Provider=InMemory` стартует, сидер создаёт данные (37 сущностей), `POST /api/auth/login` admin/admin123 → 200 (токен 277 символов), API работает на :5080.
