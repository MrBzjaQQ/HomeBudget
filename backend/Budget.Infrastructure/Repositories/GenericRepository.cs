using Budget.Application.Infrastructure.Database.Contract;
using Budget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget.Infrastructure.Repositories;

/// <summary>
/// Предоставляет реализацию репозитория для работы с сущностями типа <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">Тип сущности, с которой работает репозиторий.</typeparam>
public sealed class GenericRepository<TEntity>(BudgetDbContext db) : IGenericRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = db.Set<TEntity>();

    public IQueryable<TEntity> Query => _dbSet;

    public async Task AddAsync(TEntity entity, CancellationToken token = default)
        => await _dbSet.AddAsync(entity, token);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void Remove(TEntity entity)
        => _dbSet.Remove(entity);
}
