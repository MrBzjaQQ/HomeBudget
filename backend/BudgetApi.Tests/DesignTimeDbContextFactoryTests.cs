using Budget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет поведение design-time factory при разных конфигурациях подключения.</summary>
public class DesignTimeDbContextFactoryTests
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Default";
    private static readonly object EnvironmentLock = new();

    /// <summary>Проверяет явную ошибку при отсутствии строки подключения design-time контекста.</summary>
    [Fact]
    public void CreateDbContext_MissingConnectionString_ThrowsConfigurationException()
    {
        lock (EnvironmentLock)
        {
            var previousValue = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, null);

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => new DesignTimeDbContextFactory().CreateDbContext(Array.Empty<string>()));

                Assert.Contains(DatabaseConfiguration.DefaultConnectionStringKey, exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, previousValue);
            }
        }
    }

    /// <summary>Проверяет явную ошибку при пустой строке подключения design-time контекста.</summary>
    [Fact]
    public void CreateDbContext_EmptyConnectionString_ThrowsConfigurationException()
    {
        lock (EnvironmentLock)
        {
            var previousValue = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, string.Empty);

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => new DesignTimeDbContextFactory().CreateDbContext(Array.Empty<string>()));

                Assert.Contains(DatabaseConfiguration.DefaultConnectionStringKey, exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, previousValue);
            }
        }
    }

    /// <summary>Проверяет создание контекста с заданной конфигурацией без открытия соединения с базой.</summary>
    [Fact]
    public void CreateDbContext_ConfiguredConnectionString_CreatesContextWithoutConnecting()
    {
        lock (EnvironmentLock)
        {
            var previousValue = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            Environment.SetEnvironmentVariable(
                ConnectionStringEnvironmentVariable,
                "Host=localhost;Database=design_time_test;Username=design_time_test");

            try
            {
                using var context = new DesignTimeDbContextFactory().CreateDbContext(Array.Empty<string>());

                Assert.NotNull(context);
                Assert.False(context.Database.GetDbConnection().State == System.Data.ConnectionState.Open);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, previousValue);
            }
        }
    }
}
