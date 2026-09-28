# BRIEF: backend

## Назначение

.NET-часть приложения «Домашний бюджет»: Web API на ASP.NET Core 10, организованная по **Clean Architecture**. Общий BRIEF проекта — `../BRIEF.md`, описание архитектуры — там же.

## Слои и зависимости

| Проект | Роль | Зависит от |
| --- | --- | --- |
| `Budget.Domain` | Сущности и перечисления. Чистый C#, без зависимостей. | — |
| `Budget.Application` | Сервисы-фасады: бизнес-логика, валидация, DTO, контракты репозиториев. | Domain |
| `Budget.Infrastructure` | EF Core: контекст, миграции, репозитории, Unit of Work, сидер, BCrypt, Excel. | Application, Domain |
| `BudgetApi` | HTTP-слой: контроллеры (тонкие), JWT, middleware, Program.cs, Swagger. | Application, Infrastructure |
| `BudgetApi.Tests` | Интеграционные xUnit-тесты API (WebApplicationFactory, InMemory). | BudgetApi |

Контроллеры **не** получают `BudgetDbContext` — только интерфейсы сервисов из `Budget.Application`.

## Дерево

```
backend/
├── Budget.Domain/
│   └── Entities/          # User, Category, Transaction, TransactionKind,
│                          # BudgetItem, SavingsContainer, SavingsEntry
├── Budget.Application/
│   ├── Common/            # ApiErrorException, Validation (лимиты, Today())
│   ├── Security/          # IPasswordHasher
│   ├── Infrastructure/Database/Contract/  # IGenericRepository<T>, IUnitOfWork
│   ├── Auth/              # IAuthService, AuthService, DTO (Login/Me)
│   ├── Users/             # IUserService, UserService, UserDto
│   ├── Categories/        # ICategoryService, CategoryService, DTO
│   ├── Transactions/      # ITransactionService, TransactionService, DTO
│   ├── Budget/            # IBudgetService, BudgetService, DTO
│   ├── Savings/           # ISavingsService, SavingsService, DTO
│   ├── Export/            # IExportService, ExportService, IExcelExportService, ExportData
│   └── DependencyInjection.cs   # AddApplicationServices()
├── Budget.Infrastructure/
│   ├── Data/              # BudgetDbContext, DbSeeder, DesignTimeDbContextFactory
│   │   └── Migrations/    # 3 миграции (snake_case; ID не менять!)
│   ├── Repositories/      # GenericRepository<T>, UnitOfWork
│   ├── Security/          # BcryptPasswordHasher
│   ├── Export/            # ExcelExportService (ClosedXML)
│   └── DependencyInjection.cs   # AddDatabase(), MigrateDatabaseAsync(), SeedDatabaseAsync()
├── BudgetApi/
│   ├── Controllers/       # AuthController в Auth/, остальные здесь
│   ├── Auth/              # JwtOptions, JwtTokenService, CurrentUserExtensions, AuthController
│   ├── Common/            # ApiError, ExceptionHandlingMiddleware, ModelStateSanitizer
│   ├── Program.cs         # Serilog, JSON (enum → UPPER), Swagger, JWT, CORS, SPA-fallback
│   └── appsettings.json
└── BudgetApi.Tests/
    ├── ApiTestBase.cs     # BudgetApiFactory (InMemory), логин admin/admin123, SendAsync
    └── {Auth,User,Category,Transaction,Budget,Savings,Export}Tests.cs
```

## Ключевые механизмы

- **Старт API**: `MigrateDatabaseAsync()` + `SeedDatabaseAsync()` (пропуск в среде `Testing`). Сидер создаёт `admin/admin123`, категории, счета накоплений.
- **Провайдер БД**: `Database:Provider` = `Npgsql` (по умолчанию) или `InMemory` (тесты; имя — `Database:InMemoryName`). Переключается в `AddDatabase()`.
- **snake_case**: `UseSnakeCaseNamingConvention` только для Npgsql.
- **Автоматическое `UpdatedAt`**: оверрайд `SaveChanges(Async)` в `BudgetDbContext` проставляет `UpdatedAt` у изменённых `Transaction`/`BudgetItem`/`SavingsEntry`.
- **Ошибки**: сервисы бросают `ApiErrorException` (Application/Common); `ExceptionHandlingMiddleware` (WebApi) превращает её в JSON `{code, message, errors}`.
- **Текущий пользователь**: `User.GetUserId()` (claim `sub`) — контроллер передаёт id в сервис как параметр (Application не знает про ClaimsPrincipal).

## Команды

```bash
cd backend/BudgetApi && dotnet run --urls http://localhost:5080   # API (обязательно 5080)
cd backend/BudgetApi.Tests && dotnet test                          # тесты (InMemory, БД не нужна)
cd backend/BudgetApi && dotnet ef migrations add <Name> --project ..\Budget.Infrastructure   # новая миграция
```

Новые миграции создаются в `Budget.Infrastructure` (там контекст и дизайн-фабрика); запускайте EF CLI из `BudgetApi` (startup-проект). **Идентификаторы существующих миграций менять нельзя** — в живой БД они записаны в `__EFMigrationsHistory`.
