using Budget.Application.Infrastructure.Database.Contract;
using Budget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget.Infrastructure.Repositories;

/// <summary>
/// Реализует типовые операции над сущностями типа <typeparamref name="TEntity"/> через EF Core.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, с которой работает репозиторий.</typeparam>
public sealed class GenericRepository<TEntity>(BudgetDbContext db) : IGenericRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = db.Set<TEntity>();

    /// <summary>Возвращает запрос к сущностям без его материализации.</summary>
    public IQueryable<TEntity> Query => _dbSet;

    /// <summary>Добавляет сущность в контекст для последующего сохранения.</summary>
    /// <param name="entity">Новая сущность для добавления.</param>
    /// <param name="token">Токен отмены асинхронной операции.</param>
    /// <returns>Асинхронная операция добавления в контекст.</returns>
    public async Task AddAsync(TEntity entity, CancellationToken token = default)
        => await _dbSet.AddAsync(entity, token);

    /// <summary>Помечает сущность как изменённую.</summary>
    /// <param name="entity">Сущность, изменения которой требуется сохранить.</param>
    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    /// <summary>Помечает сущность для удаления при следующем сохранении.</summary>
    /// <param name="entity">Сущность, которую требуется удалить.</param>
    public void Remove(TEntity entity)
        => _dbSet.Remove(entity);
}
