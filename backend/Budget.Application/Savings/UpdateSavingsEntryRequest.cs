namespace Budget.Application.Savings;

/// <summary>Поля для изменения записи накоплений.</summary>
public sealed record UpdateSavingsEntryRequest
{
    /// <summary>Новый месяц в формате yyyy-MM.</summary>
    public string? Month { get; set; }

    /// <summary>Новый идентификатор участника.</summary>
    public int? UserId { get; set; }

    /// <summary>Новый идентификатор счёта.</summary>
    public int? ContainerId { get; set; }

    /// <summary>Новая положительная сумма накоплений.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Новый комментарий; null сохраняет существующее значение.</summary>
    public string? Note { get; set; }
}
