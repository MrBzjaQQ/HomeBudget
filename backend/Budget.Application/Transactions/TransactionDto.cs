using Budget.Domain.Entities;

namespace Budget.Application.Transactions;

/// <summary>Операция для отображения в журнале и ответах API.</summary>
/// <param name="Id">Идентификатор операции.</param>
/// <param name="Kind">Направление операции.</param>
/// <param name="Amount">Положительная сумма операции.</param>
/// <param name="Date">Дата совершения операции.</param>
/// <param name="CategoryId">Идентификатор категории.</param>
/// <param name="CategoryName">Название категории.</param>
/// <param name="CategoryParentName">Название группы, если категория вложенная.</param>
/// <param name="Color">Цвет категории, если он задан.</param>
/// <param name="UserId">Идентификатор владельца операции.</param>
/// <param name="UserDisplayName">Отображаемое имя владельца.</param>
/// <param name="Note">Комментарий к операции, если он задан.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения записи.</param>
public sealed record TransactionDto(
    long Id,
    TransactionKind Kind,
    decimal Amount,
    DateOnly Date,
    int CategoryId,
    string CategoryName,
    string? CategoryParentName,
    string? Color,
    int UserId,
    string UserDisplayName,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);
