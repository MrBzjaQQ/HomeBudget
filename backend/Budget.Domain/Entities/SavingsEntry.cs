namespace Budget.Domain.Entities;

/// <summary>Запись о сумме накоплений участника на счёте за определённый месяц.</summary>
public class SavingsEntry
{
    /// <summary>Уникальный идентификатор записи накоплений.</summary>
    public long Id { get; set; }

    /// <summary>Месяц учёта, представленный первым числом месяца.</summary>
    public DateOnly Month { get; set; }

    /// <summary>Идентификатор счёта накоплений.</summary>
    public int ContainerId { get; set; }

    /// <summary>Счёт, к которому относится запись.</summary>
    public SavingsContainer Container { get; set; } = null!;

    /// <summary>Идентификатор участника семьи.</summary>
    public int UserId { get; set; }

    /// <summary>Участник семьи, которому принадлежит запись.</summary>
    public User User { get; set; } = null!;

    /// <summary>Положительная сумма накоплений за месяц.</summary>
    public decimal Amount { get; set; }

    /// <summary>Необязательное пояснение к записи.</summary>
    public string? Note { get; set; }

    /// <summary>Дата и время создания записи.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата и время последнего изменения записи.</summary>
    public DateTime UpdatedAt { get; set; }
}
