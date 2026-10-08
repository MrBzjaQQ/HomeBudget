using Budget.Domain.Entities;

namespace Budget.Application.Transactions;

/// <summary>Поля новой операции или полная замена существующей операции.</summary>
public sealed record CreateTransactionRequest
{
    /// <summary>Направление операции: доход или расход.</summary>
    public TransactionKind? Kind { get; set; }

    /// <summary>Положительная денежная сумма.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Календарная дата операции.</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Идентификатор категории операции.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Идентификатор участника, которому принадлежит операция.</summary>
    public int? UserId { get; set; }

    /// <summary>Необязательный комментарий к операции.</summary>
    public string? Note { get; set; }
}
