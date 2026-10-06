using Budget.Domain.Entities;

namespace Budget.Application.Infrastructure.Database.Contract;

/// <summary>
/// Реализация паттерна Unit of Work.
/// Единая точка доступа к репозиториям сущностей и сохранению изменений.
/// </summary>
public interface IUnitOfWork
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Category> Categories { get; }

    IGenericRepository<Transaction> Transactions { get; }

    IGenericRepository<BudgetItem> BudgetItems { get; }

    IGenericRepository<SavingsContainer> SavingsContainers { get; }

    IGenericRepository<SavingsEntry> SavingsEntries { get; }

    IGenericRepository<FinancialGoal> Goals { get; }

    /// <summary>Сохраняет все накопленные изменения в базе данных.</summary>
    Task<int> SaveChangesAsync(CancellationToken token = default);
}
