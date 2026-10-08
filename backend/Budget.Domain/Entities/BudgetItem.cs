namespace Budget.Domain.Entities;

/// <summary>Плановая сумма расходов по категории за календарный месяц.</summary>
public class BudgetItem
{
    /// <summary>Уникальный идентификатор позиции бюджета.</summary>
    public long Id { get; set; }

    /// <summary>Месяц планирования, представленный первым числом месяца.</summary>
    public DateOnly Month { get; set; }

    /// <summary>Идентификатор категории расходов.</summary>
    public int CategoryId { get; set; }

    /// <summary>Категория, для которой задан план.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Положительная запланированная сумма.</summary>
    public decimal Amount { get; set; }

    /// <summary>Необязательное пояснение к плановой позиции.</summary>
    public string? Note { get; set; }

    /// <summary>Дата и время создания позиции бюджета.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата и время последнего изменения позиции бюджета.</summary>
    public DateTime UpdatedAt { get; set; }
}
