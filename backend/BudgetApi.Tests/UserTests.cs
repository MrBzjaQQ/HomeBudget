using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет HTTP-сценарии управления пользователями.</summary>
public class UserTests : ApiTestBase
{
    private static DateOnly Today => Validation.Today();

    [Fact]
    /// <summary>Проверяет возможность входа сразу после создания пользователя.</summary>
    public async Task CreateUser_ValidRequest_AllowsImmediateLogin()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "ivan", displayName = "Иван", password = "secret1" });
        Assert.Equal(201, created.Status);

        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "ivan", password = "secret1" });
        Assert.Equal(200, login.Status);
        Assert.Equal("Иван", login.Json.GetProperty("user").GetProperty("displayName").GetString());
    }

    [Fact]
    /// <summary>Проверяет ошибку валидации при создании пользователя с занятым логином.</summary>
    public async Task CreateUser_DuplicateUsername_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/users", new { username = SystemUsernames.Admin, displayName = "Дубликат", password = "secret1" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "username");
    }

    [Fact]
    /// <summary>Проверяет сохранение видимой атрибуции истории после деактивации пользователя.</summary>
    public async Task DeactivateUser_WithExistingTransactions_KeepsHistoryAttributionVisible()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "maria", displayName = "Мария", password = "secret1" });
        var userId = created.Json.GetProperty("id").GetInt32();

        var foodId = await GetCategoryIdAsync("Продукты");
        string dateStr = Today.ToString(DateFormats.Date);
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
    /// <summary>Проверяет изменение отображаемого имени пользователя.</summary>
    public async Task UpdateUser_WithNewDisplayName_ReturnsUpdatedUser()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "petr", displayName = "Петр", password = "secret1" });
        var userId = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/users/{userId}", new { displayName = "Петро" });

        Assert.Equal(200, updated.Status);
        Assert.Equal("Петро", updated.Json.GetProperty("displayName").GetString());
    }
}
