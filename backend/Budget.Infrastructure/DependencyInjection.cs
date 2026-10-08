using System.Globalization;
using Budget.Application.Export;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Application.Security;
using Budget.Infrastructure.Data;
using Budget.Infrastructure.Export;
using Budget.Infrastructure.Repositories;
using Budget.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Budget.Infrastructure;

/// <summary>Методы-расширения для настройки базы данных и связанных служб.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет контекст БД, Unit of Work и службы инфраструктуры.
    /// Провайдер БД выбирается конфигурацией Database:Provider (Npgsql по умолчанию / InMemory в тестах).
    /// </summary>
    /// <param name="services">Коллекция служб, в которую добавляются реализации инфраструктуры.</param>
    /// <param name="configuration">Конфигурация с провайдером, именем InMemory-базы или строкой подключения.</param>
    /// <returns>Та же коллекция служб для продолжения настройки.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration[DatabaseConfiguration.ProviderKey] ?? DatabaseConfiguration.NpgsqlProvider;

        services.AddDbContext<BudgetDbContext>(options =>
        {
            if (dbProvider == DatabaseConfiguration.InMemoryProvider)
            {
                options.UseInMemoryDatabase(configuration[DatabaseConfiguration.InMemoryNameKey] ?? "budget-test");
            }
            else
            {
                var connectionString = configuration[DatabaseConfiguration.DefaultConnectionStringKey]
                    ?? throw new InvalidOperationException($"{DatabaseConfiguration.DefaultConnectionStringKey} is not configured");
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
            }
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DbSeeder>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();

        return services;
    }

    /// <summary>Применяет миграции к реляционной базе данных; для InMemory-провайдера операция пропускается.</summary>
    /// <param name="app">Хост, предоставляющий настроенный контейнер служб.</param>
    /// <returns>Асинхронная операция применения миграций.</returns>
    public static async Task MigrateDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
    }

    /// <summary>Заполняет базу начальными данными, если соответствующие таблицы ещё пусты.</summary>
    /// <param name="app">Хост, из контейнера которого извлекаются контекст и сидер.</param>
    /// <returns>Асинхронная операция заполнения начальных данных.</returns>
    public static async Task SeedDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(db);
    }
}
