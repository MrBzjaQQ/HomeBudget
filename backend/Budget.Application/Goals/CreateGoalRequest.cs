namespace Budget.Application.Goals;

/// <summary>Поля новой финансовой цели накоплений.</summary>
public sealed record CreateGoalRequest
{
    /// <summary>Название цели.</summary>
    public string? Name { get; set; }

    /// <summary>Положительная сумма, необходимая для достижения цели.</summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>Идентификатор счёта, на котором учитывается цель.</summary>
    public int? ContainerId { get; set; }

    /// <summary>Необязательная дата достижения в формате yyyy-MM-dd.</summary>
    public string? DueDate { get; set; }
}
