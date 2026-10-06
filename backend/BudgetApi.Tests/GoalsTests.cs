using System.Globalization;
using System.Text.Json;
using Budget.Application.Common;
using Xunit;

namespace BudgetApi.Tests;

public class GoalsTests : ApiTestBase
{
    private async Task<int> CreateMemberAsync(string username, string displayName)
    {
        var created = await SendAsync(HttpMethod.Post, "/api/users", new { username, displayName, password = "secret123" });
        Assert.Equal(201, created.Status);
        return created.Json.GetProperty("id").GetInt32();
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

    private async Task<DateOnly> GetServerDateAsync()
    {
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", new { username = "admin", password = "admin123" });
        Assert.Equal(200, login.Status);
        return DateOnly.ParseExact(login.GetProperty<string>("serverDate")!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static JsonElement FindGoal(JsonElement list, int id)
    {
        foreach (var goal in list.EnumerateArray())
        {
            if (goal.GetProperty("id").GetInt32() == id)
            {
                return goal;
            }
        }

        throw new InvalidOperationException($"Goal {id} not found");
    }

    [Fact]
    public async Task List_Empty_Returns200_WithEmptyArray()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/goals");

        Assert.Equal(200, result.Status);
        Assert.Equal(JsonValueKind.Array, result.Json.ValueKind);
        Assert.Empty(result.Json.EnumerateArray());
    }

    [Fact]
    public async Task Create_Valid_Returns201_AndVisibleInList_WithContainerData()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Отпуск", targetAmount = 150000, containerId });

        Assert.Equal(201, created.Status);
        var id = created.Json.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal("Отпуск", created.Json.GetProperty("name").GetString());
        Assert.Equal(150000m, created.Json.GetProperty("targetAmount").GetDecimal());
        Assert.Equal(containerId, created.Json.GetProperty("containerId").GetInt32());
        Assert.Equal("Вклад", created.Json.GetProperty("containerName").GetString());
        Assert.Equal("#0f766e", created.Json.GetProperty("containerColor").GetString());
        Assert.Equal(0m, created.Json.GetProperty("saved").GetDecimal());
        Assert.False(created.Json.GetProperty("achieved").GetBoolean());
        Assert.NotNull(created.Json.GetProperty("createdAt").GetString());

        var list = await SendAsync(HttpMethod.Get, "/api/goals");
        Assert.Equal(200, list.Status);
        var item = FindGoal(list.Json, id);
        Assert.Equal("Отпуск", item.GetProperty("name").GetString());
        Assert.Equal(containerId, item.GetProperty("containerId").GetInt32());
        Assert.Equal("Вклад", item.GetProperty("containerName").GetString());
        Assert.Equal("#0f766e", item.GetProperty("containerColor").GetString());
        Assert.Equal(0m, item.GetProperty("saved").GetDecimal());
        Assert.False(item.GetProperty("achieved").GetBoolean());
    }

    [Fact]
    public async Task Create_EmptyOrWhitespaceName_Returns400_WithErrorName()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        foreach (var name in new[] { "", "   " })
        {
            var result = await SendAsync(HttpMethod.Post, "/api/goals", new { name, targetAmount = 1000, containerId });
            Assert.Equal(400, result.Status);
            Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
            TestAsserts.HasError(result, "name");
        }
    }

    [Fact]
    public async Task Create_NameTooLong_Returns400_WithErrorName()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Post, "/api/goals", new { name = new string('ц', 101), targetAmount = 1000, containerId });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    public async Task Create_InvalidTargetAmount_Returns400_WithErrorTargetAmount()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        foreach (var targetAmount in new[] { 0m, -100m, Validation.MaxAmount + 0.01m })
        {
            var result = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount, containerId });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "targetAmount");
        }
    }

    [Fact]
    public async Task Create_UnknownContainer_Returns400_WithErrorContainerId()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId = 99999 });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "containerId");
        Assert.Equal("Счёт не найден", result.Json.GetProperty("errors").GetProperty("containerId").GetString());
    }

    [Fact]
    public async Task Create_InvalidDueDate_Returns400_WithErrorDueDate()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        foreach (var dueDate in new[] { "2026-13-01", "01-01-2026", "" })
        {
            var result = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId, dueDate });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "dueDate");
            Assert.Equal("Формат даты: yyyy-MM-dd", result.Json.GetProperty("errors").GetProperty("dueDate").GetString());
        }
    }

    [Fact]
    public async Task Create_ValidDueDate_Returns201_WithDueDateInResponse()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId, dueDate = "2026-12-31" });

        Assert.Equal(201, created.Status);
        Assert.Equal("2026-12-31", created.Json.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Create_WithoutDueDate_Returns201_NullDueDateAndNullMonthlyRequired()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "БезДаты", targetAmount = 1000, containerId });

        Assert.Equal(201, created.Status);
        Assert.Equal(JsonValueKind.Null, created.Json.GetProperty("dueDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.Json.GetProperty("monthlyRequired").ValueKind);
    }

    [Fact]
    public async Task Create_EmptyBody_Returns400_WithEmptyBodyMessage()
    {
        // «Пустое тело» = явный JSON null с Content-Type: application/json
        // (запрос без Content-Type получает 415 ещё до model binding).
        // По контракту (spec.md §8) ожидается 400 + "Пустое тело запроса"
        // (ModelStateSanitizer.EmptyBodyMessage): в .NET 10 тело до метода не доходит,
        // автоматический 400 формируется InvalidModelStateResponseFactory → ModelStateSanitizer.
        // Фактически возвращается "Некорректные данные" + errors {"": "A non-empty request
        // body is required."} — sanitizer не знает новый формат model error .NET 10 (баг, см. отчёт).
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/goals")
        {
            Content = System.Net.Http.Json.JsonContent.Create<object?>(null),
        };
        var response = await Client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var result = new ApiResult((int)response.StatusCode, JsonDocument.Parse(raw).RootElement.Clone(), raw);

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        Assert.Equal("Пустое тело запроса", result.GetProperty<string>("message"));
    }

    [Fact]
    public async Task Saved_SumsLastMonthWithEntries_IgnoresEarlierAndFutureMonths()
    {
        var serverDate = await GetServerDateAsync();
        var ivanId = await CreateMemberAsync("ivan40", "Иван");
        var katyaId = await CreateMemberAsync("katya40", "Катя");
        var containerId = await GetContainerIdAsync("Наличка");

        var lastMonth = serverDate.AddMonths(-1);
        var earlierMonth = serverDate.AddMonths(-2);

        await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{earlierMonth:yyyy-MM}", userId = ivanId, containerId, amount = 99999 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{lastMonth:yyyy-MM}", userId = ivanId, containerId, amount = 10000 });
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{lastMonth:yyyy-MM}", userId = katyaId, containerId, amount = 20000 });

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Отпуск", targetAmount = 100000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var list = await SendAsync(HttpMethod.Get, "/api/goals");
        var goal = FindGoal(list.Json, goalId);
        Assert.Equal(30000m, goal.GetProperty("saved").GetDecimal());
        Assert.False(goal.GetProperty("achieved").GetBoolean());

        var futureMonth = serverDate.AddMonths(1);
        await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{futureMonth:yyyy-MM}", userId = ivanId, containerId, amount = 88888 });

        var listAfterFutureEntry = await SendAsync(HttpMethod.Get, "/api/goals");
        Assert.Equal(30000m, FindGoal(listAfterFutureEntry.Json, goalId).GetProperty("saved").GetDecimal());
    }

    [Fact]
    public async Task MonthlyRequired_DividesEvenly_ReturnsExactAmount()
    {
        var serverDate = await GetServerDateAsync();
        var containerId = await GetContainerIdAsync("Вклад");
        var dueDate = serverDate.AddMonths(2);

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 90000, containerId, dueDate = $"{dueDate:yyyy-MM-dd}" });

        Assert.Equal(201, created.Status);
        Assert.Equal(0m, created.Json.GetProperty("saved").GetDecimal());
        Assert.False(created.Json.GetProperty("achieved").GetBoolean());
        Assert.Equal(30000m, created.Json.GetProperty("monthlyRequired").GetDecimal());
    }

    [Fact]
    public async Task MonthlyRequired_DoesNotDivideEvenly_RoundsUpToWholeRuble()
    {
        var serverDate = await GetServerDateAsync();
        var containerId = await GetContainerIdAsync("Вклад");
        var dueDate = serverDate.AddMonths(1);

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 100001, containerId, dueDate = $"{dueDate:yyyy-MM-dd}" });

        Assert.Equal(201, created.Status);
        Assert.Equal(50001m, created.Json.GetProperty("monthlyRequired").GetDecimal());
    }

    [Fact]
    public async Task MonthlyRequired_DueDateInCurrentMonth_EqualsRemainingAmount()
    {
        var serverDate = await GetServerDateAsync();
        var containerId = await GetContainerIdAsync("Вклад");

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 75000, containerId, dueDate = $"{serverDate:yyyy-MM-dd}" });

        Assert.Equal(201, created.Status);
        Assert.False(created.Json.GetProperty("achieved").GetBoolean());
        Assert.Equal(75000m, created.Json.GetProperty("monthlyRequired").GetDecimal());
    }

    [Fact]
    public async Task Goal_Achieved_ReturnsAchievedTrueAndNullMonthlyRequired()
    {
        var serverDate = await GetServerDateAsync();
        var ivanId = await CreateMemberAsync("ivan44", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var lastMonth = serverDate.AddMonths(-1);
        var dueDate = serverDate.AddMonths(2);

        var entry = await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{lastMonth:yyyy-MM}", userId = ivanId, containerId, amount = 100000 });
        Assert.Equal(201, entry.Status);

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Отпуск", targetAmount = 100000, containerId, dueDate = $"{dueDate:yyyy-MM-dd}" });

        Assert.Equal(201, created.Status);
        Assert.Equal(100000m, created.Json.GetProperty("saved").GetDecimal());
        Assert.True(created.Json.GetProperty("achieved").GetBoolean());
        Assert.Equal(JsonValueKind.Null, created.Json.GetProperty("monthlyRequired").ValueKind);
    }

    [Fact]
    public async Task Goal_PastDueDate_ReturnsNullMonthlyRequiredAndNotAchieved()
    {
        var serverDate = await GetServerDateAsync();
        var containerId = await GetContainerIdAsync("Вклад");
        var pastDate = serverDate.AddDays(-1);

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 50000, containerId, dueDate = $"{pastDate:yyyy-MM-dd}" });

        Assert.Equal(201, created.Status);
        Assert.Equal(JsonValueKind.Null, created.Json.GetProperty("monthlyRequired").ValueKind);
        Assert.False(created.Json.GetProperty("achieved").GetBoolean());
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsUpdatedDto_AndPersistsInList()
    {
        var initialContainerId = await GetContainerIdAsync("Вклад");
        var targetContainerId = await GetContainerIdAsync("Наличка");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Старая цель", targetAmount = 1000, containerId = initialContainerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();
        var createdAt = created.Json.GetProperty("createdAt").GetDateTime();
        var dueDate = (await GetServerDateAsync()).AddMonths(3);

        var updated = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new
        {
            name = "  Новый отпуск  ",
            targetAmount = 90000,
            containerId = targetContainerId,
            dueDate = $"{dueDate:yyyy-MM-dd}",
        });

        Assert.Equal(200, updated.Status);
        Assert.Equal(goalId, updated.Json.GetProperty("id").GetInt32());
        Assert.Equal("Новый отпуск", updated.Json.GetProperty("name").GetString());
        Assert.Equal(90000m, updated.Json.GetProperty("targetAmount").GetDecimal());
        Assert.Equal(targetContainerId, updated.Json.GetProperty("containerId").GetInt32());
        Assert.Equal("Наличка", updated.Json.GetProperty("containerName").GetString());
        Assert.Equal("#f59e0b", updated.Json.GetProperty("containerColor").GetString());
        Assert.Equal($"{dueDate:yyyy-MM-dd}", updated.Json.GetProperty("dueDate").GetString());
        Assert.Equal(0m, updated.Json.GetProperty("saved").GetDecimal());
        Assert.False(updated.Json.GetProperty("achieved").GetBoolean());
        Assert.Equal(22500m, updated.Json.GetProperty("monthlyRequired").GetDecimal());
        Assert.Equal(createdAt, updated.Json.GetProperty("createdAt").GetDateTime());

        var list = await SendAsync(HttpMethod.Get, "/api/goals");
        Assert.Equal(200, list.Status);
        var persisted = FindGoal(list.Json, goalId);
        Assert.Equal("Новый отпуск", persisted.GetProperty("name").GetString());
        Assert.Equal(90000m, persisted.GetProperty("targetAmount").GetDecimal());
        Assert.Equal(targetContainerId, persisted.GetProperty("containerId").GetInt32());
        Assert.Equal("Наличка", persisted.GetProperty("containerName").GetString());
        Assert.Equal("#f59e0b", persisted.GetProperty("containerColor").GetString());
        Assert.Equal($"{dueDate:yyyy-MM-dd}", persisted.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Update_ChangesContainer_LeavesSavingsEntriesIntact_AndRecalculatesDto()
    {
        var serverDate = await GetServerDateAsync();
        var month = $"{serverDate:yyyy-MM}";
        var userId = await CreateMemberAsync("goalupdate1", "Иван");
        var initialContainerId = await GetContainerIdAsync("Вклад");
        var targetContainerId = await GetContainerIdAsync("Наличка");
        var initialEntry = await SendAsync(HttpMethod.Post, "/api/savings", new { month, userId, containerId = initialContainerId, amount = 12000 });
        var targetEntry = await SendAsync(HttpMethod.Post, "/api/savings", new { month, userId, containerId = targetContainerId, amount = 8000 });
        Assert.Equal(201, initialEntry.Status);
        Assert.Equal(201, targetEntry.Status);
        var initialEntryId = initialEntry.Json.GetProperty("id").GetInt64();
        var targetEntryId = targetEntry.Json.GetProperty("id").GetInt64();

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Покупка", targetAmount = 100000, containerId = initialContainerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new
        {
            name = "Покупка",
            targetAmount = 50000,
            containerId = targetContainerId,
            dueDate = (string?)null,
        });

        Assert.Equal(200, updated.Status);
        Assert.Equal(targetContainerId, updated.Json.GetProperty("containerId").GetInt32());
        Assert.Equal("Наличка", updated.Json.GetProperty("containerName").GetString());
        Assert.Equal(8000m, updated.Json.GetProperty("saved").GetDecimal());
        Assert.False(updated.Json.GetProperty("achieved").GetBoolean());
        Assert.Equal(JsonValueKind.Null, updated.Json.GetProperty("monthlyRequired").ValueKind);

        var entries = await SendAsync(HttpMethod.Get, $"/api/savings?month={month}");
        Assert.Equal(200, entries.Status);
        var persistedInitialEntry = entries.Json.GetProperty("entries").EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == initialEntryId);
        var persistedTargetEntry = entries.Json.GetProperty("entries").EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == targetEntryId);
        Assert.Equal(initialContainerId, persistedInitialEntry.GetProperty("containerId").GetInt32());
        Assert.Equal(12000m, persistedInitialEntry.GetProperty("amount").GetDecimal());
        Assert.Equal(targetContainerId, persistedTargetEntry.GetProperty("containerId").GetInt32());
        Assert.Equal(8000m, persistedTargetEntry.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Update_NullDueDate_ClearsExistingDate()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId, dueDate = "2026-12-31" });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new
        {
            name = "Цель",
            targetAmount = 1000,
            containerId,
            dueDate = (string?)null,
        });

        Assert.Equal(200, updated.Status);
        Assert.Equal(JsonValueKind.Null, updated.Json.GetProperty("dueDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, updated.Json.GetProperty("monthlyRequired").ValueKind);
    }

    [Fact]
    public async Task Update_ValidDueDate_ReturnsDateAndMonthlyRequired()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();
        var dueDate = (await GetServerDateAsync()).AddMonths(2);

        var updated = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new
        {
            name = "Цель",
            targetAmount = 90000,
            containerId,
            dueDate = $"{dueDate:yyyy-MM-dd}",
        });

        Assert.Equal(200, updated.Status);
        Assert.Equal($"{dueDate:yyyy-MM-dd}", updated.Json.GetProperty("dueDate").GetString());
        Assert.Equal(30000m, updated.Json.GetProperty("monthlyRequired").GetDecimal());
    }

    [Fact]
    public async Task Update_EmptyOrWhitespaceName_Returns400_WithErrorName()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        foreach (var name in new[] { "", "   " })
        {
            var result = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new { name, targetAmount = 1000, containerId });
            Assert.Equal(400, result.Status);
            Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
            TestAsserts.HasError(result, "name");
        }
    }

    [Fact]
    public async Task Update_NameTooLong_Returns400_WithErrorName()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var result = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new { name = new string('ц', 101), targetAmount = 1000, containerId });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "name");
    }

    [Fact]
    public async Task Update_InvalidTargetAmount_Returns400_WithErrorTargetAmount()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        foreach (var targetAmount in new[] { 0m, -100m, Validation.MaxAmount + 0.01m })
        {
            var result = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new { name = "Цель", targetAmount, containerId });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "targetAmount");
        }
    }

    [Fact]
    public async Task Update_UnknownContainer_Returns400_WithErrorContainerId()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var result = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new { name = "Цель", targetAmount = 1000, containerId = 99999 });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "containerId");
        Assert.Equal("Счёт не найден", result.Json.GetProperty("errors").GetProperty("containerId").GetString());
    }

    [Fact]
    public async Task Update_InvalidDueDate_Returns400_WithErrorDueDate()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        foreach (var dueDate in new[] { "2026-13-01", "01-01-2026", "" })
        {
            var result = await SendAsync(HttpMethod.Put, $"/api/goals/{goalId}", new { name = "Цель", targetAmount = 1000, containerId, dueDate });
            Assert.Equal(400, result.Status);
            TestAsserts.HasError(result, "dueDate");
            Assert.Equal("Формат даты: yyyy-MM-dd", result.Json.GetProperty("errors").GetProperty("dueDate").GetString());
        }
    }

    [Fact]
    public async Task Update_Missing_Returns404_NotFound()
    {
        var containerId = await GetContainerIdAsync("Вклад");

        var result = await SendAsync(HttpMethod.Put, "/api/goals/99999", new { name = "Цель", targetAmount = 1000, containerId });

        Assert.Equal(404, result.Status);
        Assert.Equal("NotFound", result.GetProperty<string>("code"));
        Assert.Equal("Цель не найдена", result.GetProperty<string>("message"));
    }

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        var containerId = await GetContainerIdAsync("Вклад");
        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Цель", targetAmount = 1000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        using var anonymous = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/goals/{goalId}")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { name = "Изменённая цель", targetAmount = 2000, containerId }),
        };

        var response = await anonymous.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_GoneFromList_ContainerAndEntriesIntact()
    {
        var serverDate = await GetServerDateAsync();
        var ivanId = await CreateMemberAsync("ivan46", "Иван");
        var containerId = await GetContainerIdAsync("Вклад");
        var lastMonth = serverDate.AddMonths(-1);

        var entry = await SendAsync(HttpMethod.Post, "/api/savings", new { month = $"{lastMonth:yyyy-MM}", userId = ivanId, containerId, amount = 5000 });
        Assert.Equal(201, entry.Status);
        var entryId = entry.Json.GetProperty("id").GetInt64();

        var created = await SendAsync(HttpMethod.Post, "/api/goals", new { name = "Отпуск", targetAmount = 100000, containerId });
        Assert.Equal(201, created.Status);
        var goalId = created.Json.GetProperty("id").GetInt32();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/goals/{goalId}");
        Assert.Equal(204, deleted.Status);

        var goals = await SendAsync(HttpMethod.Get, "/api/goals");
        Assert.Empty(goals.Json.EnumerateArray());

        var containers = await SendAsync(HttpMethod.Get, "/api/savings/containers");
        Assert.Contains(containers.Json.EnumerateArray(), c => c.GetProperty("id").GetInt32() == containerId);

        var month = await SendAsync(HttpMethod.Get, $"/api/savings?month={lastMonth:yyyy-MM}");
        var item = month.Json.GetProperty("entries").EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == entryId);
        Assert.Equal(5000m, item.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Delete_Missing_Returns404_NotFound()
    {
        var result = await SendAsync(HttpMethod.Delete, "/api/goals/99999");

        Assert.Equal(404, result.Status);
        Assert.Equal("NotFound", result.GetProperty<string>("code"));
        Assert.Equal("Цель не найдена", result.GetProperty<string>("message"));
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns401()
    {
        using var anonymous = Factory.CreateClient();

        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/goals");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await anonymous.SendAsync(get)).StatusCode);

        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/goals")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { name = "Цель", targetAmount = 1000, containerId = 1 }),
        };
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await anonymous.SendAsync(post)).StatusCode);
    }
}
