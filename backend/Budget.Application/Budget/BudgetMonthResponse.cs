namespace Budget.Application.Budget;

/// <summary>Бюджетный план и фактические расходы за выбранный месяц.</summary>
/// <param name="Month">Месяц в формате yyyy-MM.</param>
/// <param name="Items">Плановые позиции месяца.</param>
/// <param name="Categories">Сводка плана и факта по категориям расходов.</param>
/// <param name="Totals">Сводные суммы по всему месяцу.</param>
public sealed record BudgetMonthResponse(
    string Month,
    IReadOnlyList<BudgetItemDto> Items,
    IReadOnlyList<BudgetCategoryDto> Categories,
    BudgetTotalsDto Totals);
