# Todo 3: Разнести DTO накоплений, целей и экспорта

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo1, todo2, todo4, todo5
- Статус: ☑ выполнено

## Контекст
Реализует FR-1 и FR-2 для Savings, Goals и Export (см. `spec.md`, разделы 1, 7 и 8). Источники типов: `backend/Budget.Application/Savings/SavingsDtos.cs`, `Goals/GoalsDtos.cs`, `Export/ExportModels.cs`.

## Что сделать
1. Перенести каждый из 11 типов `CreateSavingsContainerRequest`, `UpdateSavingsContainerRequest`, `SavingsContainerDto`, `CreateSavingsEntryRequest`, `UpdateSavingsEntryRequest`, `SavingsEntryDto`, `SavingsMonthResponse`, `SavingsMonthAmount`, `SavingsMonthDelta`, `SavingsMemberTotal`, `SavingsSummary` в отдельный файл `Savings/{ИмяТипа}.cs`.
2. Перенести `CreateGoalRequest`, `UpdateGoalRequest`, `GoalDto` в отдельные файлы `Goals/{ИмяТипа}.cs`.
3. Перенести `ExportTransactionRow`, `ExportData` в отдельные файлы `Export/{ИмяТипа}.cs`.
4. Все DTO-record без наследников сделать `sealed`; обновить ссылки и проверить сборку Application: `dotnet build` из `backend/Budget.Application`.
5. Удалить старые мульти-типовые файлы после переноса.

## Ограничения
- Не менять контракт, свойства, nullability, типы полей, порядок параметров позиционных записей и формирование данных.
- Не добавлять XML-doc или менять бизнес-логику.

## Результат (критерий приёмки)
- [ ] Для каждого перечисленного типа существует отдельный файл; record sealed при отсутствии наследования.
- [ ] Application собирается без ошибок.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: DTO Savings, Goals и Export перенесены в отдельные файлы (16 типов); records sealed; исходные общие файлы удалены.
- Итоги тестов: `dotnet build` Budget.Application — успешно (0 предупреждений, 0 ошибок); `dotnet test` BudgetApi.Tests — 135/135 пройдено; `git diff --check` — успешно.
