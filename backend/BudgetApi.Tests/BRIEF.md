# BRIEF: BudgetApi.Tests

## Назначение

Интеграционные тесты Web API (xUnit). Гоняют **реальный host** (`WebApplicationFactory<Program>`) с InMemory-БД: HTTP-запрос → контроллер → сервис → EF (InMemory) → JSON. Тесты не требуют запущенной PostgreSQL.

## Дерево

```
BudgetApi.Tests/
├── ApiTestBase.cs          # BudgetApiFactory: env Database__Provider=InMemory + уникальный
│                           #   InMemoryName, окружение Testing, SeedAsync через DI (DbSeeder);
│                           # логин admin/admin123 → Bearer-токен; SendAsync; GetCategoryIdAsync;
│                           # ApiResult, TestAsserts.HasError
├── AuthTests.cs            # login/me, деактивация, неверный пароль
├── UserTests.cs            # CRUD пользователей, уникальность логина
├── CategoryTests.cs        # дерево, наследование kind, 409-сценарии
├── TransactionTests.cs     # фильтры, пагинация, итоги, валидация, 404/400
├── BudgetTests.cs          # план/факт месяца, CRUD позиций
├── SavingsTests.cs         # счета, записи, сводка 12 мес., admin-исключение
└── ExportTests.cs          # чистые тесты ExcelExportService (ClosedXML): листы, итоги
```

## Правила

- Новый сценарий API → новый тест в файл по фиче (или новый файл `*Tests.cs`), наследуя `ApiTestBase`.
- Тестируйте через HTTP (`SendAsync`), а не через сервисы — это интеграционный уровень.
- Ожидайте единый формат ошибок: `code` + `message` (+ `errors` с ключами полей).
- В среде `Testing` авто-миграция/сидер Program.cs не выполняются — сидинг делает `Factory.SeedAsync()`.
- InMemory не проверяет check-ограничения БД и snake_case — это нормально, валидацию делают сервисы.

## Команды

```bash
dotnet test                                        # весь набор
dotnet test --filter "FullyQualifiedName~UserTests" # один класс
```

## Навигация

- Выше: `../BRIEF.md` (backend), `../../BRIEF.md` (корень).
- Тестируемый код: `../BudgetApi` (BRIEF.md там), логика — `../Budget.Application` (BRIEF.md там).
