namespace Budget.Application.Infrastructure.Database.Contract;

/// <summary>
/// Реализация паттерна Generic Repository.
/// Предоставляет IQueryable по сущности и примитивные операции изменения.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, с которой работает репозиторий.</typeparam>
public interface IGenericRepository<TEntity>
    where TEntity : class
{
    /// <summary>Запрос по сущности (трекинг по умолчанию, как у DbSet).</summary>
    IQueryable<TEntity> Query { get; }

    /// <summary>Добавляет новую сущность.</summary>
    Task AddAsync(TEntity entity, CancellationToken token = default);

    /// <summary>Отмечает сущность как изменённую.</summary>
    void Update(TEntity entity);

    /// <summary>Отмечает сущность как удалённую.</summary>
    void Remove(TEntity entity);
}
