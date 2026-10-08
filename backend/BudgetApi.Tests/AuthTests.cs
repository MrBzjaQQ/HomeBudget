using System.Text;
using System.Text.Json;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет HTTP-сценарии аутентификации и получения текущего пользователя.</summary>
public class AuthTests : ApiTestBase
{
    [Fact]
    /// <summary>Проверяет отказ во входе при неверном пароле.</summary>
    public async Task Login_InvalidPassword_Returns401()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = SystemUsernames.Admin, password = "wrong" });

        Assert.Equal(401, result.Status);
        Assert.Equal(ApiErrorCodes.Unauthorized, result.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет отказ во входе деактивированному пользователю.</summary>
    public async Task Login_DeactivatedUser_Returns401()
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
    /// <summary>Проверяет выдачу токена и данных пользователя при успешном входе.</summary>
    public async Task Login_ValidCredentials_ReturnsTokenAndUser()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = SystemUsernames.Admin, password = "admin123" });

        Assert.Equal(200, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty<string>("token")));
        Assert.Equal(SystemUsernames.Admin, result.Json.GetProperty("user").GetProperty("username").GetString());
    }

    [Fact]
    /// <summary>Проверяет формат даты сервера в ответе успешного входа.</summary>
    public async Task Login_ValidCredentials_ReturnsServerDateInTodayFormat()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = SystemUsernames.Admin, password = "admin123" });

        Assert.Equal(200, result.Status);
        var serverDate = result.GetProperty<string>("serverDate")!;
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", serverDate);
        Assert.Equal(Validation.Today().ToString(DateFormats.Date), serverDate);
    }

    [Fact]
    /// <summary>Проверяет получение текущего пользователя и даты сервера.</summary>
    public async Task GetCurrentUser_AuthenticatedRequest_ReturnsUserAndServerDate()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/auth/me");

        Assert.Equal(200, result.Status);
        Assert.Equal(SystemUsernames.Admin, result.Json.GetProperty("user").GetProperty("username").GetString());
        var serverDate = result.GetProperty<string>("serverDate")!;
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", serverDate);
        Assert.Equal(Validation.Today().ToString(DateFormats.Date), serverDate);
    }

    [Fact]
    /// <summary>Проверяет формат ошибки при запросе текущего пользователя без токена.</summary>
    public async Task GetCurrentUser_WithoutToken_Returns401JsonError()
    {
        using var anonymous = Factory.CreateClient();
        var result = await anonymous.GetAsync("/api/auth/me");
        var raw = await result.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(ApiErrorCodes.Unauthorized, JsonDocument.Parse(raw).RootElement.GetProperty("code").GetString());
        Assert.Contains("Требуется авторизация", raw);
    }

    [Fact]
    /// <summary>Проверяет срок действия выданного токена в 24 часа.</summary>
    public async Task Login_ValidCredentials_ReturnsTokenWith24HourLifetime()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = SystemUsernames.Admin, password = "admin123" });
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
