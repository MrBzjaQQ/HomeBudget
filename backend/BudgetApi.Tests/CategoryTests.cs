using Xunit;

namespace BudgetApi.Tests;

public class CategoryTests : ApiTestBase
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Create_Child_InheritsKindFromParent()
    {
        var group = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестГруппа", kind = "INCOME", color = "#123456" });
        Assert.Equal(201, group.Status);
        var groupId = group.Json.GetProperty("id").GetInt32();

        var child = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестПод", kind = "INCOME", parentId = groupId });
        Assert.Equal(201, child.Status);
        Assert.Equal("INCOME", child.Json.GetProperty("kind").GetString());
        Assert.Equal(groupId, child.Json.GetProperty("parentId").GetInt32());
    }

    [Fact]
    public async Task Create_Child_WithMismatchedKind_Returns400()
    {
        var group = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестГруппа2", kind = "INCOME" });
        var groupId = group.Json.GetProperty("id").GetInt32();

        var child = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестПод2", kind = "EXPENSE", parentId = groupId });
        Assert.Equal(400, child.Status);
        TestAsserts.HasError(child, "kind");
    }

    [Fact]
    public async Task Create_SubcategoryUnderSubcategory_Returns400()
    {
        var group = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестГруппа3", kind = "EXPENSE" });
        Assert.Equal(201, group.Status);
        var groupId = group.Json.GetProperty("id").GetInt32();

        var child = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестПод3", kind = "EXPENSE", parentId = groupId });
        Assert.Equal(201, child.Status);
        var childId = child.Json.GetProperty("id").GetInt32();

        var grandchild = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ТестВнук", parentId = childId });
        Assert.Equal(400, grandchild.Status);
        Assert.Equal("ValidationFailed", grandchild.GetProperty<string>("code"));
        TestAsserts.HasError(grandchild, "parentId");

        var deletedChild = await SendAsync(HttpMethod.Delete, $"/api/categories/{childId}");
        Assert.Equal(204, deletedChild.Status);
        var deletedGroup = await SendAsync(HttpMethod.Delete, $"/api/categories/{groupId}");
        Assert.Equal(204, deletedGroup.Status);
    }

    [Fact]
    public async Task Create_DuplicateNameInScope_Returns409()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "Зарплата", kind = "INCOME" });

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Delete_CategoryWithTransactions_Returns409()
    {
        var foodId = await GetCategoryIdAsync("Продукты");
        string dateStr = Today.ToString("yyyy-MM-dd");
        await SendAsync(HttpMethod.Post, "/api/transactions", new { kind = "EXPENSE", amount = 10, date = dateStr, categoryId = foodId });

        var result = await SendAsync(HttpMethod.Delete, $"/api/categories/{foodId}");

        Assert.Equal(409, result.Status);
        Assert.Equal("Conflict", result.GetProperty<string>("code"));
    }

    [Fact]
    public async Task Archived_Category_HiddenByDefault_VisibleWithFlag()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "Архивная", kind = "EXPENSE" });
        var id = created.Json.GetProperty("id").GetInt32();

        var archived = await SendAsync(HttpMethod.Put, $"/api/categories/{id}", new { isArchived = true });
        Assert.Equal(200, archived.Status);

        var list = await SendAsync(HttpMethod.Get, "/api/categories");
        Assert.DoesNotContain(list.Json.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);

        var listWithArchived = await SendAsync(HttpMethod.Get, "/api/categories?includeArchived=true");
        Assert.Contains(listWithArchived.Json.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Update_RenamesCategory()
    {
        var created = await SendAsync(HttpMethod.Post, "/api/categories", new { name = "ПервоеИмя", kind = "EXPENSE" });
        var id = created.Json.GetProperty("id").GetInt32();

        var updated = await SendAsync(HttpMethod.Put, $"/api/categories/{id}", new { name = "ВтороеИмя" });

        Assert.Equal(200, updated.Status);
        Assert.Equal("ВтороеИмя", updated.Json.GetProperty("name").GetString());
    }
}
