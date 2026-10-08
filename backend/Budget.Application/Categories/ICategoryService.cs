using Budget.Domain.Entities;

namespace Budget.Application.Categories;

/// <summary>Предоставляет операции просмотра и управления деревом категорий.</summary>
public interface ICategoryService
{
    /// <summary>Возвращает дерево категорий с заданными фильтрами.</summary>
    /// <param name="kind">Необязательное направление операций для фильтрации.</param>
    /// <param name="includeArchived">Включать ли архивированные категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Корневые категории с дочерними элементами.</returns>
    Task<IReadOnlyList<CategoryTreeDto>> ListAsync(TransactionKind? kind, bool includeArchived, CancellationToken ct);

    /// <summary>Создаёт новую группу или подкатегорию.</summary>
    /// <param name="request">Данные создаваемой категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной категории.</returns>
    Task<CategoryTreeDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct);

    /// <summary>Изменяет предоставленные поля категории.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="request">Изменяемые свойства категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление категории.</returns>
    Task<CategoryTreeDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);

    /// <summary>Удаляет категорию, если на неё не ссылаются дочерние данные.</summary>
    /// <param name="id">Идентификатор удаляемой категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteAsync(int id, CancellationToken ct);
}
