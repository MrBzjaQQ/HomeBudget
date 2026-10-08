namespace Budget.Application.Categories;

/// <summary>Набор необязательных изменений категории.</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Новое название; null означает не изменять название.</summary>
    public string? Name { get; set; }

    /// <summary>Новый цвет в формате #RRGGBB; пустая строка очищает цвет.</summary>
    public string? Color { get; set; }

    /// <summary>Новый порядок отображения.</summary>
    public int? SortOrder { get; set; }

    /// <summary>Новое состояние архивации категории.</summary>
    public bool? IsArchived { get; set; }
}
