# Todo 4: Разнести конфигурации EF сущностей

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo1, todo2, todo3, todo5
- Статус: ☑ выполнено

## Контекст
Реализует FR-3 и FR-10. Сейчас конфигурации семи сущностей находятся в `backend/Budget.Infrastructure/Data/BudgetDbContext.cs:22-124`; `UpdatedAt` управляется отдельно в `:126-164`.

## Что сделать
1. Создать отдельный `IEntityTypeConfiguration<TEntity>` в `backend/Budget.Infrastructure/Data/Configurations/` для `User`, `Category`, `Transaction`, `BudgetItem`, `SavingsContainer`, `SavingsEntry`, `FinancialGoal`.
2. Перенести настройки каждой сущности из `BudgetDbContext.OnModelCreating` в её конфигурацию и зарегистрировать все конфигурации в контексте.
3. Оставить DbSet, `SaveChanges`/`SaveChangesAsync` и `UpdatedAt`-логику в `BudgetDbContext` без функциональных изменений.
4. Запустить `dotnet build` из `backend/Budget.Infrastructure` и `dotnet ef migrations has-pending-model-changes` из `backend/BudgetApi` с `--project ..\Budget.Infrastructure` (если для команды доступна требуемая среда), зафиксировать оба результата.

## Ограничения
- Сохранить все precision, индексы и их имена, check constraints, связи/delete behavior, default-значения и типы enum.
- Не менять сущности, миграции, snapshot и схему данных.

## Результат (критерий приёмки)
- [ ] Созданы семь отдельных конфигураций; OnModelCreating не содержит прежний inline-маппинг.
- [ ] Сборка успешна; модель не показывает непредусмотренных изменений относительно миграционного snapshot.
- [ ] `UpdatedAt` сохраняет текущее поведение.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: добавлены семь IEntityTypeConfiguration; BudgetDbContext применяет их из сборки, DbSet/UpdatedAt сохранены; миграции не менялись.
- Итоги тестов: `dotnet build` Infrastructure — 0 предупреждений/ошибок; `dotnet test` — 135/135; `has-pending-model-changes` — изменений модели нет (предупреждение: EF tools 10.0.5 старее runtime 10.0.11).
