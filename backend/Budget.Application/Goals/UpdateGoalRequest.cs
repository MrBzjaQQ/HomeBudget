namespace Budget.Application.Goals;

/// <summary>Значения финансовой цели для полного обновления.</summary>
public sealed record UpdateGoalRequest
{
    /// <summary>Новое название цели.</summary>
    public string? Name { get; set; }

    /// <summary>Новая положительная целевая сумма.</summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>Идентификатор счёта, связанного с целью.</summary>
    public int? ContainerId { get; set; }

    /// <summary>Новый необязательный срок достижения в формате yyyy-MM-dd.</summary>
    public string? DueDate { get; set; }
}
