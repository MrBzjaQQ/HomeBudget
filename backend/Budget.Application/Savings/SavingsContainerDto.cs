namespace Budget.Application.Savings;

/// <summary>Публичные данные счёта семейных накоплений.</summary>
/// <param name="Id">Идентификатор счёта.</param>
/// <param name="Name">Название счёта.</param>
/// <param name="Color">Цвет счёта, если задан.</param>
/// <param name="SortOrder">Порядок отображения счёта.</param>
/// <param name="CreatedAt">Дата и время создания счёта.</param>
public sealed record SavingsContainerDto(
    int Id,
    string Name,
    string? Color,
    int SortOrder,
    DateTime CreatedAt);
