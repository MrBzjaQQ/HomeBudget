namespace Budget.Application.Goals;

/// <summary>Финансовая цель с актуальным прогрессом и рекомендуемым темпом накоплений.</summary>
/// <param name="Id">Идентификатор цели.</param>
/// <param name="Name">Название цели.</param>
/// <param name="TargetAmount">Целевая сумма.</param>
/// <param name="ContainerId">Идентификатор связанного счёта.</param>
/// <param name="ContainerName">Название связанного счёта.</param>
/// <param name="ContainerColor">Цвет счёта, если он задан.</param>
/// <param name="DueDate">Срок достижения в формате yyyy-MM-dd, если задан.</param>
/// <param name="Saved">Сумма накоплений, отнесённая к цели.</param>
/// <param name="MonthlyRequired">Рекомендуемая сумма накоплений в месяц либо null, если расчёт не применим.</param>
/// <param name="Achieved">Признак достижения целевой суммы.</param>
/// <param name="CreatedAt">Дата и время создания цели.</param>
public sealed record GoalDto(
    int Id,
    string Name,
    decimal TargetAmount,
    int ContainerId,
    string ContainerName,
    string? ContainerColor,
    string? DueDate,
    decimal Saved,
    decimal? MonthlyRequired,
    bool Achieved,
    DateTime CreatedAt);
