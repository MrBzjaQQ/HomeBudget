using Budget.Domain.Entities;

namespace Budget.Application.Transactions;

public record TransactionDto(
    long Id,
    TransactionKind Kind,
    decimal Amount,
    DateOnly Date,
    int CategoryId,
    string CategoryName,
    string? CategoryParentName,
    string? Color,
    int UserId,
    string UserDisplayName,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TransactionListResponse(
    IReadOnlyList<TransactionDto> Items,
    int Total,
    int Page,
    int PageSize,
    TotalsDto Totals);

public record TotalsDto(decimal Income, decimal Expense, decimal Net);

public record CreateTransactionRequest
{
    public TransactionKind? Kind { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? Date { get; set; }
    public int? CategoryId { get; set; }
    public int? UserId { get; set; }
    public string? Note { get; set; }
}

public record SummaryCategoryDto(
    int CategoryId,
    string Name,
    string Kind,
    int? ParentId,
    string? ParentName,
    string? Color,
    decimal Income,
    decimal Expense);

public record SummaryMonthDto(string Month, decimal Income, decimal Expense);

public record SummaryResponse(string GroupBy, object Rows);
