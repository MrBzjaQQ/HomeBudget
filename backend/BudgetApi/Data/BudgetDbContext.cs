using BudgetApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Data;

public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            entity.HasIndex(x => x.Kind).HasDatabaseName("idx_cat_kind");
            entity.Property(x => x.Color).HasMaxLength(7);
            entity.HasIndex(x => new { x.Name, x.Kind, x.ParentId }).IsUnique();
            entity.HasMany(x => x.Children)
                .WithOne(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
            entity.Property(x => x.IsArchived).HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            entity.Property(x => x.Amount).HasPrecision(15, 2);
            entity.ToTable(t => t.HasCheckConstraint("chk_txn_amount_positive", "amount > 0"));
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => x.TransactionDate).HasDatabaseName("idx_txn_date");
            entity.HasIndex(x => new { x.UserId, x.TransactionDate }).HasDatabaseName("idx_txn_user_date");
            entity.HasIndex(x => x.CategoryId).HasDatabaseName("idx_txn_category");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdatedTransactions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        TouchUpdatedTransactions();
        return base.SaveChanges();
    }

    private void TouchUpdatedTransactions()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Transaction>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
