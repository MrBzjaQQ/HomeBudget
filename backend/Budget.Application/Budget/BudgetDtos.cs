namespace Budget.Application.Budget;

public record CreateBudgetItemRequest
{
    public string? Month { get; set; }
    public int? CategoryId { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public record BudgetItemDto(
    long Id,
    string Month,
    int CategoryId,
    string CategoryName,
    string? CategoryParentName,
    string? Color,
    decimal Amount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BudgetCategoryDto(
    int CategoryId,
    string Name,
    string? ParentName,
    string? Color,
    decimal Planned,
    decimal Actual,
    decimal Diff);

public record BudgetTotalsDto(decimal Planned, decimal Actual, decimal Diff);

public record BudgetMonthResponse(
    string Month,
    IReadOnlyList<BudgetItemDto> Items,
    IReadOnlyList<BudgetCategoryDto> Categories,
    BudgetTotalsDto Totals);
