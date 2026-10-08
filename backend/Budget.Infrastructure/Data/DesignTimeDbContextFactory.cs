using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Hosting;

namespace Budget.Infrastructure.Data;

/// <summary>
/// Создаёт контекст для инструментов EF Core без запуска полноценного приложения.
/// Строка подключения считывается из стандартного ключа конфигурации приложения.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    /// <summary>Создаёт контекст PostgreSQL для выполнения команд EF Core во время разработки.</summary>
    /// <param name="args">Аргументы командной строки, используемые при построении конфигурации.</param>
    /// <returns>Контекст с настройками PostgreSQL и snake_case-именованием.</returns>
    public BudgetDbContext CreateDbContext(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var connectionString = builder.Configuration[DatabaseConfiguration.DefaultConnectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{DatabaseConfiguration.DefaultConnectionStringKey} is not configured. Set it in configuration or through the ConnectionStrings__Default environment variable.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BudgetDbContext>();
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
        return new BudgetDbContext(optionsBuilder.Options);
    }
}
