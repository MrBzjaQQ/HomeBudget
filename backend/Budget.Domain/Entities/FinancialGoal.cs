namespace Budget.Domain.Entities;

/// <summary>Цель накоплений с целевой суммой и необязательным сроком достижения.</summary>
public class FinancialGoal
{
    /// <summary>Уникальный идентификатор финансовой цели.</summary>
    public int Id { get; set; }

    /// <summary>Название цели.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Сумма, необходимая для достижения цели.</summary>
    public decimal TargetAmount { get; set; }

    /// <summary>Идентификатор счёта накоплений, к которому привязана цель.</summary>
    public int ContainerId { get; set; }

    /// <summary>Счёт накоплений, используемый для цели.</summary>
    public SavingsContainer Container { get; set; } = null!;

    /// <summary>Планируемая дата достижения цели, если она задана.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Дата и время создания цели.</summary>
    public DateTime CreatedAt { get; set; }
}
