namespace Budget.Application.Budget;

public interface IBudgetService
{
    Task<BudgetMonthResponse> GetMonthAsync(string? month, CancellationToken ct);

    Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct);

    Task<BudgetItemDto> UpdateAsync(long id, CreateBudgetItemRequest request, CancellationToken ct);

    Task DeleteAsync(long id, CancellationToken ct);
}
