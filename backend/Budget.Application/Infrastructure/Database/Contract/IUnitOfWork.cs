using Budget.Domain.Entities;

namespace Budget.Application.Infrastructure.Database.Contract;

/// <summary>
/// Единая точка доступа к репозиториям сущностей и фиксации изменений.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Репозиторий учётных записей пользователей.</summary>
    IGenericRepository<User> Users { get; }

    /// <summary>Репозиторий категорий доходов и расходов.</summary>
    IGenericRepository<Category> Categories { get; }

    /// <summary>Репозиторий финансовых операций.</summary>
    IGenericRepository<Transaction> Transactions { get; }

    /// <summary>Репозиторий плановых позиций бюджета.</summary>
    IGenericRepository<BudgetItem> BudgetItems { get; }

    /// <summary>Репозиторий счетов накоплений.</summary>
    IGenericRepository<SavingsContainer> SavingsContainers { get; }

    /// <summary>Репозиторий ежемесячных записей накоплений.</summary>
    IGenericRepository<SavingsEntry> SavingsEntries { get; }

    /// <summary>Репозиторий финансовых целей.</summary>
    IGenericRepository<FinancialGoal> Goals { get; }

    /// <summary>Фиксирует все накопленные изменения в хранилище данных.</summary>
    /// <param name="token">Токен отмены операции сохранения.</param>
    /// <returns>Количество записей, затронутых сохранением.</returns>
    Task<int> SaveChangesAsync(CancellationToken token = default);
}
