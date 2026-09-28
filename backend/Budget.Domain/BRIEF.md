# BRIEF: Budget.Domain

## Назначение

Слой доменных сущностей. Чистый C# без каких-либо зависимостей (ни EF Core, ни ASP.NET). Это самый внутренний слой Clean Architecture: от него зависят все остальные слои backend, он не зависит ни от кого.

## Дерево

```
Budget.Domain/
└── Entities/
    ├── User.cs             # учётная запись семьи: username, displayName, passwordHash (BCrypt), isActive
    ├── Category.cs         # группа или подкатегория (parentId), kind, color, sortOrder, isArchived
    ├── Transaction.cs      # операция: kind (Income/Expense), amount > 0, date, note; FK User, Category
    ├── TransactionKind.cs  # enum Income / Expense
    ├── BudgetItem.cs       # плановый расход: month (первое число), categoryId, amount
    ├── SavingsContainer.cs # счёт накоплений: name (уникально), color, sortOrder
    └── SavingsEntry.cs     # запись накопления: month, userId, containerId, amount; unique (month, container, user)
```

## Правила

- Не добавлять сюда зависимости и фреймворчный код.
- Суммы — всегда положительные `decimal`; знак выводится из `TransactionKind`.
- `Month` у `BudgetItem`/`SavingsEntry` — всегда первое число месяца.
- Изменение сущностей требует миграции (см. `../Budget.Infrastructure/BRIEF.md`): `dotnet ef migrations add <Name>` из `../BudgetApi` с `--project ..\Budget.Infrastructure`.

## Навигация

- Выше: `../BRIEF.md` (backend), `../../BRIEF.md` (корень проекта).
- Используется в: `../Budget.Application` (сервисы), `../Budget.Infrastructure` (DbContext, миграции).
