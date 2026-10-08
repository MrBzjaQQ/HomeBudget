namespace Budget.Application.Budget;

/// <summary>Плановая позиция бюджета с информацией о категории и времени изменения.</summary>
/// <param name="Id">Идентификатор позиции.</param>
/// <param name="Month">Месяц планирования в формате yyyy-MM.</param>
/// <param name="CategoryId">Идентификатор категории.</param>
/// <param name="CategoryName">Название категории.</param>
/// <param name="CategoryParentName">Название родительской группы при наличии.</param>
/// <param name="Color">Цвет категории при наличии.</param>
/// <param name="Amount">Запланированная сумма.</param>
/// <param name="Note">Комментарий к плану.</param>
/// <param name="CreatedAt">Дата и время создания позиции.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения.</param>
public sealed record BudgetItemDto(
    long Id,
    string Month,
    int CategoryId,
    string CategoryName,
    string? CategoryParentName,
    string? Color,
    decimal Amount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);
