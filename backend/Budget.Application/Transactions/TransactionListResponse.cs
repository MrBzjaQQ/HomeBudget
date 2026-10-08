namespace Budget.Application.Transactions;

/// <summary>Страница операций, метаданные пагинации и агрегированные суммы.</summary>
/// <param name="Items">Операции текущей страницы.</param>
/// <param name="Total">Общее число операций, соответствующих фильтрам.</param>
/// <param name="Page">Номер текущей страницы.</param>
/// <param name="PageSize">Максимальное число элементов страницы.</param>
/// <param name="Totals">Итоги доходов, расходов и их разницы.</param>
public sealed record TransactionListResponse(
    IReadOnlyList<TransactionDto> Items,
    int Total,
    int Page,
    int PageSize,
    TotalsDto Totals);
