namespace Budget.Application.Transactions;

/// <summary>Итоги доходов и расходов за один календарный месяц.</summary>
/// <param name="Month">Месяц в формате yyyy-MM.</param>
/// <param name="Income">Сумма доходов за месяц.</param>
/// <param name="Expense">Сумма расходов за месяц.</param>
public sealed record SummaryMonthDto(string Month, decimal Income, decimal Expense);
