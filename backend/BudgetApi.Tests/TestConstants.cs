global using Budget.Application.Common;

namespace BudgetApi.Tests;

internal static class TestConstants
{
    public const string DatabaseProviderEnvironmentVariable = "Database__Provider";

    public const string DatabaseInMemoryNameEnvironmentVariable = "Database__InMemoryName";

    public const string FixedGoalDueDate = "2026-12-31";

    public const string September2026 = "2026-09";

    public const string October2026 = "2026-10";

    public const string InvalidMonth = "2026-13";

    public const string InvalidDate = "2026-13-01";
}
