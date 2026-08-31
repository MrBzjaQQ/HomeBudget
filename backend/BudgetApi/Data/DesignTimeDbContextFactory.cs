using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetApi.Data;

/// <summary>
/// Lets tooling (dotnet-ef) create a context without running the full host,
/// so migrations do not attempt to connect to a real database.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    public BudgetDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("BudgetDb")
            ?? "Host=localhost;Port=5432;Database=budget;Username=budget;Password=***";
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
        return new BudgetDbContext(optionsBuilder.Options);
    }
}
