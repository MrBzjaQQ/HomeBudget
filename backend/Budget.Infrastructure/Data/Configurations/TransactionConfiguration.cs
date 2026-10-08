using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает поля, ограничения и связи финансовой операции.</summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <summary>Определяет схему хранения операции в модели EF Core.</summary>
    /// <param name="builder">Построитель отображения операции.</param>
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Amount).HasPrecision(15, 2);
        builder.ToTable(t => t.HasCheckConstraint("chk_txn_amount_positive", "amount > 0"));
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.TransactionDate).HasDatabaseName("idx_txn_date");
        builder.HasIndex(x => new { x.UserId, x.TransactionDate }).HasDatabaseName("idx_txn_user_date");
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("idx_txn_category");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
