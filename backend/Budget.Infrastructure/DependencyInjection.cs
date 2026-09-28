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
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["Database:Provider"] ?? "Npgsql";

        services.AddDbContext<BudgetDbContext>(options =>
        {
            if (dbProvider == "InMemory")
            {
                options.UseInMemoryDatabase(configuration["Database:InMemoryName"] ?? "budget-test");
            }
            else
            {
                var connectionString = configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
            }
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DbSeeder>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();

        return services;
    }

    /// <summary>Применяет миграции БД (схема обновляется автоматически при старте API).</summary>
    public static async Task MigrateDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Заполняет пустую базу стартовыми данными (администратор, категории, счета).</summary>
    public static async Task SeedDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(db);
    }
}
