namespace Budget.Application.Infrastructure.Database.Contract;

/// <summary>
/// Реализация паттерна Generic Repository.
/// Предоставляет IQueryable по сущности и примитивные операции изменения.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, с которой работает репозиторий.</typeparam>
public interface IGenericRepository<TEntity>
    where TEntity : class
{
    /// <summary>Запрос сущностей с отслеживанием изменений провайдером по умолчанию.</summary>
    IQueryable<TEntity> Query { get; }

    /// <summary>Добавляет новую сущность для последующего сохранения.</summary>
    /// <param name="entity">Сущность, которую требуется добавить.</param>
    /// <param name="token">Токен отмены операции добавления.</param>
    Task AddAsync(TEntity entity, CancellationToken token = default);

    /// <summary>Отмечает сущность для обновления при следующем сохранении.</summary>
    /// <param name="entity">Сущность с новыми значениями.</param>
    void Update(TEntity entity);

    /// <summary>Отмечает сущность для удаления при следующем сохранении.</summary>
    /// <param name="entity">Сущность, которую требуется удалить.</param>
    void Remove(TEntity entity);
}
