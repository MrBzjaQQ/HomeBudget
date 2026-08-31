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
}
