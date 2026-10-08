namespace Budget.Application.Budget;

/// <summary>Поля плановой позиции бюджета для заданного месяца.</summary>
public sealed record CreateBudgetItemRequest
{
    /// <summary>Месяц плана в формате yyyy-MM.</summary>
    public string? Month { get; set; }

    /// <summary>Идентификатор категории расходов.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Положительная плановая сумма.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Необязательный комментарий к плану.</summary>
    public string? Note { get; set; }
}
