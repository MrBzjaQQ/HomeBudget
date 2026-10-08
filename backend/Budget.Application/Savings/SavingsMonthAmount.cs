namespace Budget.Application.Savings;

/// <summary>Сумма учтённых накоплений за календарный месяц.</summary>
/// <param name="Month">Месяц в формате yyyy-MM.</param>
/// <param name="Amount">Сумма записей накоплений за месяц.</param>
public sealed record SavingsMonthAmount(string Month, decimal Amount);
