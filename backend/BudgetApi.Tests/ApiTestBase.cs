using System.Text.Json;
using Budget.Infrastructure;
using Budget.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Создаёт HTTP-хост тестового API с InMemory-базой и предоставляет сидинг.</summary>
public sealed class BudgetApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString("N");

    /// <summary>Настраивает провайдер InMemory и уникальное имя тестовой базы.</summary>
    public BudgetApiFactory()
    {
        // Environment variables are always part of WebApplication.CreateBuilder configuration.
        Environment.SetEnvironmentVariable(TestConstants.DatabaseProviderEnvironmentVariable, Budget.Infrastructure.Data.DatabaseConfiguration.InMemoryProvider);
        Environment.SetEnvironmentVariable(TestConstants.DatabaseInMemoryNameEnvironmentVariable, _dbName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(BudgetApi.Common.ApiEnvironments.Testing);
    }

    /// <summary>Заполняет тестовую базу начальными данными через внедрённый сидер.</summary>
    /// <returns>Асинхронная операция заполнения базы.</returns>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(db);
    }
}

/// <summary>Содержит HTTP-статус, разобранный JSON и исходный текст ответа API.</summary>
/// <param name="Status">Числовой HTTP-статус ответа.</param>
/// <param name="Json">Разобранное JSON-тело ответа.</param>
/// <param name="Raw">Исходное текстовое тело ответа.</param>
public sealed record ApiResult(int Status, JsonElement Json, string Raw)
{
    /// <summary>Десериализует значение свойства JSON-ответа в указанный тип.</summary>
    /// <typeparam name="T">Тип результата десериализации.</typeparam>
    /// <param name="name">Имя свойства верхнего уровня в JSON.</param>
    /// <returns>Десериализованное значение свойства.</returns>
    public T GetProperty<T>(string name)
        => Json.GetProperty(name).Deserialize<T>()!;
}

/// <summary>Содержит вспомогательные утверждения для контрактов ошибок API.</summary>
public static class TestAsserts
{
    /// <summary>Проверяет наличие строковой ошибки для указанного поля ответа.</summary>
    /// <param name="result">Результат HTTP-запроса с ошибкой.</param>
    /// <param name="field">Имя поля в объекте ошибок.</param>
    public static void HasError(ApiResult result, string field)
    {
        Xunit.Assert.Equal(JsonValueKind.Object, result.Json.GetProperty("errors").ValueKind);
        Xunit.Assert.Equal(JsonValueKind.String, result.Json.GetProperty("errors").GetProperty(field).ValueKind);
    }
}

/// <summary>Предоставляет интеграционному тесту HTTP-клиент, сидированную базу и токен администратора.</summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    /// <summary>Тестовый HTTP-хост и источник DI-служб для сценария.</summary>
    protected BudgetApiFactory Factory { get; private set; } = default!;

    /// <summary>HTTP-клиент, авторизованный токеном администратора.</summary>
    protected HttpClient Client { get; private set; } = default!;

    /// <summary>Токен доступа администратора, полученный при инициализации.</summary>
    protected string Token { get; private set; } = string.Empty;

    /// <summary>Создаёт тестовый хост, заполняет базу и выполняет вход администратора.</summary>
    /// <returns>Асинхронная операция инициализации теста.</returns>
    public async Task InitializeAsync()
    {
        Factory = new BudgetApiFactory();
        await Factory.SeedAsync();
        Client = Factory.CreateClient();
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = SystemUsernames.Admin, password = "admin123" });
        if (login.Status != 200)
        {
            throw new InvalidOperationException($"Login failed: {login.Raw}");
        }

        Token = login.GetProperty<string>("token")!;
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
    }

    /// <summary>Освобождает HTTP-клиент и тестовый хост после завершения теста.</summary>
    /// <returns>Асинхронная операция освобождения ресурсов.</returns>
    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>Отправляет HTTP-запрос тестов с текущей авторизацией и разбирает ответ.</summary>
    /// <param name="method">HTTP-метод запроса.</param>
    /// <param name="url">Путь API относительно тестового хоста.</param>
    /// <param name="body">Необязательное тело запроса, сериализуемое в JSON.</param>
    /// <returns>Статус, JSON и исходный текст ответа.</returns>
    protected async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = System.Net.Http.Json.JsonContent.Create(body);
        }

        var response = await Client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement json = string.IsNullOrWhiteSpace(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return new ApiResult((int)response.StatusCode, json, raw);
    }

    /// <summary>Находит идентификатор корневой категории или её непосредственной подкатегории.</summary>
    /// <param name="name">Имя искомой категории.</param>
    /// <param name="parentName">Имя группы, в которой следует искать подкатегорию, либо null для корневой категории.</param>
    /// <returns>Идентификатор найденной категории.</returns>
    protected async Task<int> GetCategoryIdAsync(string name, string? parentName = null)
    {
        var result = await SendAsync(HttpMethod.Get, "/api/categories");
        var items = result.Json.EnumerateArray().ToList();
        foreach (var group in items)
        {
            if (string.IsNullOrEmpty(parentName) && group.GetProperty("name").GetString() == name)
            {
                return group.GetProperty("id").GetInt32();
            }

            if (!string.IsNullOrEmpty(parentName) && group.GetProperty("name").GetString() == parentName)
            {
                foreach (var child in group.GetProperty("children").EnumerateArray())
                {
                    if (child.GetProperty("name").GetString() == name)
                    {
                        return child.GetProperty("id").GetInt32();
                    }
                }
            }
        }

        throw new InvalidOperationException($"Category {name} not found");
    }
}
