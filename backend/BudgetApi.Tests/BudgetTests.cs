using System.Globalization;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет HTTP-сценарии месячного бюджета и его позиций.</summary>
public class BudgetTests : ApiTestBase
{
    private static (string Key, DateOnly First) LastMonth()
    {
        var now = Validation.Today();
        var first = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
        return (first.ToString(DateFormats.Month), first);
    }

    private static (string Key, DateOnly First) MonthBeforeLast()
    {
        var (key, first) = LastMonth();
        var prev = first.AddMonths(-1);
        return (prev.ToString(DateFormats.Month), prev);
    }

    [Fact]
    /// <summary>Проверяет создание бюджетной позиции и её появление в месячном плане.</summary>
    public async Task CreateBudgetItem_ValidRequest_Returns201AndAppearsInMonth()
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var created = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 15000, note = "основной запас" });
        Assert.Equal(201, created.Status);
        var id = created.Json.GetProperty("id").GetInt64();
        Assert.Equal(key, created.Json.GetProperty("month").GetString());
        Assert.Equal(categoryId, created.Json.GetProperty("categoryId").GetInt32());
        Assert.Equal(15000m, created.Json.GetProperty("amount").GetDecimal());
        Assert.Equal("основной запас", created.Json.GetProperty("note").GetString());

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        Assert.Equal(200, got.Status);
        Assert.Equal(key, got.Json.GetProperty("month").GetString());
        var ids = got.Json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToList();
        Assert.Contains(id, ids);

        var item = got.Json.GetProperty("items").EnumerateArray().First(i => i.GetProperty("id").GetInt64() == id);
        Assert.Equal("Супермаркет", item.GetProperty("categoryName").GetString());
        Assert.Equal("Продукты", item.GetProperty("categoryParentName").GetString());
        Assert.Equal(15000m, item.GetProperty("amount").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет возможность создать несколько позиций для категории в одном месяце.</summary>
    public async Task CreateBudgetItem_DuplicateCategoryAndMonth_AllowsBothItems()
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var first = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 1000 });
        var second = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 2000 });

        Assert.Equal(201, first.Status);
        Assert.Equal(201, second.Status);

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        var items = got.Json.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("categoryId").GetInt32() == categoryId)
            .ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    /// <summary>Проверяет запрет планирования бюджета по категории доходов.</summary>
    public async Task CreateBudgetItem_WithIncomeCategory_Returns400()
    {
        var (key, _) = LastMonth();
        var salaryId = await GetCategoryIdAsync("Зарплата");

        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = salaryId, amount = 100 });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "categoryId");
        Assert.Equal("Бюджет можно планировать только по категориям расходов",
            result.Json.GetProperty("errors").GetProperty("categoryId").GetString());
    }

    [Fact]
    /// <summary>Проверяет отклонение бюджетной позиции с несуществующей категорией.</summary>
    public async Task CreateBudgetItem_WithMissingCategory_Returns400()
    {
        var (key, _) = LastMonth();

        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = 999999, amount = 100 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "categoryId");
        Assert.Equal("Категория не найдена", result.Json.GetProperty("errors").GetProperty("categoryId").GetString());
    }

    [Theory]
    [InlineData(TestConstants.InvalidMonth)]
    [InlineData("09-2026")]
    [InlineData("")]
    /// <summary>Проверяет отклонение бюджетной позиции с некорректным месяцем.</summary>
    /// <param name="month">Значение месяца в недопустимом формате.</param>
    public async Task CreateBudgetItem_InvalidMonth_Returns400(string month)
    {
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { month, categoryId, amount = 100 });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "month");
        Assert.Equal(DateValidationMessages.MonthFormat, result.Json.GetProperty("errors").GetProperty("month").GetString());
    }

    [Fact]
    /// <summary>Проверяет отклонение запроса месяца бюджета при отсутствии или ошибочном значении.</summary>
    public async Task GetBudgetMonth_MissingOrInvalidMonth_Returns400()
    {
        var missing = await SendAsync(HttpMethod.Get, "/api/budget");
        Assert.Equal(400, missing.Status);
        TestAsserts.HasError(missing, "month");

        var bad = await SendAsync(HttpMethod.Get, $"/api/budget?month={TestConstants.InvalidMonth}");
        Assert.Equal(400, bad.Status);
        TestAsserts.HasError(bad, "month");

        var empty = await SendAsync(HttpMethod.Get, "/api/budget?month=");
        Assert.Equal(400, empty.Status);
        TestAsserts.HasError(empty, "month");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1_000_000_000_000.00)]
    /// <summary>Проверяет отклонение бюджетной позиции с недопустимой суммой.</summary>
    /// <param name="amount">Недопустимое значение суммы позиции.</param>
    public async Task CreateBudgetItem_InvalidAmount_Returns400(decimal amount)
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    /// <summary>Проверяет отклонение бюджетной позиции с чрезмерно длинной заметкой.</summary>
    public async Task CreateBudgetItem_NoteAboveMaximumLength_Returns400()
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 100, note = new string('а', Validation.MaxNoteLength + 1) });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "note");
    }

    [Fact]
    /// <summary>Проверяет ошибки для всех обязательных полей при создании пустой бюджетной позиции.</summary>
    public async Task CreateBudgetItem_WithoutRequiredFields_Returns400WithAllFieldErrors()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/budget", new { });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "month");
        TestAsserts.HasError(result, "categoryId");
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    /// <summary>Проверяет обновление полей позиции и корректность временных меток.</summary>
    public async Task UpdateBudgetItem_ChangedFields_UpdatesValuesAndTimestamp()
    {
        var (key, _) = LastMonth();
        var foodId = await GetCategoryIdAsync("Супермаркет", "Продукты");
        var marketId = await GetCategoryIdAsync("Рынок", "Продукты");

        var created = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = foodId, amount = 100, note = "old" });
        var id = created.Json.GetProperty("id").GetInt64();
        var createdAt = DateTime.Parse(created.Json.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture);

        var updated = await SendAsync(HttpMethod.Put, $"/api/budget/{id}", new { month = key, categoryId = marketId, amount = 250, note = "new" });

        Assert.Equal(200, updated.Status);
        Assert.Equal(250m, updated.Json.GetProperty("amount").GetDecimal());
        Assert.Equal(marketId, updated.Json.GetProperty("categoryId").GetInt32());
        Assert.Equal("Рынок", updated.Json.GetProperty("categoryName").GetString());
        Assert.Equal("new", updated.Json.GetProperty("note").GetString());
        var updatedAt = DateTime.Parse(updated.Json.GetProperty("updatedAt").GetString()!, CultureInfo.InvariantCulture);
        Assert.True(updatedAt >= createdAt, $"expected updatedAt >= createdAt, got {updatedAt} < {createdAt}");
    }

    [Fact]
    /// <summary>Проверяет перемещение позиции между месячными бюджетами при обновлении.</summary>
    public async Task UpdateBudgetItem_ChangedMonth_MovesItemBetweenMonths()
    {
        var (key, _) = LastMonth();
        var (otherKey, _) = MonthBeforeLast();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var created = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 100 });
        var id = created.Json.GetProperty("id").GetInt64();

        var updated = await SendAsync(HttpMethod.Put, $"/api/budget/{id}", new { month = otherKey, categoryId, amount = 100 });
        Assert.Equal(200, updated.Status);
        Assert.Equal(otherKey, updated.Json.GetProperty("month").GetString());

        var oldMonth = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        Assert.DoesNotContain(oldMonth.Json.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetInt64() == id);

        var newMonth = await SendAsync(HttpMethod.Get, $"/api/budget?month={otherKey}");
        Assert.Contains(newMonth.Json.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    /// <summary>Проверяет ответы для обновления и удаления несуществующей бюджетной позиции.</summary>
    public async Task UpdateOrDeleteBudgetItem_MissingId_Returns404()
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var updated = await SendAsync(HttpMethod.Put, "/api/budget/999999", new { month = key, categoryId, amount = 100 });
        Assert.Equal(404, updated.Status);
        Assert.Equal(ApiErrorCodes.NotFound, updated.GetProperty<string>("code"));
        Assert.Equal("Позиция бюджета не найдена", updated.GetProperty<string>("message"));

        var deleted = await SendAsync(HttpMethod.Delete, "/api/budget/999999");
        Assert.Equal(404, deleted.Status);
        Assert.Equal(ApiErrorCodes.NotFound, deleted.GetProperty<string>("code"));
    }

    [Fact]
    /// <summary>Проверяет удаление бюджетной позиции из месячного плана.</summary>
    public async Task DeleteBudgetItem_ExistingItem_RemovesItFromMonth()
    {
        var (key, _) = LastMonth();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var created = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 100 });
        var id = created.Json.GetProperty("id").GetInt64();

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/budget/{id}");
        Assert.Equal(204, deleted.Status);

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        Assert.DoesNotContain(got.Json.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    /// <summary>Проверяет агрегирование месячных итогов по всем пользователям без доходов.</summary>
    public async Task GetBudgetMonth_WithTransactions_AggregatesExpensesAcrossUsers()
    {
        var (key, first) = LastMonth();
        var foodId = await GetCategoryIdAsync("Супермаркет", "Продукты");
        var taxiId = await GetCategoryIdAsync("Такси", "Транспорт");
        var salaryId = await GetCategoryIdAsync("Зарплата");
        var date = first.AddDays(5).ToString(DateFormats.Date);

        var secondUser = await SendAsync(HttpMethod.Post, "/api/users", new { username = "ivan", displayName = "Иван", password = "secret123" });
        Assert.Equal(201, secondUser.Status);
        var ivanId = secondUser.Json.GetProperty("id").GetInt32();

        await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = foodId, amount = 1000 });
        await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = taxiId, amount = 500 });

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 400, date, categoryId = foodId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 300, date, categoryId = taxiId, userId = ivanId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 100, date, categoryId = foodId, userId = ivanId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "INCOME", amount = 5000, date, categoryId = salaryId, userId = ivanId });

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        Assert.Equal(200, got.Status);
        var totals = got.Json.GetProperty("totals");
        Assert.Equal(1500m, totals.GetProperty("planned").GetDecimal());
        Assert.Equal(800m, totals.GetProperty("actual").GetDecimal());
        Assert.Equal(700m, totals.GetProperty("diff").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет план, факт, разницу и фильтрацию категорий месячного бюджета.</summary>
    public async Task GetBudgetMonth_WithCategoryActivity_ReturnsPlanActualDiffAndFilteredCategories()
    {
        var (key, first) = LastMonth();
        var foodId = await GetCategoryIdAsync("Супермаркет", "Продукты");
        var taxiId = await GetCategoryIdAsync("Такси", "Транспорт");
        var pharmacyId = await GetCategoryIdAsync("Аптека", "Здоровье");
        var fuelId = await GetCategoryIdAsync("Бензин", "Транспорт");
        var date = first.AddDays(5).ToString(DateFormats.Date);

        await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = foodId, amount = 1000 });
        await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId = pharmacyId, amount = 500 });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 400, date, categoryId = foodId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 800, date, categoryId = taxiId });

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        Assert.Equal(200, got.Status);
        var rows = got.Json.GetProperty("categories").EnumerateArray().ToList();

        Assert.Equal(3, rows.Count);

        var food = rows.Single(r => r.GetProperty("categoryId").GetInt32() == foodId);
        Assert.Equal(1000m, food.GetProperty("planned").GetDecimal());
        Assert.Equal(400m, food.GetProperty("actual").GetDecimal());
        Assert.Equal(600m, food.GetProperty("diff").GetDecimal());

        var taxi = rows.Single(r => r.GetProperty("categoryId").GetInt32() == taxiId);
        Assert.Equal(0m, taxi.GetProperty("planned").GetDecimal());
        Assert.Equal(800m, taxi.GetProperty("actual").GetDecimal());
        Assert.Equal(-800m, taxi.GetProperty("diff").GetDecimal());

        var pharmacy = rows.Single(r => r.GetProperty("categoryId").GetInt32() == pharmacyId);
        Assert.Equal(500m, pharmacy.GetProperty("planned").GetDecimal());
        Assert.Equal(0m, pharmacy.GetProperty("actual").GetDecimal());
        Assert.Equal(500m, pharmacy.GetProperty("diff").GetDecimal());

        Assert.DoesNotContain(rows, r => r.GetProperty("categoryId").GetInt32() == fuelId);
    }

    [Fact]
    /// <summary>Проверяет исключение позиций других месяцев из ответа месячного бюджета.</summary>
    public async Task GetBudgetMonth_WithItemsFromOtherMonths_ExcludesOtherMonthItems()
    {
        var (key, _) = LastMonth();
        var (otherKey, _) = MonthBeforeLast();
        var categoryId = await GetCategoryIdAsync("Супермаркет", "Продукты");

        var current = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 100 });
        var other = await SendAsync(HttpMethod.Post, "/api/budget", new { month = otherKey, categoryId, amount = 200 });
        var currentId = current.Json.GetProperty("id").GetInt64();
        var otherId = other.Json.GetProperty("id").GetInt64();

        var got = await SendAsync(HttpMethod.Get, $"/api/budget?month={key}");
        var ids = got.Json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToList();

        Assert.Contains(currentId, ids);
        Assert.DoesNotContain(otherId, ids);
    }

    [Fact]
    /// <summary>Проверяет запрет удаления категории, используемой бюджетными позициями.</summary>
    public async Task DeleteCategory_WithBudgetItems_Returns409()
    {
        var (key, _) = LastMonth();

        var created = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестБюджет", kind = "EXPENSE" });
        Assert.Equal(201, created.Status);
        var categoryId = created.Json.GetProperty("id").GetInt32();

        var item = await SendAsync(HttpMethod.Post, "/api/budget", new { month = key, categoryId, amount = 100 });
        Assert.Equal(201, item.Status);

        var result = await SendAsync(HttpMethod.Delete, $"/api/categories/{categoryId}");

        Assert.Equal(409, result.Status);
        Assert.Equal(ApiErrorCodes.Conflict, result.GetProperty<string>("code"));
        Assert.Equal("В категории есть плановые бюджеты — сначала удалите их", result.GetProperty<string>("message"));
    }
}
