namespace Budget.Application.Savings;

/// <summary>Данные для учёта накоплений участника за месяц.</summary>
public sealed record CreateSavingsEntryRequest
{
    /// <summary>Месяц учёта в формате yyyy-MM.</summary>
    public string? Month { get; set; }

    /// <summary>Идентификатор участника; при отсутствии используется текущий пользователь.</summary>
    public int? UserId { get; set; }

    /// <summary>Идентификатор счёта накоплений.</summary>
    public int? ContainerId { get; set; }

    /// <summary>Положительная сумма накоплений.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Необязательный комментарий к записи.</summary>
    public string? Note { get; set; }
}
