# Todo 2: Разнести DTO операций и бюджета

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo1, todo3, todo4, todo5
- Статус: ☑ выполнено

## Контекст
Реализует FR-1 и FR-2 для Transactions и Budget (см. `spec.md`, разделы 1, 7 и 8). Источники типов: `backend/Budget.Application/Transactions/TransactionDtos.cs` и `backend/Budget.Application/Budget/BudgetDtos.cs`.

## Что сделать
1. Перенести `TransactionDto`, `TransactionListResponse`, `TotalsDto`, `CreateTransactionRequest`, `SummaryCategoryDto`, `SummaryMonthDto`, `SummaryResponse` — каждый тип в отдельный файл `Transactions/{ИмяТипа}.cs`.
2. Перенести `CreateBudgetItemRequest`, `BudgetItemDto`, `BudgetCategoryDto`, `BudgetTotalsDto`, `BudgetMonthResponse` — каждый тип в отдельный файл `Budget/{ИмяТипа}.cs`.
3. Все DTO-record без наследников сделать `sealed`; проверить ссылки, namespaces и сборку Application.
4. Удалить старые `TransactionDtos.cs` и `BudgetDtos.cs` после переноса. Запустить `dotnet build` из `backend/Budget.Application`.

## Ограничения
- Не менять JSON-контракт, имена/типы свойств, nullability, конструкторы и поведение сериализации.
- Не добавлять здесь XML-doc и не менять бизнес-логику.

## Результат (критерий приёмки)
- [ ] Каждый перечисленный тип объявлен в самостоятельном файле; DTO-record sealed, если не базовый.
- [ ] Application собирается без ошибок.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: DTO Transactions и Budget перенесены по одному типу на файл; records sealed; старые мульти-типовые файлы удалены.
- Итоги тестов: `dotnet build` Budget.Application — успешно (0 предупреждений, 0 ошибок); `dotnet test` BudgetApi.Tests — 135/135 пройдено.
