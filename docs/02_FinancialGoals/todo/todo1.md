# Todo 1: Сущность FinancialGoal + конфигурация DbContext + миграция

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo4
- Статус: ☑ выполнено (в работе → выполнено 2026-09-28)

## Контекст
Первый слой фичи «Финансовые цели» (spec.md, разделы 7, 8). Нужна сущность цели с привязкой к счёту накоплений: `FinancialGoal` ссылается на `SavingsContainer` (`ON DELETE RESTRICT` — счёт с целями не удаляется, FR-7). У цели нет `UpdatedAt` (редактирования нет, spec.md раздел 3) — `TouchUpdated()` не трогаем. Реализует базу для FR-1…FR-7.

## Что сделать
1. Создать `backend/Budget.Domain/Entities/FinancialGoal.cs` — сущность (стиль — как `SavingsEntry.cs` в той же папке):
   - `Id` int (PK);
   - `Name` string — название, обязательно, до 100 символов;
   - `TargetAmount` decimal — целевая сумма, > 0;
   - `ContainerId` int + навигация `Container` (тип `SavingsContainer`);
   - `DueDate` DateOnly? — дата достижения, nullable;
   - `CreatedAt` DateTime.
   - Уникальности по имени НЕТ (две цели могут называться одинаково).
2. Изменить `backend/Budget.Domain/Entities/SavingsContainer.cs` — добавить навигацию `ICollection<FinancialGoal> Goals` (инициализировать пустым списком).
3. Изменить `backend/Budget.Infrastructure/Data/BudgetDbContext.cs` (образец — блок конфигурации `SavingsEntry`, строки 92-108):
   - `DbSet<FinancialGoal> Goals => Set<FinancialGoal>();`;
   - `OnModelCreating`: `Name` — `HasMaxLength(100).IsRequired()`; `TargetAmount` — `HasPrecision(15, 2)` + check-констрейнт `target_amount > 0`; `DueDate` — DateOnly (date); `CreatedAt` — `HasDefaultValueSql("now()")`; FK `Container` → `WithMany(x => x.Goals)`, `DeleteBehavior.Restrict`; индекс по `ContainerId`.
4. Создать миграцию: `cd backend/BudgetApi && dotnet ef migrations add AddFinancialGoals --project ..\Budget.Infrastructure`.
5. Проверить: `dotnet build` (из `backend/BudgetApi`) без ошибок; `dotnet ef migrations has-pending-model-changes --project ..\Budget.Infrastructure` (из `backend/BudgetApi`) — модель и миграция синхронны.

## Ограничения
- Clean Architecture: `Budget.Domain` не зависит ни от кого; конфигурация — только в `Budget.Infrastructure`.
- snake_case-имена в БД даёт `UseSnakeCaseNamingConvention` автоматически — в коде миграции не переименовывать вручную.
- Идентификаторы и содержимое существующих миграций (`InitialCreate`, `AddBudgetItems`, `AddSavings`) НЕ менять.
- Не добавлять `UpdatedAt` и не трогать `TouchUpdated()`.
- Не менять существующие сущности, кроме навигации в `SavingsContainer`.

## Результат (критерий приёмки)
- [ ] `backend/Budget.Domain/Entities/FinancialGoal.cs` существует со всеми полями из раздела «Что сделать».
- [ ] `SavingsContainer` содержит `Goals`; `BudgetDbContext` — DbSet и конфигурацию `FinancialGoal` (FK Restrict, check > 0, индекс по `container_id`).
- [ ] Миграция `AddFinancialGoals` создана: таблица `financial_goals` (`id` identity PK, `name varchar(100) not null`, `target_amount numeric(15,2) not null` + check, `container_id int not null` FK `ON DELETE RESTRICT`, `due_date date null`, `created_at timestamptz not null default now()`, индекс по `container_id`).
- [ ] `dotnet build` из `backend/BudgetApi` — 0 ошибок; `has-pending-model-changes` — нет незакрытых изменений модели.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано:
  - Созданы: `backend/Budget.Domain/Entities/FinancialGoal.cs`; миграция `20260928135240_AddFinancialGoals.cs` (+Designer) — таблица `financial_goals` (id identity PK, name varchar(100) not null, target_amount numeric(15,2) not null + check `target_amount > 0`, container_id int not null FK ON DELETE RESTRICT, due_date date null, created_at timestamptz not null default now(), индекс по container_id).
  - Изменены: `SavingsContainer.cs` (навигация `Goals`), `BudgetDbContext.cs` (DbSet + конфигурация, FK Restrict), `BudgetDbContextModelSnapshot.cs` (EF).
  - Обоснованное отклонение: DbSet назван `FinancialGoals` (вместо `Goals` из текста todo) — иначе конвенция дала бы таблицу `goals`, а критерий приёмки требует `financial_goals`.
  - Существующие миграции и `TouchUpdated()` не тронуты.
- Итоги тестов: `dotnet build` — 0 ошибок/0 предупреждений; `has-pending-model-changes` — «No changes have been made to the model»; `dotnet test` — Passed: 104 / 104.
