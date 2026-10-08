namespace Budget.Application.Savings;

/// <summary>Изменение суммы накоплений между последовательными месяцами.</summary>
/// <param name="Month">Месяц, к которому относится изменение, в формате yyyy-MM.</param>
/// <param name="Delta">Разница с суммой предыдущего месяца.</param>
public sealed record SavingsMonthDelta(string Month, decimal Delta);
