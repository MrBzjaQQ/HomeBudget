using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Budget.Infrastructure.Data;

namespace Budget.Infrastructure.Repositories;

/// <summary>Реализация паттерна Unit of Work для базы данных.</summary>
public sealed class UnitOfWork(BudgetDbContext db) : IUnitOfWork
{
    public IGenericRepository<User> Users { get; } = new GenericRepository<User>(db);

    public IGenericRepository<Category> Categories { get; } = new GenericRepository<Category>(db);

    public IGenericRepository<Transaction> Transactions { get; } = new GenericRepository<Transaction>(db);

    public IGenericRepository<BudgetItem> BudgetItems { get; } = new GenericRepository<BudgetItem>(db);

    public IGenericRepository<SavingsContainer> SavingsContainers { get; } = new GenericRepository<SavingsContainer>(db);

    public IGenericRepository<SavingsEntry> SavingsEntries { get; } = new GenericRepository<SavingsEntry>(db);

    public Task<int> SaveChangesAsync(CancellationToken token = default)
        => db.SaveChangesAsync(token);
}
