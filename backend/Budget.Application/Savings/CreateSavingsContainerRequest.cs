namespace Budget.Application.Savings;

/// <summary>Поля нового общего счёта накоплений.</summary>
public sealed record CreateSavingsContainerRequest
{
    /// <summary>Уникальное отображаемое название счёта.</summary>
    public string? Name { get; set; }

    /// <summary>Необязательный цвет счёта в формате #RRGGBB.</summary>
    public string? Color { get; set; }

    /// <summary>Порядок отображения; если не указан, добавляется после существующих счетов.</summary>
    public int? SortOrder { get; set; }
}
