namespace Budget.Application.Export;

/// <summary>Строка финансовой операции в подготовленном наборе экспорта.</summary>
/// <param name="Date">Дата операции.</param>
/// <param name="IsIncome">Признак доходной операции.</param>
/// <param name="Amount">Положительная сумма операции.</param>
/// <param name="CategoryPath">Полный путь категории от группы до подкатегории.</param>
/// <param name="UserName">Отображаемое имя участника.</param>
/// <param name="Note">Комментарий к операции, если задан.</param>
public sealed record ExportTransactionRow(
    DateOnly Date,
    bool IsIncome,
    decimal Amount,
    string CategoryPath,
    string UserName,
    string? Note);
