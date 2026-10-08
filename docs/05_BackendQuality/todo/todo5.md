# Todo 5: Удалить зашитую строку подключения

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo1, todo2, todo3, todo4
- Статус: ☑ выполнено

## Контекст
Реализует FR-5 и FR-6. Сейчас design-time factory использует переменную `BudgetDb` и fallback со строкой подключения (`backend/Budget.Infrastructure/Data/DesignTimeDbContextFactory.cs:11-20`), а runtime читает `ConnectionStrings:Default` (`backend/Budget.Infrastructure/DependencyInjection.cs:23-38`). В tracked `backend/BudgetApi/appsettings.json:2-4` также содержится connection string.

## Что сделать
1. Изменить `DesignTimeDbContextFactory` так, чтобы она разрешала `ConnectionStrings:Default` по стандартному .NET-конфигурационному пути, включая `ConnectionStrings__Default` environment variable, как runtime.
2. При отсутствующем/пустом значении бросать понятный `InvalidOperationException` до создания provider options; текст сообщения не должен содержать секрет.
3. Удалить ключ `ConnectionStrings:Default` из `backend/BudgetApi/appsettings.json`.
4. Проверить `.gitignore`: добавить исключение `.env` только если соответствующего правила реально нет.
5. Собрать `Budget.Infrastructure` и `BudgetApi`; отдельные regression-тесты factory добавляются в todo для тестов.

## Ограничения
- Не добавлять connection string в код, fallback, пример реального секрета или tracked конфигурацию.
- Не менять runtime-конфигурацию Compose, JWT-настройки, provider-переключение или API.

## Результат (критерий приёмки)
- [ ] Design-time factory принимает runtime-совместимый ключ и fail-fast при его отсутствии.
- [ ] В `appsettings.json` отсутствует connection string; `.env` исключён из Git.
- [ ] Инфраструктура и API собираются.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: DesignTimeDbContextFactory читает ConnectionStrings:Default через стандартный .NET configuration и fail-fast при пустом значении; connection string удалена из appsettings; .env исключён из Git. Добавлен Microsoft.Extensions.Hosting.
- Итоги тестов: Infrastructure и BudgetApi build — успешно (0 предупреждений/ошибок каждый); `dotnet test` — 135/135; `git diff --check` и проверка gitignore — успешно.
