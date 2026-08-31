using System.Text.Json;
using BudgetApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetApi.Tests;

public sealed class BudgetApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString("N");

    public BudgetApiFactory()
    {
        // Environment variables are always part of WebApplication.CreateBuilder configuration.
        Environment.SetEnvironmentVariable("Database__Provider", "InMemory");
        Environment.SetEnvironmentVariable("Database__InMemoryName", _dbName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        await DbSeeder.SeedAsync(db);
    }
}

public sealed record ApiResult(int Status, JsonElement Json, string Raw)
{
    public T GetProperty<T>(string name)
        => Json.GetProperty(name).Deserialize<T>()!;
}

public static class TestAsserts
{
    public static void HasError(ApiResult result, string field)
    {
        Xunit.Assert.Equal(JsonValueKind.Object, result.Json.GetProperty("errors").ValueKind);
        Xunit.Assert.Equal(JsonValueKind.String, result.Json.GetProperty("errors").GetProperty(field).ValueKind);
    }
}

public abstract class ApiTestBase : IAsyncLifetime
{
    protected BudgetApiFactory Factory { get; private set; } = default!;
    protected HttpClient Client { get; private set; } = default!;
    protected string Token { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Factory = new BudgetApiFactory();
        await Factory.SeedAsync();
        Client = Factory.CreateClient();
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "admin123" });
        if (login.Status != 200)
        {
            throw new InvalidOperationException($"Login failed: {login.Raw}");
        }

        Token = login.GetProperty<string>("token")!;
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

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
