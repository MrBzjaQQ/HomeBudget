namespace Budget.Application.Transactions;

/// <summary>Итоговые суммы доходов и расходов с чистым балансом.</summary>
/// <param name="Income">Сумма доходных операций.</param>
/// <param name="Expense">Сумма расходных операций.</param>
/// <param name="Net">Разница между доходами и расходами.</param>
public sealed record TotalsDto(decimal Income, decimal Expense, decimal Net);
