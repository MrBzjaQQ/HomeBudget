using Xunit;

namespace BudgetApi.Tests;

public class UserTests : ApiTestBase
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Create_NewUser_CanLoginImmediately()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "ivan", displayName = "Иван", password = "secret1" });
        Assert.Equal(201, created.Status);

        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "ivan", password = "secret1" });
        Assert.Equal(200, login.Status);
        Assert.Equal("Иван", login.Json.GetProperty("user").GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Create_DuplicateUsername_Returns409()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/users", new { username = "admin", displayName = "Дубликат", password = "secret1" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "username");
    }

    [Fact]
    public async Task DeactivatedUser_KeepsHistory_AttributionVisible()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "maria", displayName = "Мария", password = "secret1" });
        var userId = created.Json.GetProperty("id").GetInt32();

        var foodId = await GetCategoryIdAsync("Продукты");
        string dateStr = Today.ToString("yyyy-MM-dd");
        var txn = await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 42, date = dateStr, categoryId = foodId, userId });
        Assert.Equal(201, txn.Status);
        Assert.Equal("Мария", txn.Json.GetProperty("userDisplayName").GetString());

        var deactivated = await SendAsync(HttpMethod.Put, $"/api/users/{userId}", new { isActive = false });
        Assert.Equal(200, deactivated.Status);

        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "maria", password = "secret1" });
        Assert.Equal(401, login.Status);

        var list = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&userId={userId}");
        Assert.Equal(200, list.Status);
        Assert.Equal(1, list.Json.GetProperty("total").GetInt32());
        Assert.Equal("Мария", list.Json.GetProperty("items")[0].GetProperty("userDisplayName").GetString());

        var users = await SendAsync(HttpMethod.Get, "/api/users");
        Assert.Contains(users.Json.EnumerateArray(), u => u.GetProperty("id").GetInt32() == userId && !u.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Update_RenamesUser()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "petr", displayName = "Петр", password = "secret1" });
        var userId = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/users/{userId}", new { displayName = "Петро" });

        Assert.Equal(200, updated.Status);
        Assert.Equal("Петро", updated.Json.GetProperty("displayName").GetString());
    }
}
