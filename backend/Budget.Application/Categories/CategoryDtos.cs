using Budget.Domain.Entities;

namespace Budget.Application.Categories;

public record CategoryTreeDto(
    int Id,
    string Name,
    string Kind,
    int? ParentId,
    string? Color,
    int SortOrder,
    bool IsArchived,
    IReadOnlyList<CategoryTreeDto>? Children);

public record CreateCategoryRequest
{
    public string? Name { get; set; }
    public TransactionKind? Kind { get; set; }
    public int? ParentId { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}

public record UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsArchived { get; set; }
}
