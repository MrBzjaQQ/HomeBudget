using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Infrastructure.Data;

/// <summary>Контекст EF Core для хранения данных приложения «Домашний бюджет».</summary>
/// <param name="options">Настройки контекста, включая выбранный провайдер базы данных.</param>
public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    /// <summary>Пользователи, зарегистрированные в бюджете.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Категории доходов и расходов, включая иерархию подкатегорий.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Операции дохода и расхода.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>Плановые статьи бюджета.</summary>
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();

    /// <summary>Счета и контейнеры накоплений.</summary>
    public DbSet<SavingsContainer> SavingsContainers => Set<SavingsContainer>();

    /// <summary>Пополнения и снятия средств со счетов накоплений.</summary>
    public DbSet<SavingsEntry> SavingsEntries => Set<SavingsEntry>();

    /// <summary>Финансовые цели, связанные со счетами накоплений.</summary>
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();

    /// <summary>Применяет конфигурации сущностей из текущей сборки к модели контекста.</summary>
    /// <param name="modelBuilder">Построитель модели данных EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
    }

    /// <summary>Сохраняет изменения и обновляет время изменения поддерживаемых сущностей.</summary>
    /// <param name="cancellationToken">Токен отмены операции сохранения.</param>
    /// <returns>Количество записей, затронутых сохранением.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdated();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Синхронно сохраняет изменения и обновляет время изменения поддерживаемых сущностей.</summary>
    /// <returns>Количество записей, затронутых сохранением.</returns>
    public override int SaveChanges()
    {
        TouchUpdated();
        return base.SaveChanges();
    }

    private void TouchUpdated()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Transaction>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BudgetItem>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SavingsEntry>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
