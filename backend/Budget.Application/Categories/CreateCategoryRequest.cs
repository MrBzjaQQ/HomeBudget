using Budget.Domain.Entities;

namespace Budget.Application.Categories;

/// <summary>Данные для добавления группы категорий или подкатегории.</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Название создаваемой категории.</summary>
    public string? Name { get; set; }

    /// <summary>Направление операций для группы; подкатегория наследует его от родителя.</summary>
    public TransactionKind? Kind { get; set; }

    /// <summary>Идентификатор родительской группы для создания подкатегории.</summary>
    public int? ParentId { get; set; }

    /// <summary>Необязательный цвет категории в формате #RRGGBB.</summary>
    public string? Color { get; set; }

    /// <summary>Порядок отображения; при отсутствии используется значение по умолчанию.</summary>
    public int? SortOrder { get; set; }
}
