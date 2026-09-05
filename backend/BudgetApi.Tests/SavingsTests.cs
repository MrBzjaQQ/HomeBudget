using System.Text.Json;
using BudgetApi.Data;
using BudgetApi.Data.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetApi.Tests;

public class SavingsTests : ApiTestBase
{
    private const int SummaryYear = 2026;

    private async Task<int> CreateMemberAsync(string username, string displayName)
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username, displayName, password = "secret123" });
        Assert.Equal(201, created.Status);
        return created.Json.GetProperty("id").GetInt32();
    }

    private async Task<HttpClient> CreateMemberClientAsync(string username)
    {
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username, password = "secret123" });
        Assert.Equal(200, login.Status);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.GetProperty<string>("token")!);
        return client;
    }

    private async Task<int> GetContainerIdAsync(string name)
    {
        var list = await SendAsync(HttpMethod.Get, "/api/savings/containers");
        foreach (var container in list.Json.EnumerateArray())
        {
            if (container.GetProperty("name").GetString() == name)
            {
                return container.GetProperty("id").GetInt32();
            }
        }

        throw new InvalidOperationException($"Container {name} not found");
    }

    private async Task<int> GetAdminUserIdAsync()
    {
        var users = await SendAsync(HttpMethod.Get, "/api/users");
        foreach (var user in users.Json.EnumerateArray())
        {
            if (user.GetProperty("username").GetString() == "admin")
            {
                return user.GetProperty("id").GetInt32();
            }
        }

        throw new InvalidOperationException("admin user not found");
    }

    private static void AssertMonthAmount(JsonElement row, string month, decimal amount)
    {
        Assert.Equal(month, row.GetProperty("month").GetString());
        Assert.Equal(amount, row.GetProperty("amount").GetDecimal());
    }

    private static void AssertMonthDelta(JsonElement row, string month, decimal delta)
    {
        Assert.Equal(month, row.GetProperty("month").GetString());
        Assert.Equal(delta, row.GetProperty("delta").GetDecimal());
    }

    [Fact]
    public async Task Containers_Seed_PresentInList()
    {
        var list = await SendAsync(HttpMethod.Get, "/api/savings/containers");

        Assert.Equal(200, list.Status);
        var items = list.Json.EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        var names = items.Select(c => c.GetProperty("name").GetString()).ToList();
        Assert.Contains("Вклад", names);
        Assert.Contains("Наличка", names);
        Assert.Contains("Накопительный счёт", names);
    }

    [Fact]
    public async Task Container_Create_Returns201_AndVisibleInList()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Севка", color = "#112233" });

        Assert.Equal(201, created.Status);
        Assert.Equal("Севка", created.Json.GetProperty("name").GetString());
        Assert.Equal("#112233", created.Json.GetProperty("color").GetString());
        Assert.Equal(4, created.Json.GetProperty("sortOrder").GetInt32());

        var list = await SendAsync(HttpMethod.Get, "/api/savings/containers");
        Assert.Contains(list.Json.EnumerateArray(), c => c.GetProperty("name").GetString() == "Севка");
    }

    [Fact]
    public async Task Container_Create_DuplicateName_Returns409()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Вклад" });

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Container_Create_EmptyName_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "   " });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    public async Task Container_Create_NameTooLong_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = new string('с', 101) });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    public async Task Container_Create_BadColor_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Окрас", color = "red" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "color");
    }

    [Fact]
    public async Task Container_Update_RenamedAndRecolored_Returns200_OtherFieldsUntouched()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "СтароеИмя", color = "#112233", sortOrder = 5 });
        Assert.Equal(201, created.Status);
        var id = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/containers/{id}", new { name = "НовоеИмя", color = "#aa00bb" });

        Assert.Equal(200, updated.Status);
        Assert.Equal("НовоеИмя", updated.Json.GetProperty("name").GetString());
        Assert.Equal("#aa00bb", updated.Json.GetProperty("color").GetString());
        Assert.Equal(5, updated.Json.GetProperty("sortOrder").GetInt32());
    }

    [Fact]
    public async Task Container_Update_ToNameOfAnotherContainer_Returns400()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "ПервыйСчёт" });
        var id = created.Json.GetProperty("id").GetInt32();

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/containers/{id}", new { name = "Вклад" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    public async Task Container_Update_EmptyColor_ClearsColor()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Цветастый", color = "#123456" });
        var id = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/containers/{id}", new { color = "" });

        Assert.Equal(200, updated.Status);
        Assert.Equal(JsonValueKind.Null, updated.Json.GetProperty("color").ValueKind);
    }

    [Fact]
    public async Task Container_UpdateOrDelete_Missing_Returns404()
    {
        var updated = await SendAsync(HttpMethod.Put, "/api/savings/containers/99999", new { name = "Ник" });
        Assert.Equal(404, updated.Status);
        Assert.Equal("NotFound", updated.GetProperty<string>("code"));

        var deleted = await SendAsync(HttpMethod.Delete, "/api/savings/containers/99999");
        Assert.Equal(404, deleted.Status);
        Assert.Equal("NotFound", deleted.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Container_Delete_WithoutEntries_Returns204()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Временный" });
        var id = created.Json.GetProperty("id").GetInt32();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/savings/containers/{id}");
        Assert.Equal(204, deleted.Status);

        var list = await SendAsync(HttpMethod.Get, "/api/savings/containers");
        Assert.DoesNotContain(list.Json.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Container_Delete_WithEntries_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan12", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var entry = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1000 });
        Assert.Equal(201, entry.Status);

        var result = await SendAsync(HttpMethod.Delete, $"/api/savings/containers/{containerId}");

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Entry_Create_ForNonAdmin_Returns201_AndVisibleInMonth()
    {
        var memberId = await CreateMemberAsync("ivan13", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 100000, note = "осенний остаток" });
        Assert.Equal(201, created.Status);
        Assert.Equal("2026-09", created.Json.GetProperty("month").GetString());
        Assert.Equal("Иван", created.Json.GetProperty("userName").GetString());
        Assert.Equal(memberId, created.Json.GetProperty("userId").GetInt32());
        Assert.Equal("Вклад", created.Json.GetProperty("containerName").GetString());
        Assert.Equal(100000m, created.Json.GetProperty("amount").GetDecimal());
        Assert.Equal("осенний остаток", created.Json.GetProperty("note").GetString());
        var id = created.Json.GetProperty("id").GetInt64();

        var month = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-09");
        Assert.Equal(200, month.Status);
        var item = month.Json.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("id").GetInt64() == id);
        Assert.Equal("Иван", item.GetProperty("userName").GetString());
        Assert.Equal(100000m, item.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Entry_Create_ForAdmin_Returns400()
    {
        var adminId = await GetAdminUserIdAsync();
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = adminId, containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    public async Task Entry_Create_WithoutUserId_ByAdmin_Returns400()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    public async Task Entry_Create_WithoutUserId_ByNonAdmin_UsesCurrentUser()
    {
        var memberId = await CreateMemberAsync("ivan16", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        using var memberClient = await CreateMemberClientAsync("ivan16");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/savings")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { month = "2026-09", containerId, amount = 5000 }),
        };
        var response = await memberClient.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(raw).RootElement.Clone();

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(memberId, json.GetProperty("userId").GetInt32());
    }

    [Fact]
    public async Task Entry_Create_DuplicateTriple_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan17", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var first = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1000 });
        Assert.Equal(201, first.Status);

        var second = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 2000 });

        Assert.Equal(409, second.Status);
        Assert.Equal("Conflict", second.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Entry_Create_BadContainer_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan18", "Иван");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId = 99999, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "containerId");
    }

    [Fact]
    public async Task Entry_Create_BadUser_Returns400()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = 99999, containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    public async Task Entry_Create_InvalidAmount_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan20", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var zero = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 0 });
        Assert.Equal(400, zero.Status);
        TestAsserts.HasError(zero, "amount");

        var negative = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = -5 });
        Assert.Equal(400, negative.Status);
        TestAsserts.HasError(negative, "amount");

        var tooBig = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1_000_000_000_000m });
        Assert.Equal(400, tooBig.Status);
        TestAsserts.HasError(tooBig, "amount");
    }

    [Fact]
    public async Task Entry_Create_BadMonth_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan21", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        foreach (var badMonth in new[] { "2026-13", "09-2026", "" })
        {
            var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = badMonth, userId = memberId, containerId, amount = 1000 });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "month");
        }
    }

    [Fact]
    public async Task Entry_Create_NoteTooLong_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan22", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new
        {
            month = "2026-09",
            userId = memberId,
            containerId,
            amount = 1000,
            note = new string('а', 501),
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "note");
    }

    [Fact]
    public async Task Entry_Create_MissingRequiredFields_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "month");
        TestAsserts.HasError(result, "containerId");
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    public async Task Entry_Update_ChangesAmount_AndUpdatedAtNotBeforeCreatedAt()
    {
        var memberId = await CreateMemberAsync("ivan24", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();
        var createdAt = created.Json.GetProperty("createdAt").GetDateTime();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { amount = 2500 });

        Assert.Equal(200, updated.Status);
        Assert.Equal(2500m, updated.Json.GetProperty("amount").GetDecimal());
        Assert.True(updated.Json.GetProperty("updatedAt").GetDateTime() >= createdAt);
    }

    [Fact]
    public async Task Entry_Update_MoveToOtherMonthContainerUser_Returns200()
    {
        var memberId = await CreateMemberAsync("ivan25", "Иван");
        var katyaId = await CreateMemberAsync("katya25", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId = depositId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new
        {
            month = "2026-10",
            userId = katyaId,
            containerId = cashId,
            amount = 1500,
        });
        Assert.Equal(200, updated.Status);
        Assert.Equal("2026-10", updated.Json.GetProperty("month").GetString());
        Assert.Equal(katyaId, updated.Json.GetProperty("userId").GetInt32());
        Assert.Equal(cashId, updated.Json.GetProperty("containerId").GetInt32());
        Assert.Equal("Катя", updated.Json.GetProperty("userName").GetString());

        var oldMonth = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-09");
        Assert.DoesNotContain(oldMonth.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
        var newMonth = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-10");
        Assert.Contains(newMonth.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    public async Task Entry_Update_MoveToOccupiedTriple_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan26", "Иван");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId = cashId, amount = 2000 });
        var id = created.Json.GetProperty("id").GetInt64();
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId = depositId, amount = 1000 });

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { containerId = depositId });

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Entry_Update_ToAdmin_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan27", "Иван");
        var adminId = await GetAdminUserIdAsync();
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { userId = adminId });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    public async Task Entry_UpdateOrDelete_Missing_Returns404()
    {
        var updated = await SendAsync(HttpMethod.Put, "/api/savings/99999", new { amount = 10 });
        Assert.Equal(404, updated.Status);
        Assert.Equal("NotFound", updated.GetProperty<string>("code"));

        var deleted = await SendAsync(HttpMethod.Delete, "/api/savings/99999");
        Assert.Equal(404, deleted.Status);
        Assert.Equal("NotFound", deleted.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Entry_Delete_Returns204_AndGoneFromMonth()
    {
        var memberId = await CreateMemberAsync("ivan29", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/savings/{id}");
        Assert.Equal(204, deleted.Status);

        var month = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-09");
        Assert.DoesNotContain(month.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    public async Task GetMonth_Total_SumsOnlyEntriesOfThatMonth()
    {
        var ivanId = await CreateMemberAsync("ivan30", "Иван");
        var katyaId = await CreateMemberAsync("katya30", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = ivanId, containerId = depositId, amount = 100000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = katyaId, containerId = depositId, amount = 50000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = katyaId, containerId = cashId, amount = 20000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-10", userId = ivanId, containerId = depositId, amount = 777777 });

        var result = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-09");

        Assert.Equal(200, result.Status);
        Assert.Equal("2026-09", result.Json.GetProperty("month").GetString());
        Assert.Equal(3, result.Json.GetProperty("entries").EnumerateArray().Count());
        Assert.Equal(170000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_Entries_SortedByUserNameContainerOrderContainerNameAndId()
    {
        var annaId = await CreateMemberAsync("anna31", "Анна");
        var ivanId = await CreateMemberAsync("ivan31a", "Иван");
        var ivan2Id = await CreateMemberAsync("ivan31b", "Иван");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = annaId, containerId = cashId, amount = 100 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = annaId, containerId = depositId, amount = 200 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = ivanId, containerId = depositId, amount = 300 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-09", userId = ivan2Id, containerId = depositId, amount = 400 });

        var result = await SendAsync(HttpMethod.Get, "/api/savings?month=2026-09");
        var rows = result.Json.GetProperty("entries").EnumerateArray()
            .Select(e => (e.GetProperty("userName").GetString()!, e.GetProperty("containerName").GetString()!, e.GetProperty("amount").GetDecimal()))
            .ToList();

        Assert.Equal(new[]
        {
            ("Анна", "Вклад", 200m),
            ("Анна", "Наличка", 100m),
            ("Иван", "Вклад", 300m),
            ("Иван", "Вклад", 400m),
        }, rows);
    }

    [Fact]
    public async Task Summary_ReturnsTwelveMonths_ByMemberAndTotal()
    {
        var ivanId = await CreateMemberAsync("ivan32", "Иван");
        var katyaId = await CreateMemberAsync("katya32", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-01", userId = ivanId, containerId = depositId, amount = 10000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-03", userId = ivanId, containerId = depositId, amount = 20000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-01", userId = katyaId, containerId = cashId, amount = 5000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2025-12", userId = ivanId, containerId = depositId, amount = 424242 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?year={SummaryYear}");

        Assert.Equal(200, result.Status);
        Assert.Equal(SummaryYear, result.Json.GetProperty("year").GetInt32());

        var months = result.Json.GetProperty("months").EnumerateArray().ToList();
        Assert.Equal(12, months.Count);
        AssertMonthAmount(months[0], "2026-01", 15000m);
        AssertMonthAmount(months[1], "2026-02", 0m);
        AssertMonthAmount(months[2], "2026-03", 20000m);
        AssertMonthAmount(months[11], "2026-12", 0m);

        var byMember = result.Json.GetProperty("byMember").EnumerateArray().ToList();
        Assert.Equal(2, byMember.Count);
        Assert.Equal("Иван", byMember[0].GetProperty("userName").GetString());
        Assert.Equal(30000m, byMember[0].GetProperty("amount").GetDecimal());
        Assert.Equal("Катя", byMember[1].GetProperty("userName").GetString());
        Assert.Equal(5000m, byMember[1].GetProperty("amount").GetDecimal());
        Assert.Equal(35000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Summary_Deltas_FromPreviousMonth_JanuaryUsesPreviousDecember_NegativeWhenDecrease()
    {
        var ivanId = await CreateMemberAsync("ivan33", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2025-12", userId = ivanId, containerId, amount = 30000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-01", userId = ivanId, containerId, amount = 30000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-02", userId = ivanId, containerId, amount = 15000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-04", userId = ivanId, containerId, amount = 25000 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?year={SummaryYear}");
        var deltas = result.Json.GetProperty("deltas").EnumerateArray().ToList();

        Assert.Equal(12, deltas.Count);
        AssertMonthDelta(deltas[0], "2026-01", 0m);
        AssertMonthDelta(deltas[1], "2026-02", -15000m);
        AssertMonthDelta(deltas[2], "2026-03", -15000m);
        AssertMonthDelta(deltas[3], "2026-04", 25000m);
    }

    [Fact]
    public async Task Summary_ExcludesAdminEntries()
    {
        var ivanId = await CreateMemberAsync("ivan34", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var adminId = await GetAdminUserIdAsync();

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = "2026-05", userId = ivanId, containerId, amount = 10000 });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        db.SavingsEntries.Add(new SavingsEntry
        {
            Month = new DateOnly(SummaryYear, 5, 1),
            UserId = adminId,
            ContainerId = containerId,
            Amount = 99999,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?year={SummaryYear}");

        Assert.Equal(200, result.Status);
        var months = result.Json.GetProperty("months").EnumerateArray().ToList();
        AssertMonthAmount(months[4], "2026-05", 10000m);
        var byMember = result.Json.GetProperty("byMember").EnumerateArray().ToList();
        Assert.Single(byMember);
        Assert.Equal("Иван", byMember[0].GetProperty("userName").GetString());
        Assert.Equal(10000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Summary_InvalidYear_Returns400()
    {
        foreach (var year in new[] { 1999, 2200, 0 })
        {
            var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?year={year}");
            Assert.Equal(400, result.Status);
            Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        }
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns401()
    {
        using var anonymous = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/savings?month=2026-09");
        var response = await anonymous.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
