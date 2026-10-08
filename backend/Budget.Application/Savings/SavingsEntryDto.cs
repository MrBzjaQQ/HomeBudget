namespace Budget.Application.Savings;

/// <summary>Публичное представление ежемесячной записи накоплений.</summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="Month">Месяц записи в формате yyyy-MM.</param>
/// <param name="UserId">Идентификатор участника.</param>
/// <param name="UserName">Отображаемое имя участника.</param>
/// <param name="ContainerId">Идентификатор счёта.</param>
/// <param name="ContainerName">Название счёта.</param>
/// <param name="Color">Цвет счёта, если задан.</param>
/// <param name="Amount">Сумма накоплений за месяц.</param>
/// <param name="Note">Комментарий к записи.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения записи.</param>
public sealed record SavingsEntryDto(
    long Id,
    string Month,
    int UserId,
    string UserName,
    int ContainerId,
    string ContainerName,
    string? Color,
    decimal Amount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);
