# BRIEF: Budget.Application

## Назначение

Слой бизнес-логики (Application) Clean Architecture. Все правила домена, валидации и построение DTO находятся здесь. WebApi-контроллеры вызывают только интерфейсы сервисов из этого слоя и **никогда** не касаются DbContext.

## Дерево

```
Budget.Application/
├── Common/
│   ├── ApiException.cs     # ApiErrorException: BadRequest/Unauthorized/NotFound/Conflict (int-коды HTTP)
│   └── Validation.cs       # MaxAmount, MaxNoteLength, Today(), IsValidHexColor
├── Security/
│   └── IPasswordHasher.cs  # контракт хеширования (реализация — BCrypt в Infrastructure)
├── Infrastructure/Database/Contract/
│   ├── IGenericRepository.cs  # Query (IQueryable), AddAsync, Update, Remove
│   └── IUnitOfWork.cs         # Users/Categories/Transactions/BudgetItems/SavingsContainers/SavingsEntries + SaveChangesAsync
├── Auth/         # IAuthService: LoginAsync (проверка пароля, isActive), GetCurrentUserAsync
├── Users/        # IUserService: List/Create/Update; UserDto, UserDtos.ToDto, запросы
├── Categories/   # ICategoryService: дерево (ListAsync), Create/Update/Delete (409 при зависимостях)
├── Transactions/ # ITransactionService: ListAsync (фильтры, пагинация, итоги), Summary, CRUD
├── Budget/       # IBudgetService: GetMonthAsync (план/факт по категориям), CRUD
├── Savings/      # ISavingsService: счета CRUD, записи CRUD, SummaryAsync (12 мес., admin исключён)
├── Export/       # IExportService (сборка данных), IExcelExportService (контракт), ExportData
└── DependencyInjection.cs    # AddApplicationServices(): регистрация всех сервисов (scoped)
```

## Правила

- Сервис — `sealed class` с интерфейсом `I*Service`; регистрация в `AddApplicationServices()`.
- Ошибки — только через `ApiErrorException` (Common). Ничего ASP.NET-специфичного не использовать (статусы — int).
- Текущий пользователь передаётся в сервис параметром `currentUserId` (Application не знает про ClaimsPrincipal/JWT).
- Запросы к данным — через `IUnitOfWork`: `uow.Transactions.Query` даёт `IQueryable<Transaction>` (трекинг по умолчанию, как у DbSet); для чистых чтений — `.AsNoTracking()`. Сохранение — `uow.SaveChangesAsync(ct)` после мутаций.
- Слои выше: `../BudgetApi`. Ниже: `../Budget.Domain`. Реализация контрактов БД — в `../Budget.Infrastructure`.

## Навигация

- Выше: `../BRIEF.md` (backend), `../../BRIEF.md` (корень).
- Контракт реализует: `../Budget.Infrastructure` (`UnitOfWork`, `GenericRepository<T>`, `BcryptPasswordHasher`, `ExcelExportService`).
