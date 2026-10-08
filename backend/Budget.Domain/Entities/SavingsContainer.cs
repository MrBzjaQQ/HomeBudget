namespace Budget.Domain.Entities;

/// <summary>Общий счёт, на котором семья учитывает накопления.</summary>
public class SavingsContainer
{
    /// <summary>Уникальный идентификатор счёта накоплений.</summary>
    public int Id { get; set; }

    /// <summary>Уникальное название счёта.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Необязательный цвет счёта для отображения.</summary>
    public string? Color { get; set; }

    /// <summary>Порядок отображения счёта.</summary>
    public int SortOrder { get; set; }

    /// <summary>Дата и время создания счёта.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Ежемесячные записи накоплений по этому счёту.</summary>
    public ICollection<SavingsEntry> Entries { get; set; } = new List<SavingsEntry>();

    /// <summary>Финансовые цели, связанные со счётом.</summary>
    public ICollection<FinancialGoal> Goals { get; set; } = new List<FinancialGoal>();
}
