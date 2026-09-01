using System.Text;
using Xunit;

namespace BudgetApi.Tests;

public class TransactionTests : ApiTestBase
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Create_ValidExpense_Returns201_AndAppearsInList()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");
        var date = Today;

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 1250.50,
            date = date.ToString("yyyy-MM-dd"),
            categoryId,
            note = "Продукты, Пятёрочка",
        });

        Assert.Equal(201, created.Status);

        var list = await SendAsync(HttpMethod.Get, $"/api/transactions?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}");
        Assert.Equal(200, list.Status);
        Assert.True(list.Json.GetProperty("total").GetInt32() >= 1);
        Assert.True(list.Json.GetProperty("totals").GetProperty("expense").GetDecimal() >= 1250.50m);
    }

    [Fact]
    public async Task Create_FutureDate_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");
        var future = Today.AddDays(1);

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = future.ToString("yyyy-MM-dd"),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "date");
    }

    [Fact]
    public async Task Create_ZeroAmount_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 0,
            date = Today.ToString("yyyy-MM-dd"),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    public async Task Create_AmountAboveLimit_Returns400()
    {
        var categoryId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 1_000_000_000_000.00m,
            date = Today.ToString("yyyy-MM-dd"),
            categoryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "amount");
    }

    [Fact]
    public async Task Create_CategoryKindMismatch_Returns400()
    {
        var salaryId = await GetCategoryIdAsync("Зарплата");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString("yyyy-MM-dd"),
            categoryId = salaryId,
        });

        Assert.Equal(400, result.Status);
        TestAsserts.HasError(result, "categoryId");
    }

    [Fact]
    public async Task Create_NoteAbove500Chars_Returns400()
    {
        var foodId = await GetCategoryIdAsync("Продукты");

        var result = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString("yyyy-MM-dd"),
            categoryId = foodId,
            note = new string('а', 501),
        });

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        TestAsserts.HasError(result, "note");
    }

    [Fact]
    public async Task Create_NoteExactly500Chars_Returns201_AndStoresNote()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var note = new string('б', 500);

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new
        {
            kind = "EXPENSE",
            amount = 100,
            date = Today.ToString("yyyy-MM-dd"),
            categoryId = foodId,
            note,
        });

        Assert.Equal(201, created.Status);
        Assert.Equal(note, created.Json.GetProperty("note").GetString());

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/transactions/{created.Json.GetProperty("id").GetInt64()}");
        Assert.Equal(204, deleted.Status);
    }

    [Fact]
    public async Task Create_BadDateInBody_Returns400_WithoutNetInternals()
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
    public async Task Create_BadKindInBody_Returns400_WithoutNetInternals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = new StringContent(
                $"{{\"kind\":\"MIXED\",\"amount\":10,\"date\":\"{Today:yyyy-MM-dd}\",\"categoryId\":{foodId}}}",
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
    public async Task Create_MalformedJson_Returns400_WithoutNetInternals()
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
    public async Task List_Filters_Combine_AndTotalsMatch()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var salaryId = await GetCategoryIdAsync("Зарплата");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

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
    public async Task Delete_RemovesTransaction_AndRecomputesTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

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
    public async Task Update_ChangesFields()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

        var created = await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 10, date = dateStr, categoryId = foodId, note = "old" });
        var id = created.Json.GetProperty("id").GetInt64();

        var updated = await SendAsync(HttpMethod.Put, $"/api/transactions/{id}", new { kind = "EXPENSE", amount = 25, date = dateStr, categoryId = foodId, note = "new" });
        Assert.Equal(200, updated.Status);
        Assert.Equal(25m, updated.Json.GetProperty("amount").GetDecimal());
        Assert.Equal("new", updated.Json.GetProperty("note").GetString());
    }

    [Fact]
    public async Task List_Pagination_Returns50PerPage()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

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
    public async Task Summary_ByCategory_ReturnsTotals()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 300, date = dateStr, categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={dateStr}&to={dateStr}&groupBy=category");
        Assert.Equal(200, result.Status);
        var rows = result.Json.GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("categoryId").GetInt32() == foodId).ToList();
        Assert.Single(rows);
        Assert.Equal(300m, rows[0].GetProperty("expense").GetDecimal());
    }

    [Fact]
    public async Task List_CategoryIdGroup_ExpandsToChildSubcategories()
    {
        var groupId = await GetCategoryIdAsync("Продукты");
        var childId = await GetCategoryIdAsync("Супермаркет", "Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

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
    public async Task Summary_ByMonth_ReturnsMonthKey()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var date = Today;
        string dateStr = date.ToString("yyyy-MM-dd");

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 300, date = dateStr, categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={dateStr}&to={dateStr}&groupBy=month");
        Assert.Equal(200, result.Status);
        var expectedKey = $"{date.Year:D4}-{date.Month:D2}";
        var rows = result.Json.GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("month").GetString() == expectedKey).ToList();
        Assert.Single(rows);
        Assert.Equal(300m, rows[0].GetProperty("expense").GetDecimal());
    }

    [Fact]
    public async Task Summary_InvalidGroupBy_Returns400()
    {
        var result = await SendAsync(HttpMethod.Get, "/api/transactions/summary?groupBy=day");

        Assert.Equal(400, result.Status);
        Assert.Equal("ValidationFailed", result.GetProperty<string>("code"));
        Assert.Contains("groupBy", result.GetProperty<string>("message"));
    }

    [Fact]
    public async Task Summary_ByMonth_ReturnsRowsInAscendingOrder()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        var firstOfMonth = new DateOnly(Today.Year, Today.Month, 1);
        var earlierDate = firstOfMonth.AddMonths(-1).AddDays(5);

        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 10, date = earlierDate.ToString("yyyy-MM-dd"), categoryId = foodId });
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 20, date = Today.ToString("yyyy-MM-dd"), categoryId = foodId });

        var result = await SendAsync(HttpMethod.Get, $"/api/transactions/summary?from={earlierDate:yyyy-MM-dd}&to={Today:yyyy-MM-dd}&groupBy=month");
        Assert.Equal(200, result.Status);

        var months = result.Json.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("month").GetString()!).ToList();
        Assert.Equal(2, months.Count);
        Assert.True(string.CompareOrdinal(months[0], months[1]) < 0, $"Expected ascending months, got {string.Join(", ", months)}");
    }
}
