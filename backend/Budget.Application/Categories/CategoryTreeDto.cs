namespace Budget.Application.Categories;

/// <summary>Категория с непосредственным списком дочерних категорий.</summary>
/// <param name="Id">Идентификатор категории.</param>
/// <param name="Name">Название категории.</param>
/// <param name="Kind">Направление операций в верхнем регистре.</param>
/// <param name="ParentId">Идентификатор родителя либо null для группы.</param>
/// <param name="Color">Цвет категории, если задан.</param>
/// <param name="SortOrder">Порядок отображения внутри уровня.</param>
/// <param name="IsArchived">Признак архивации.</param>
/// <param name="Children">Дочерние категории; null, когда они не включены.</param>
public sealed record CategoryTreeDto(
    int Id,
    string Name,
    string Kind,
    int? ParentId,
    string? Color,
    int SortOrder,
    bool IsArchived,
    IReadOnlyList<CategoryTreeDto>? Children);
