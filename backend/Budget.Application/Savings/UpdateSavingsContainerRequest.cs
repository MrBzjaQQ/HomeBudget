namespace Budget.Application.Savings;

/// <summary>Необязательные изменения счёта накоплений.</summary>
public sealed record UpdateSavingsContainerRequest
{
    /// <summary>Новое название счёта; null оставляет прежнее.</summary>
    public string? Name { get; set; }

    /// <summary>Новый цвет; пустая строка очищает цвет.</summary>
    public string? Color { get; set; }

    /// <summary>Новый порядок отображения.</summary>
    public int? SortOrder { get; set; }
}
