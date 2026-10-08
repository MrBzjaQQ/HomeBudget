namespace Budget.Application.Savings;

/// <summary>Сводка семейных накоплений за последовательное окно месяцев.</summary>
/// <param name="To">Последний месяц сводки в формате yyyy-MM.</param>
/// <param name="Months">Сумма накоплений отдельно за каждый месяц.</param>
/// <param name="Deltas">Изменение суммы относительно предыдущего месяца.</param>
/// <param name="ByMember">Совокупные суммы накоплений по участникам.</param>
/// <param name="Total">Общая сумма накоплений участников за окно сводки.</param>
public sealed record SavingsSummary(
    string To,
    IReadOnlyList<SavingsMonthAmount> Months,
    IReadOnlyList<SavingsMonthDelta> Deltas,
    IReadOnlyList<SavingsMemberTotal> ByMember,
    decimal Total);
