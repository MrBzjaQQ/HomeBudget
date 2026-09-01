using System.Text;
using System.Text.Json;
using Xunit;

namespace BudgetApi.Tests;

public class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "wrong" });

        Assert.Equal(401, result.Status);
        Assert.Equal("Unauthorized", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Login_WithDeactivatedUser_Returns401()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username = "temp", displayName = "Темп", password = "secret1" });
        Assert.Equal(201, created.Status);
        var userId = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/users/{userId}", new { isActive = false });
        Assert.Equal(200, updated.Status);

        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "temp", password = "secret1" });
        Assert.Equal(401, result.Status);
    }

    [Fact]
    public async Task Login_Success_ReturnsTokenAndUser()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "admin123" });

        Assert.Equal(200, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty<string>("token")));
        Assert.Equal("admin", result.Json.GetProperty("user").GetProperty("username").GetString());
    }

    [Fact]
    public async Task Login_Success_ReturnsServerDateInTodayFormat()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "admin123" });

        Assert.Equal(200, result.Status);
        var serverDate = result.GetProperty<string>("serverDate")!;
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", serverDate);
        Assert.Equal(BudgetApi.Common.Validation.Today().ToString("yyyy-MM-dd"), serverDate);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/auth/me");

        Assert.Equal(200, result.Status);
        Assert.Equal("admin", result.Json.GetProperty("user").GetProperty("username").GetString());
        var serverDate = result.GetProperty<string>("serverDate")!;
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", serverDate);
        Assert.Equal(BudgetApi.Common.Validation.Today().ToString("yyyy-MM-dd"), serverDate);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401_WithJsonEnvelope()
    {
        using var anonymous = Factory.CreateClient();
        var result = await anonymous.GetAsync("/api/auth/me");
        var raw = await result.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Unauthorized", JsonDocument.Parse(raw).RootElement.GetProperty("code").GetString());
        Assert.Contains("Требуется авторизация", raw);
    }

    [Fact]
    public async Task Login_TokenLifetime_IsExactly24Hours()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "admin123" });
        var token = result.GetProperty<string>("token")!;
        var payload = token.Split('.')[1];
        payload += new string('=', (4 - payload.Length % 4) % 4);
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = JsonDocument.Parse(json);

        var exp = doc.RootElement.GetProperty("exp").GetInt64();
        var iat = doc.RootElement.GetProperty("iat").GetInt64();

        Assert.Equal(86_400, exp - iat);
    }
}
