# BRIEF: BudgetApi (Web API)

## Назначение

HTTP-слой (Presentation) Clean Architecture. Тонкие контроллеры, JWT-авторизация, middleware обработки ошибок, настройка host (Program.cs), Swagger, раздача собранного SPA. **Не содержит бизнес-логики и не использует DbContext напрямую** — только сервисы из `../Budget.Application`.

## Дерево

```
BudgetApi/
├── Program.cs               # bootstrap: Serilog, JSON (camelCase, enum → UPPER),
│                            # InvalidModelStateResponseFactory, Swagger, JWT, CORS,
│                            # MigrateDatabaseAsync + SeedDatabaseAsync (не в Testing),
│                            # static files + SPA-fallback (wwwroot)
├── Controllers/             # TransactionsController, CategoriesController, BudgetController,
│                            # SavingsController, UsersController, ExportController
├── Auth/
│   ├── AuthController.cs    # POST /api/auth/login, GET /api/auth/me
│   ├── JwtOptions.cs        # секция "Jwt" (Key, Issuer, Audience, Lifetime)
│   ├── JwtTokenService.cs   # выдача JWT (claim sub/username/display_name)
│   └── CurrentUserExtensions.cs  # ClaimsPrincipal.GetUserId() (claim "sub")
├── Common/
│   ├── ApiError.cs          # payload ошибок {"code","message","errors"}
│   ├── ExceptionHandlingMiddleware.cs  # ApiErrorException → JSON
│   └── ModelStateSanitizer.cs          # «сырые» сообщения model binding → дружелюбные (RU)
└── appsettings.json         # ConnectionStrings:Default, Jwt, Cors, Logging
```

## Правила

- Контроллер: DI-конструктор с интерфейсами сервисов; метод = аргументы запроса → вызов сервиса → `Ok/NoContent/StatusCode(201, dto)`. Пустое тело (`request is null`) → `ApiErrorException.BadRequest("Пустое тело запроса")`.
- ID текущего пользователя в сервисах, зависящих от пользователя (Transactions.Create/Update, Savings.Create/Update) — `User.GetUserId()`.
- JSON: camelCase; перечисления — строки ВЕРХНИМ регистром (`JsonStringEnumConverter` + `UpperCaseNamingPolicy`). Не менять — frontend зависит.
- 401 без токена — JSON `{"code":"Unauthorized",...}` (OnChallenge в JWT-настройках).
- Swagger — только в `Development`.
- Продакшен: API раздаёт SPA из `wwwroot` (содержимое `frontend/dist/budget-web/browser`); без сборки SPA — 404-сообщение.

## Команды

```bash
dotnet run --urls http://localhost:5080     # ОБЯЗАТЕЛЬНО порт 5080 (прокси фронтенда)
dotnet ef migrations add <Name> --project ..\Budget.Infrastructure
dotnet ef migrations has-pending-model-changes --project ..\Budget.Infrastructure
```

## Навигация

- Выше: `../BRIEF.md` (backend), `../../BRIEF.md` (корень).
- Сервисы: `../Budget.Application` (BRIEF.md там). Инфраструктура: `../Budget.Infrastructure` (BRIEF.md там). Тесты: `../BudgetApi.Tests` (BRIEF.md там).
