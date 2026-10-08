using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает хранение плановой статьи бюджета и её связей.</summary>
public sealed class BudgetItemConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    /// <summary>Определяет ограничения, точность чисел, индексы и связи статьи бюджета.</summary>
    /// <param name="builder">Построитель отображения статьи бюджета.</param>
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(15, 2);
        builder.ToTable(t => t.HasCheckConstraint("chk_budget_amount_positive", "amount > 0"));
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.Month, x.CategoryId }).HasDatabaseName("idx_budget_month_category");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasOne(x => x.Category)
            .WithMany(x => x.BudgetItems)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
