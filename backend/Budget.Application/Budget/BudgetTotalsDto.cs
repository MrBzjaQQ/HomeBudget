namespace Budget.Application.Budget;

/// <summary>Итоги плановых и фактических расходов за месяц.</summary>
/// <param name="Planned">Общая запланированная сумма.</param>
/// <param name="Actual">Общая сумма фактических расходов.</param>
/// <param name="Diff">Разница между плановой и фактической суммой.</param>
public sealed record BudgetTotalsDto(decimal Planned, decimal Actual, decimal Diff);
