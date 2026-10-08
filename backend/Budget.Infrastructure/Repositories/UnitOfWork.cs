using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Budget.Infrastructure.Data;

namespace Budget.Infrastructure.Repositories;

/// <summary>Объединяет репозитории и сохраняет их изменения одной операцией EF Core.</summary>
/// <param name="db">Контекст базы данных, общий для всех репозиториев.</param>
public sealed class UnitOfWork(BudgetDbContext db) : IUnitOfWork
{
    /// <summary>Репозиторий пользователей.</summary>
    public IGenericRepository<User> Users { get; } = new GenericRepository<User>(db);

    /// <summary>Репозиторий категорий.</summary>
    public IGenericRepository<Category> Categories { get; } = new GenericRepository<Category>(db);

    /// <summary>Репозиторий операций бюджета.</summary>
    public IGenericRepository<Transaction> Transactions { get; } = new GenericRepository<Transaction>(db);

    /// <summary>Репозиторий плановых статей бюджета.</summary>
    public IGenericRepository<BudgetItem> BudgetItems { get; } = new GenericRepository<BudgetItem>(db);

    /// <summary>Репозиторий счетов накоплений.</summary>
    public IGenericRepository<SavingsContainer> SavingsContainers { get; } = new GenericRepository<SavingsContainer>(db);

    /// <summary>Репозиторий движений по счетам накоплений.</summary>
    public IGenericRepository<SavingsEntry> SavingsEntries { get; } = new GenericRepository<SavingsEntry>(db);

    /// <summary>Репозиторий финансовых целей.</summary>
    public IGenericRepository<FinancialGoal> Goals { get; } = new GenericRepository<FinancialGoal>(db);

    /// <summary>Сохраняет все отслеживаемые изменения во всех репозиториях.</summary>
    /// <param name="token">Токен отмены операции сохранения.</param>
    /// <returns>Количество записей, затронутых сохранением.</returns>
    public Task<int> SaveChangesAsync(CancellationToken token = default)
        => db.SaveChangesAsync(token);
}
