# BRIEF: Budget.Infrastructure

## Назначение

Слой инфраструктуры (Infrastructure) Clean Architecture: всё, что связано с конкретными технологиями — EF Core, PostgreSQL/InMemory, BCrypt, ClosedXML. Реализует контракты из `../Budget.Application` (`IUnitOfWork`, `IGenericRepository<T>`, `IPasswordHasher`, `IExcelExportService`).

## Дерево

```
Budget.Infrastructure/
├── Data/
│   ├── BudgetDbContext.cs          # модель EF; оверрайд SaveChanges(Async) → UpdatedAt
│   ├── DbSeeder.cs                 # сидер (admin/admin123, категории, счета); DI-класс
│   ├── DesignTimeDbContextFactory.cs # для dotnet-ef (чтение env BudgetDb)
│   └── Migrations/                 # 3 миграции + snapshot (snake_case)
├── Repositories/
│   ├── GenericRepository.cs        # обёртка над DbSet<T>: Query, AddAsync, Update, Remove
│   └── UnitOfWork.cs               # 6 репозиториев + SaveChangesAsync
├── Security/
│   └── BcryptPasswordHasher.cs
├── Export/
│   └── ExcelExportService.cs       # ClosedXML: BuildMonthFile / BuildYearFile
└── DependencyInjection.cs          # AddDatabase(IConfiguration), MigrateDatabaseAsync(), SeedDatabaseAsync()
```

## Правила

- **Идентификаторы миграций (`2026..._Name`) менять нельзя** — в живой БД они в `__EFMigrationsHistory`. Новая миграция: `cd ../BudgetApi && dotnet ef migrations add <Name> --project ..\Budget.Infrastructure`.
- Имена таблиц/колонн — snake_case (`UseSnakeCaseNamingConvention`), индексы/ключи — `pk_`, `fk_`, `idx_`, `uq_`, `chk_`.
- Выбор провайдера БД: `Database:Provider` (`Npgsql` / `InMemory`), строка подключения — `ConnectionStrings:Default` (обязательна только для Npgsql).
- Конфигурация модели (precision, check-ограничения, индексы) — в `OnModelCreating` `BudgetDbContext`.
- Сидер — DI-класс (`AddScoped<DbSeeder>`), вызывается `SeedDatabaseAsync()` при старте (кроме среды `Testing`).

## Навигация

- Выше: `../BRIEF.md` (backend), `../../BRIEF.md` (корень).
- Контракты, которые реализует: `../Budget.Application/Infrastructure/Database/Contract/`, `../Budget.Application/Security/`, `../Budget.Application/Export/IExcelExportService.cs`.
