using System.Text.Json;
using Budget.Application.Common;
using Budget.Application.Savings;
using Budget.Domain.Entities;
using Budget.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет HTTP-сценарии счетов и записей накоплений, а также их сводку.</summary>
public class SavingsTests : ApiTestBase
{
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
            if (user.GetProperty("username").GetString() == SystemUsernames.Admin)
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
    /// <summary>Проверяет наличие начальных счетов накоплений в списке.</summary>
    public async Task ListContainers_SeededData_ReturnsInitialContainers()
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
    /// <summary>Проверяет создание счёта накоплений и его появление в списке.</summary>
    public async Task CreateContainer_ValidRequest_Returns201AndAppearsInList()
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
    /// <summary>Проверяет конфликт при создании счёта с повторным именем.</summary>
    public async Task CreateContainer_DuplicateName_Returns409()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Вклад" });

        Assert.Equal(409, result.Status);
        Assert.Equal(ApiErrorCodes.Conflict, result.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет валидацию пустого имени счёта накоплений.</summary>
    public async Task CreateContainer_EmptyName_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "   " });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    /// <summary>Проверяет валидацию имени счёта, превышающего допустимую длину.</summary>
    public async Task CreateContainer_NameAboveMaximumLength_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = new string('с', Validation.MaxNameLength + 1) });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    /// <summary>Проверяет валидацию некорректного цвета счёта накоплений.</summary>
    public async Task CreateContainer_InvalidColor_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Окрас", color = "red" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "color");
    }

    [Fact]
    /// <summary>Проверяет изменение имени и цвета счёта без изменения остальных полей.</summary>
    public async Task UpdateContainer_NameAndColor_ReturnsUpdatedContainerAndPreservesOtherFields()
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
    /// <summary>Проверяет запрет присвоения счёту имени другого существующего счёта.</summary>
    public async Task UpdateContainer_DuplicateName_Returns400()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "ПервыйСчёт" });
        var id = created.Json.GetProperty("id").GetInt32();

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/containers/{id}", new { name = "Вклад" });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    /// <summary>Проверяет очистку цвета счёта при передаче пустого значения.</summary>
    public async Task UpdateContainer_EmptyColor_ClearsColor()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Цветастый", color = "#123456" });
        var id = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/containers/{id}", new { color = "" });

        Assert.Equal(200, updated.Status);
        Assert.Equal(JsonValueKind.Null, updated.Json.GetProperty("color").ValueKind);
    }

    [Fact]
    /// <summary>Проверяет ответы обновления и удаления для отсутствующего счёта.</summary>
    public async Task UpdateOrDeleteContainer_MissingId_Returns404()
    {
        var updated = await SendAsync(HttpMethod.Put, "/api/savings/containers/99999", new { name = "Ник" });
        Assert.Equal(404, updated.Status);
        Assert.Equal(ApiErrorCodes.NotFound, updated.GetProperty<string>("code"));

        var deleted = await SendAsync(HttpMethod.Delete, "/api/savings/containers/99999");
        Assert.Equal(404, deleted.Status);
        Assert.Equal(ApiErrorCodes.NotFound, deleted.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет удаление счёта, не содержащего записей.</summary>
    public async Task DeleteContainer_WithoutEntries_Returns204AndRemovesContainer()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/savings/containers", new { name = "Временный" });
        var id = created.Json.GetProperty("id").GetInt32();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/savings/containers/{id}");
        Assert.Equal(204, deleted.Status);

        var list = await SendAsync(HttpMethod.Get, "/api/savings/containers");
        Assert.DoesNotContain(list.Json.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    /// <summary>Проверяет запрет удаления счёта, связанного с записями накоплений.</summary>
    public async Task DeleteContainer_WithEntries_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan12", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var entry = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1000 });
        Assert.Equal(201, entry.Status);

        var result = await SendAsync(HttpMethod.Delete, $"/api/savings/containers/{containerId}");

        Assert.Equal(409, result.Status);
        Assert.Equal(ApiErrorCodes.Conflict, result.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет запрет удаления счёта, используемого финансовой целью.</summary>
    public async Task DeleteContainer_WithGoal_Returns409()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var goal = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Отпуск", targetAmount = 100000, containerId });
        Assert.Equal(201, goal.Status);

        var result = await SendAsync(HttpMethod.Delete, $"/api/savings/containers/{containerId}");

        Assert.Equal(409, result.Status);
        Assert.Equal(ApiErrorCodes.Conflict, result.GetProperty<string>("code"));
        Assert.Contains("финансовые цели", result.GetProperty<string>("message"));
    }

    [Fact]
    /// <summary>Проверяет создание записи накоплений для обычного пользователя.</summary>
    public async Task CreateSavingsEntry_ForNonAdmin_Returns201AndAppearsInMonth()
    {
        var memberId = await CreateMemberAsync("ivan13", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 100000, note = "осенний остаток" });
        Assert.Equal(201, created.Status);
        Assert.Equal(TestConstants.September2026, created.Json.GetProperty("month").GetString());
        Assert.Equal("Иван", created.Json.GetProperty("userName").GetString());
        Assert.Equal(memberId, created.Json.GetProperty("userId").GetInt32());
        Assert.Equal("Вклад", created.Json.GetProperty("containerName").GetString());
        Assert.Equal(100000m, created.Json.GetProperty("amount").GetDecimal());
        Assert.Equal("осенний остаток", created.Json.GetProperty("note").GetString());
        var id = created.Json.GetProperty("id").GetInt64();

        var month = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");
        Assert.Equal(200, month.Status);
        var item = month.Json.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("id").GetInt64() == id);
        Assert.Equal("Иван", item.GetProperty("userName").GetString());
        Assert.Equal(100000m, item.GetProperty("amount").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет запрет создания записи накоплений от имени администратора.</summary>
    public async Task CreateSavingsEntry_ForAdmin_Returns400()
    {
        var adminId = await GetAdminUserIdAsync();
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = adminId, containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    /// <summary>Проверяет необходимость указать пользователя при создании записи администратором.</summary>
    public async Task CreateSavingsEntry_WithoutUserIdByAdmin_Returns400()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    /// <summary>Проверяет использование текущего пользователя при создании записи без его идентификатора.</summary>
    public async Task CreateSavingsEntry_WithoutUserIdByMember_UsesCurrentUser()
    {
        var memberId = await CreateMemberAsync("ivan16", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        using var memberClient = await CreateMemberClientAsync("ivan16");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/savings")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { month = TestConstants.September2026, containerId, amount = 5000 }),
        };
        var response = await memberClient.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(raw).RootElement.Clone();

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(memberId, json.GetProperty("userId").GetInt32());
    }

    [Fact]
    /// <summary>Проверяет конфликт повторной записи для той же комбинации месяца, пользователя и счёта.</summary>
    public async Task CreateSavingsEntry_DuplicateMonthUserContainer_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan17", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var first = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1000 });
        Assert.Equal(201, first.Status);

        var second = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 2000 });

        Assert.Equal(409, second.Status);
        Assert.Equal(ApiErrorCodes.Conflict, second.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет отклонение записи с несуществующим счётом.</summary>
    public async Task CreateSavingsEntry_UnknownContainer_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan18", "Иван");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId = 99999, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "containerId");
    }

    [Fact]
    /// <summary>Проверяет отклонение записи с несуществующим пользователем.</summary>
    public async Task CreateSavingsEntry_UnknownUser_Returns400()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = 99999, containerId, amount = 1000 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    /// <summary>Проверяет отклонение записи с недопустимой суммой.</summary>
    public async Task CreateSavingsEntry_InvalidAmount_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan20", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var zero = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 0 });
        Assert.Equal(400, zero.Status);
        TestAsserts.HasError(zero, "amount");

        var negative = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = -5 });
        Assert.Equal(400, negative.Status);
        TestAsserts.HasError(negative, "amount");

        var tooBig = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1_000_000_000_000m });
        Assert.Equal(400, tooBig.Status);
        TestAsserts.HasError(tooBig, "amount");
    }

    [Fact]
    /// <summary>Проверяет отклонение записи с некорректным месяцем.</summary>
    public async Task CreateSavingsEntry_InvalidMonth_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan21", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        foreach (var badMonth in new[] { TestConstants.InvalidMonth, "09-2026", "" })
        {
            var result = await SendAsync(HttpMethod.Post, "/api/savings", new { month = badMonth, userId = memberId, containerId, amount = 1000 });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "month");
        }
    }

    [Fact]
    /// <summary>Проверяет отклонение записи с заметкой длиннее допустимого предела.</summary>
    public async Task CreateSavingsEntry_NoteAboveMaximumLength_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan22", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/savings", new
        {
            month = TestConstants.September2026,
            userId = memberId,
            containerId,
            amount = 1000,
            note = new string('а', Validation.MaxNoteLength + 1),
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "note");
    }

    [Fact]
    /// <summary>Проверяет ошибки обязательных полей при создании пустой записи накоплений.</summary>
    public async Task CreateSavingsEntry_WithoutRequiredFields_Returns400()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/savings", new { });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "month");
        TestAsserts.HasError(result, "containerId");
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    /// <summary>Проверяет изменение суммы записи и корректность временных меток.</summary>
    public async Task UpdateSavingsEntry_ChangedAmount_UpdatesValueAndTimestamp()
    {
        var memberId = await CreateMemberAsync("ivan24", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();
        var createdAt = created.Json.GetProperty("createdAt").GetDateTime();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { amount = 2500 });

        Assert.Equal(200, updated.Status);
        Assert.Equal(2500m, updated.Json.GetProperty("amount").GetDecimal());
        Assert.True(updated.Json.GetProperty("updatedAt").GetDateTime() >= createdAt);
    }

    [Fact]
    /// <summary>Проверяет перенос записи в другой месяц, счёт и пользователя.</summary>
    public async Task UpdateSavingsEntry_ChangedMonthContainerAndUser_ReturnsUpdatedEntry()
    {
        var memberId = await CreateMemberAsync("ivan25", "Иван");
        var katyaId = await CreateMemberAsync("katya25", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId = depositId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var updated = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new
        {
            month = TestConstants.October2026,
            userId = katyaId,
            containerId = cashId,
            amount = 1500,
        });
        Assert.Equal(200, updated.Status);
        Assert.Equal(TestConstants.October2026, updated.Json.GetProperty("month").GetString());
        Assert.Equal(katyaId, updated.Json.GetProperty("userId").GetInt32());
        Assert.Equal(cashId, updated.Json.GetProperty("containerId").GetInt32());
        Assert.Equal("Катя", updated.Json.GetProperty("userName").GetString());

        var oldMonth = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");
        Assert.DoesNotContain(oldMonth.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
        var newMonth = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.October2026}");
        Assert.Contains(newMonth.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    /// <summary>Проверяет конфликт переноса записи на уже занятую комбинацию.</summary>
    public async Task UpdateSavingsEntry_ToOccupiedMonthUserContainer_Returns409()
    {
        var memberId = await CreateMemberAsync("ivan26", "Иван");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId = cashId, amount = 2000 });
        var id = created.Json.GetProperty("id").GetInt64();
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId = depositId, amount = 1000 });

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { containerId = depositId });

        Assert.Equal(409, result.Status);
        Assert.Equal(ApiErrorCodes.Conflict, result.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет запрет назначения записи накоплений администратору.</summary>
    public async Task UpdateSavingsEntry_AssignedToAdmin_Returns400()
    {
        var memberId = await CreateMemberAsync("ivan27", "Иван");
        var adminId = await GetAdminUserIdAsync();
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var result = await SendAsync(HttpMethod.Put, $"/api/savings/{id}", new { userId = adminId });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "userId");
    }

    [Fact]
    /// <summary>Проверяет ответы обновления и удаления для отсутствующей записи накоплений.</summary>
    public async Task UpdateOrDeleteSavingsEntry_MissingId_Returns404()
    {
        var updated = await SendAsync(HttpMethod.Put, "/api/savings/99999", new { amount = 10 });
        Assert.Equal(404, updated.Status);
        Assert.Equal(ApiErrorCodes.NotFound, updated.GetProperty<string>("code"));

        var deleted = await SendAsync(HttpMethod.Delete, "/api/savings/99999");
        Assert.Equal(404, deleted.Status);
        Assert.Equal(ApiErrorCodes.NotFound, deleted.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет удаление записи накоплений из месячного списка.</summary>
    public async Task DeleteSavingsEntry_ExistingEntry_Returns204AndRemovesItFromMonth()
    {
        var memberId = await CreateMemberAsync("ivan29", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = memberId, containerId, amount = 1000 });
        var id = created.Json.GetProperty("id").GetInt64();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/savings/{id}");
        Assert.Equal(204, deleted.Status);

        var month = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");
        Assert.DoesNotContain(month.Json.GetProperty("entries").EnumerateArray(), e => e.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    /// <summary>Проверяет сумму записей только за запрошенный месяц.</summary>
    public async Task GetSavingsMonth_WithEntries_ReturnsMonthTotalOnly()
    {
        var ivanId = await CreateMemberAsync("ivan30", "Иван");
        var katyaId = await CreateMemberAsync("katya30", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = ivanId, containerId = depositId, amount = 100000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = katyaId, containerId = depositId, amount = 50000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = katyaId, containerId = cashId, amount = 20000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.October2026, userId = ivanId, containerId = depositId, amount = 777777 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");

        Assert.Equal(200, result.Status);
        Assert.Equal(TestConstants.September2026, result.Json.GetProperty("month").GetString());
        Assert.Equal(3, result.Json.GetProperty("entries").EnumerateArray().Count());
        Assert.Equal(170000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет сортировку записей месяца по пользователю, порядку счёта, имени и идентификатору.</summary>
    public async Task GetSavingsMonth_WithEntries_ReturnsEntriesInDefinedOrder()
    {
        var annaId = await CreateMemberAsync("anna31", "Анна");
        var ivanId = await CreateMemberAsync("ivan31a", "Иван");
        var ivan2Id = await CreateMemberAsync("ivan31b", "Иван");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = annaId, containerId = cashId, amount = 100 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = annaId, containerId = depositId, amount = 200 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = ivanId, containerId = depositId, amount = 300 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = TestConstants.September2026, userId = ivan2Id, containerId = depositId, amount = 400 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");
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
    /// <summary>Проверяет сводку за двенадцать месяцев по пользователям и общую сумму.</summary>
    public async Task GetSavingsSummary_WithinWindow_ReturnsMonthlyMemberAndTotalAmounts()
    {
        var ivanId = await CreateMemberAsync("ivan32", "Иван");
        var katyaId = await CreateMemberAsync("katya32", "Катя");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var to = Validation.Today().AddMonths(-1);
        var first = to.AddMonths(-(SavingsConstants.SummaryMonthCount - 1));
        var beforeWindow = first.AddMonths(-1);

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = first.ToString(DateFormats.Month), userId = ivanId, containerId = depositId, amount = 10000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = to.AddMonths(-2).ToString(DateFormats.Month), userId = ivanId, containerId = depositId, amount = 20000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = to.ToString(DateFormats.Month), userId = katyaId, containerId = cashId, amount = 5000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = beforeWindow.ToString(DateFormats.Month), userId = ivanId, containerId = depositId, amount = 424242 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?to={to.ToString(DateFormats.Month)}");

        Assert.Equal(200, result.Status);
        Assert.Equal(to.ToString(DateFormats.Month), result.Json.GetProperty("to").GetString());

        var months = result.Json.GetProperty("months").EnumerateArray().ToList();
        Assert.Equal(SavingsConstants.SummaryMonthCount, months.Count);
        AssertMonthAmount(months[0], first.ToString(DateFormats.Month), 10000m);
        AssertMonthAmount(months[11], to.ToString(DateFormats.Month), 5000m);

        var byMember = result.Json.GetProperty("byMember").EnumerateArray().ToList();
        Assert.Equal(2, byMember.Count);
        Assert.Equal("Иван", byMember[0].GetProperty("userName").GetString());
        Assert.Equal(30000m, byMember[0].GetProperty("amount").GetDecimal());
        Assert.Equal("Катя", byMember[1].GetProperty("userName").GetString());
        Assert.Equal(5000m, byMember[1].GetProperty("amount").GetDecimal());
        Assert.Equal(35000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет расчёт месячных изменений относительно предшествующего месяца.</summary>
    public async Task GetSavingsSummary_WithIncreasingAndDecreasingAmounts_ReturnsCorrectDeltas()
    {
        var ivanId = await CreateMemberAsync("ivan33", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");

        var to = Validation.Today().AddMonths(-1);
        var first = to.AddMonths(-(SavingsConstants.SummaryMonthCount - 1));
        var beforeWindow = first.AddMonths(-1);

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = beforeWindow.ToString(DateFormats.Month), userId = ivanId, containerId, amount = 30000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = first.ToString(DateFormats.Month), userId = ivanId, containerId, amount = 30000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = first.AddMonths(1).ToString(DateFormats.Month), userId = ivanId, containerId, amount = 15000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = first.AddMonths(3).ToString(DateFormats.Month), userId = ivanId, containerId, amount = 25000 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?to={to.ToString(DateFormats.Month)}");
        var deltas = result.Json.GetProperty("deltas").EnumerateArray().ToList();

        Assert.Equal(SavingsConstants.SummaryMonthCount, deltas.Count);
        AssertMonthDelta(deltas[0], first.ToString(DateFormats.Month), 0m);
        AssertMonthDelta(deltas[1], first.AddMonths(1).ToString(DateFormats.Month), -15000m);
        AssertMonthDelta(deltas[2], first.AddMonths(2).ToString(DateFormats.Month), -15000m);
        AssertMonthDelta(deltas[3], first.AddMonths(3).ToString(DateFormats.Month), 25000m);
    }

    [Fact]
    /// <summary>Проверяет ограничение будущего месяца текущим и исключение будущих записей.</summary>
    public async Task GetSavingsSummary_FutureEndMonth_ClampsToCurrentMonthAndExcludesFutureEntries()
    {
        var ivanId = await CreateMemberAsync("ivan35", "Иван");
        var depositId = await GetContainerIdAsync("Вклад");
        var cashId = await GetContainerIdAsync("Наличка");

        var today = Validation.Today();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var futureMonth = currentMonth.AddMonths(1);

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = currentMonth.ToString(DateFormats.Month), userId = ivanId, containerId = depositId, amount = 1000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = futureMonth.ToString(DateFormats.Month), userId = ivanId, containerId = cashId, amount = 9999 });

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?to={futureMonth.ToString(DateFormats.Month)}");

        Assert.Equal(200, result.Status);
        Assert.Equal(currentMonth.ToString(DateFormats.Month), result.Json.GetProperty("to").GetString());
        var months = result.Json.GetProperty("months").EnumerateArray().ToList();
        Assert.Equal(SavingsConstants.SummaryMonthCount, months.Count);
        AssertMonthAmount(months[11], currentMonth.ToString(DateFormats.Month), 1000m);
        Assert.Equal(1000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет исключение записей администратора из сводки накоплений.</summary>
    public async Task GetSavingsSummary_WithAdminEntries_ExcludesAdminAmounts()
    {
        var ivanId = await CreateMemberAsync("ivan34", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var adminId = await GetAdminUserIdAsync();

        var to = Validation.Today().AddMonths(-1);

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = to.ToString(DateFormats.Month), userId = ivanId, containerId, amount = 10000 });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        db.SavingsEntries.Add(new SavingsEntry
        {
            Month = to,
            UserId = adminId,
            ContainerId = containerId,
            Amount = 99999,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?to={to.ToString(DateFormats.Month)}");

        Assert.Equal(200, result.Status);
        var months = result.Json.GetProperty("months").EnumerateArray().ToList();
        AssertMonthAmount(months[11], to.ToString(DateFormats.Month), 10000m);
        var byMember = result.Json.GetProperty("byMember").EnumerateArray().ToList();
        Assert.Single(byMember);
        Assert.Equal("Иван", byMember[0].GetProperty("userName").GetString());
        Assert.Equal(10000m, result.Json.GetProperty("total").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет отклонение сводки с некорректным конечным месяцем.</summary>
    public async Task GetSavingsSummary_InvalidEndMonth_Returns400()
    {
        foreach (var to in new[] { TestConstants.InvalidMonth, "20261", "abc", "2026-00" })
        {
            var result = await SendAsync(HttpMethod.Get, $"/api/savings/summary?to={to}");
            Assert.Equal(400, result.Status);
            Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        }
    }

    [Fact]
    /// <summary>Проверяет использование текущего месяца по умолчанию в сводке.</summary>
    public async Task GetSavingsSummary_WithoutEndMonth_DefaultsToCurrentMonth()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/savings/summary");

        Assert.Equal(200, result.Status);
        var today = Validation.Today();
        Assert.Equal($"{today.Year}-{today.Month:00}", result.Json.GetProperty("to").GetString());
        Assert.Equal(SavingsConstants.SummaryMonthCount, result.Json.GetProperty("months").EnumerateArray().Count());
    }

    [Fact]
    /// <summary>Проверяет отказ в доступе к данным накоплений без авторизации.</summary>
    public async Task GetSavings_WithoutToken_Returns401()
    {
        using var anonymous = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/savings?month={TestConstants.September2026}");
        var response = await anonymous.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
