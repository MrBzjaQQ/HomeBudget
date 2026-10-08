namespace Budget.Application.Transactions;

/// <summary>Сводные данные операций с указанием применённой группировки.</summary>
/// <param name="GroupBy">Ключ группировки: category либо month.</param>
/// <param name="Rows">Список строк соответствующего типа сводки.</param>
public sealed record SummaryResponse(string GroupBy, object Rows);
