using Budget.Domain.Entities;

namespace Budget.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionListResponse> ListAsync(
        DateOnly? from,
        DateOnly? to,
        int? userId,
        List<int>? categoryIds,
        TransactionKind? kind,
        string? q,
        int page,
        int pageSize,
        string? sort,
        string? dir,
        CancellationToken ct);

    Task<TransactionDto> GetAsync(long id, CancellationToken ct);

    Task<SummaryResponse> SummaryAsync(DateOnly? from, DateOnly? to, string? groupBy, CancellationToken ct);

    Task<TransactionDto> CreateAsync(CreateTransactionRequest request, int currentUserId, CancellationToken ct);

    Task<TransactionDto> UpdateAsync(long id, CreateTransactionRequest request, int currentUserId, CancellationToken ct);

    Task DeleteAsync(long id, CancellationToken ct);
}
