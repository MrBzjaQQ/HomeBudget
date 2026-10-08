namespace Budget.Application.Savings;

/// <summary>Записи накоплений и общий итог за один календарный месяц.</summary>
/// <param name="Month">Месяц в формате yyyy-MM.</param>
/// <param name="Entries">Записи участников по счетам за выбранный месяц.</param>
/// <param name="Total">Сумма всех записей месяца.</param>
public sealed record SavingsMonthResponse(
    string Month,
    IReadOnlyList<SavingsEntryDto> Entries,
    decimal Total);
