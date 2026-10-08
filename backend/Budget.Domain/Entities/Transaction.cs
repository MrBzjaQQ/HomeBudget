namespace Budget.Domain.Entities;

/// <summary>Доходная или расходная финансовая операция пользователя.</summary>
public class Transaction
{
    /// <summary>Уникальный идентификатор операции.</summary>
    public long Id { get; set; }

    /// <summary>Идентификатор пользователя, которому принадлежит операция.</summary>
    public int UserId { get; set; }

    /// <summary>Пользователь, которому принадлежит операция.</summary>
    public User User { get; set; } = null!;

    /// <summary>Идентификатор категории операции.</summary>
    public int CategoryId { get; set; }

    /// <summary>Категория, к которой отнесена операция.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Направление операции: доход или расход.</summary>
    public TransactionKind Kind { get; set; }

    /// <summary>Положительная сумма операции; знак определяется её <see cref="Kind"/>.</summary>
    public decimal Amount { get; set; }

    /// <summary>Календарная дата совершения операции.</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Необязательный комментарий к операции.</summary>
    public string? Note { get; set; }

    /// <summary>Дата и время создания операции.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата и время последнего изменения операции.</summary>
    public DateTime UpdatedAt { get; set; }
}
