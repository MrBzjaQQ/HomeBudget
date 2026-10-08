namespace Budget.Application.Budget;

/// <summary>Сопоставление плановой и фактической суммы расходов по категории.</summary>
/// <param name="CategoryId">Идентификатор категории расходов.</param>
/// <param name="Name">Название категории.</param>
/// <param name="ParentName">Название родительской группы, если есть.</param>
/// <param name="Color">Цвет категории, если задан.</param>
/// <param name="Planned">Сумма запланированных расходов.</param>
/// <param name="Actual">Сумма фактических расходов.</param>
/// <param name="Diff">Разница между планом и фактом.</param>
public sealed record BudgetCategoryDto(
    int CategoryId,
    string Name,
    string? ParentName,
    string? Color,
    decimal Planned,
    decimal Actual,
    decimal Diff);
