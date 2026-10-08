namespace Budget.Application.Transactions;

/// <summary>Агрегированные операции конкретной категории.</summary>
/// <param name="CategoryId">Идентификатор категории.</param>
/// <param name="Name">Название категории.</param>
/// <param name="Kind">Направление категории в верхнем регистре.</param>
/// <param name="ParentId">Идентификатор родительской группы, если есть.</param>
/// <param name="ParentName">Название родительской группы, если есть.</param>
/// <param name="Color">Цвет категории, если задан.</param>
/// <param name="Income">Суммарные доходы по категории.</param>
/// <param name="Expense">Суммарные расходы по категории.</param>
public sealed record SummaryCategoryDto(
    int CategoryId,
    string Name,
    string Kind,
    int? ParentId,
    string? ParentName,
    string? Color,
    decimal Income,
    decimal Expense);
