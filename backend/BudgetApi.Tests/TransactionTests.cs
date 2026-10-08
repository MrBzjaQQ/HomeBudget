using System.Text;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет HTTP-сценарии создания, выборки, обновления и удаления операций.</summary>
public class TransactionTests : ApiTestBase
{
    private static DateOnly Today => Validation.Today();

    [Fact]
    /// <summary>Проверяет создание корректной расходной операции и её появление в списке.</summary>
    public async Task CreateTransaction_ValidExpense_Returns201AndAppearsInList()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");
        var date = Today;

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 1250.50,
            date = date.ToString(DateFormats.Date),
            categoryId,
            note = "Продукты, Пятёрочка",
        });

        Assert.Equal(201, created.Status);

        var list = await SendAsync(HttpMethod.Get, $"/api/transactions?from={date.ToString(DateFormats.Date)}&to={date.ToString(DateFormats.Date)}");
        Assert.Equal(200, list.Status);
        Assert.True(list.Json.GetProperty("total").GetInt32() >= 1);
        Assert.True(list.Json.GetProperty("totals").GetProperty("expense").GetDecimal() >= 1250.50m);
    }

    [Fact]
    /// <summary>Проверяет отклонение операции с будущей датой.</summary>
    public async Task CreateTransaction_FutureDate_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");
        var future = Today.AddDays(1);

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = future.ToString(DateFormats.Date),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "date");
    }

    [Fact]
    /// <summary>Проверяет отклонение операции с нулевой суммой.</summary>
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 0,
            date = Today.ToString(DateFormats.Date),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    /// <summary>Проверяет отклонение операции с суммой выше допустимого предела.</summary>
    public async Task CreateTransaction_AmountAboveLimit_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 1_000_000_000_000.00m,
            date = Today.ToString(DateFormats.Date),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    /// <summary>Проверяет отклонение операции с категорией несовместимого типа.</summary>
    public async Task CreateTransaction_CategoryKindMismatch_Returns400()
    {
        var salaryId = await GetCategoryIdAsync("Зарплата");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString(DateFormats.Date),
            categoryId = salaryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "categoryId");
    }

    [Fact]
    /// <summary>Проверяет отклонение операции с заметкой длиннее допустимого предела.</summary>
    public async Task CreateTransaction_NoteAboveMaximumLength_Returns400()
    {
        var foodId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString(DateFormats.Date),
            categoryId = foodId,
            note = new string('а', Validation.MaxNoteLength + 1),
        });

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "note");
    }

    [Fact]
    /// <summary>Проверяет сохранение заметки предельной длины при создании операции.</summary>
    public async Task CreateTransaction_NoteAtMaximumLength_Returns201AndStoresNote()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var note = new string('б', Validation.MaxNoteLength);

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString(DateFormats.Date),
            categoryId = foodId,
            note,
        });

        Assert.Equal(201, created.Status);
        Assert.Equal(note, created.Json.GetProperty("note").GetString());

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/transactions/{created.Json.GetProperty("id").GetInt64()}");
        Assert.Equal(204, deleted.Status);
    }

    [Fact]
    /// <summary>Проверяет безопасную ошибку для некорректной даты в JSON-теле.</summary>
    public async Task CreateTransaction_InvalidDateInBody_Returns400WithoutInternals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = new StringContent(
                $"{{\"kind\":\"EXPENSE\",\"amount\":10,\"date\":\"2026-13-32\",\"categoryId\":{foodId}}}",
                Encoding.UTF8,
                "application/json"),
        };
        var response = await Client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(400, (int)response.StatusCode);
        Assert.DoesNotContain("System.", raw);
        Assert.DoesNotContain("\"request\"", raw);
        Assert.Contains("Некорректная дата", raw);
    }

    [Fact]
    /// <summary>Проверяет безопасную ошибку для неизвестного типа операции в JSON-теле.</summary>
    public async Task CreateTransaction_InvalidKindInBody_Returns400WithoutInternals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = new StringContent(
                $"{{\"kind\":\"MIXED\",\"amount\":10,\"date\":\"{Today.ToString(DateFormats.Date)}\",\"categoryId\":{foodId}}}",
                Encoding.UTF8,
                "application/json"),
        };
        var response = await Client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(400, (int)response.StatusCode);
        Assert.DoesNotContain("System.", raw);
        Assert.DoesNotContain("BudgetApi.", raw);
        Assert.DoesNotContain("\"request\"", raw);
        Assert.Contains("Некорректный тип операции", raw);
    }

    [Fact]
    /// <summary>Проверяет безопасную ошибку при некорректном JSON операции.</summary>
    public async Task CreateTransaction_MalformedJson_Returns400WithoutInternals()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = new StringContent("'not json at all", Encoding.UTF8, "application/json"),
        };
        var response = await Client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(400, (int)response.StatusCode);
        Assert.DoesNotContain("System.", raw);
        Assert.DoesNotContain("\"request\"", raw);
        Assert.Contains("Некорректные данные запроса", raw);
    }

    [Fact]
    /// <summary>Проверяет совместную работу фильтров списка и соответствие итогов.</summary>
    public async Task ListTransactions_CombinedFilters_ReturnsMatchingRowsAndTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var salaryId = await GetCategoryIdAsync("Зарплата");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 100, date = dateStr, categoryId = foodId, note = "молоко" });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 50, date = dateStr, categoryId = foodId, note = "хлеб" });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "INCOME", amount = 1000, date = dateStr, categoryId = salaryId, note = "зарплата" });

        var byNote = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&kind=EXPENSE&q=молоко");
        Assert.Equal(1, byNote.Json.GetProperty("total").GetInt32());
        Assert.Equal(100m, byNote.Json.GetProperty("totals").GetProperty("expense").GetDecimal());

        var allExpenses = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&kind=EXPENSE");
        Assert.Equal(2, allExpenses.Json.GetProperty("total").GetInt32());
        var totals = allExpenses.Json.GetProperty("totals");
        Assert.Equal(150m, totals.GetProperty("expense").GetDecimal());
        Assert.Equal(0m, totals.GetProperty("income").GetDecimal());
        Assert.Equal(-150m, totals.GetProperty("net").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет удаление операции и пересчёт итоговых сумм.</summary>
    public async Task DeleteTransaction_ExistingRecord_RemovesItAndRecomputesTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 77, date = dateStr, categoryId = foodId });
        var id = created.Json.GetProperty("id").GetInt64();

        var before = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}");
        Assert.True(before.Json.GetProperty("totals").GetProperty("expense").GetDecimal() >= 77m);

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/transactions/{id}");
        Assert.Equal(204, deleted.Status);

        var after = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}");
        var expense = after.Json.GetProperty("totals").GetProperty("expense").GetDecimal();
        Assert.True(expense < 77m || expense == 0m);
    }

    [Fact]
    /// <summary>Проверяет обновление полей операции.</summary>
    public async Task UpdateTransaction_ChangedFields_ReturnsUpdatedTransaction()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 10, date = dateStr, categoryId = foodId, note = "old" });
        var id = created.Json.GetProperty("id").GetInt64();

        var updated = await SendAsync(HttpMethod.Put, $"/api/transactions/{id}", new { kind = "EXPENSE", amount = 25, date = dateStr, categoryId = foodId, note = "new" });
        Assert.Equal(200, updated.Status);
        Assert.Equal(25m, updated.Json.GetProperty("amount").GetDecimal());
        Assert.Equal("new", updated.Json.GetProperty("note").GetString());
    }

    [Fact]
    /// <summary>Проверяет разбиение списка операций на страницы заданного размера.</summary>
    public async Task ListTransactions_MoreThanOnePage_ReturnsConfiguredPageSize()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        for (var i = 0; i < 60; i++)
        {
            await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = i + 1, date = dateStr, categoryId = foodId });
        }

        var page1 = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&page=1&pageSize=50");
        var items1 = page1.Json.GetProperty("items").EnumerateArray().Count();
        Assert.Equal(50, items1);
        Assert.Equal(60, page1.Json.GetProperty("total").GetInt32());

        var page2 = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&page=2&pageSize=50");
        var items2 = page2.Json.GetProperty("items").EnumerateArray().Count();
        Assert.Equal(10, items2);
    }

    [Fact]
    /// <summary>Проверяет получение итогов операций с группировкой по категории.</summary>
    public async Task GetTransactionSummary_GroupedByCategory_ReturnsCategoryTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 300, date = dateStr, categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={dateStr}&to={dateStr}&groupBy=category");
        Assert.Equal(200, result.Status);
        var rows = result.Json.GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("categoryId").GetInt32() == foodId).ToList();
        Assert.Single(rows);
        Assert.Equal(300m, rows[0].GetProperty("expense").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет включение операций подкатегорий в фильтр по группе категорий.</summary>
    public async Task ListTransactions_FilteredByParentCategory_IncludesChildTransactions()
    {
        var groupId = await GetCategoryIdAsync("Продукты");
        var childId = await GetCategoryIdAsync("Супермаркет", "Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 555, date = dateStr, categoryId = childId, note = "групповой-тест" });
        var id = created.Json.GetProperty("id").GetInt64();

        try
        {
            var byGroup = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&categoryId={groupId}");
            Assert.Equal(200, byGroup.Status);
            var groupIds = byGroup.Json.GetProperty("items").EnumerateArray().Select(t => t.GetProperty("categoryId").GetInt32()).ToList();
            Assert.Contains(childId, groupIds);

            var byChild = await SendAsync(HttpMethod.Get, $"/api/transactions?from={dateStr}&to={dateStr}&categoryId={childId}");
            Assert.Equal(200, byChild.Status);
            var childIds = byChild.Json.GetProperty("items").EnumerateArray().Select(t => t.GetProperty("categoryId").GetInt32()).ToList();
            Assert.Contains(childId, childIds);

            Assert.True(byGroup.Json.GetProperty("total").GetInt32() >= byChild.Json.GetProperty("total").GetInt32());
        }
        finally
        {
            await SendAsync(HttpMethod.Delete, $"/api/transactions/{id}");
        }
    }

    [Fact]
    /// <summary>Проверяет получение ключа месяца при группировке итогов по месяцу.</summary>
    public async Task GetTransactionSummary_GroupedByMonth_ReturnsMonthKeyAndTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString(DateFormats.Date);

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 300, date = dateStr, categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={dateStr}&to={dateStr}&groupBy=month");
        Assert.Equal(200, result.Status);
        var expectedKey = $"{date.Year:D4}-{date.Month:D2}";
        var rows = result.Json.GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("month").GetString() == expectedKey).ToList();
        Assert.Single(rows);
        Assert.Equal(300m, rows[0].GetProperty("expense").GetDecimal());
    }

    [Fact]
    /// <summary>Проверяет отклонение неподдерживаемого значения группировки итогов.</summary>
    public async Task GetTransactionSummary_UnsupportedGrouping_Returns400()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/transactions/summary?groupBy=day");

        Assert.Equal(400, result.Status);
        Assert.Equal(ApiErrorCodes.ValidationFailed, result.GetProperty<string>("code"));
        Assert.Contains("groupBy", result.GetProperty<string>("message"));
    }

    [Fact]
    /// <summary>Проверяет сортировку месячных итогов по возрастанию.</summary>
    public async Task GetTransactionSummary_GroupedByMonth_ReturnsRowsInAscendingOrder()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var firstOfMonth = new DateOnly(Today.Year, Today.Month, 1);
        var earlierDate = firstOfMonth.AddMonths(-1).AddDays(5);

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 10, date = earlierDate.ToString(DateFormats.Date), categoryId = foodId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 20, date = Today.ToString(DateFormats.Date), categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={earlierDate.ToString(DateFormats.Date)}&to={Today.ToString(DateFormats.Date)}&groupBy=month");
        Assert.Equal(200, result.Status);

        var months = result.Json.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("month").GetString()!).ToList();
        Assert.Equal(2, months.Count);
        Assert.True(string.CompareOrdinal(months[0], months[1]) < 0, $"Expected ascending months, got {string.Join(", ", months)}");
    }
}
