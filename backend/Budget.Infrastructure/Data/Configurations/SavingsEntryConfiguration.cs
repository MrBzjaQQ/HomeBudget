using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает хранение записей о движении средств накоплений.</summary>
public sealed class SavingsEntryConfiguration : IEntityTypeConfiguration<SavingsEntry>
{
    /// <summary>Определяет точность суммы, индексы и связи записи с контейнером и пользователем.</summary>
    /// <param name="builder">Построитель отображения записи накоплений.</param>
    public void Configure(EntityTypeBuilder<SavingsEntry> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(15, 2);
        builder.ToTable(t => t.HasCheckConstraint("chk_savings_amount_positive", "amount > 0"));
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.Month, x.ContainerId, x.UserId }).IsUnique().HasDatabaseName("uq_savings_month_container_user");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasOne(x => x.Container)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany(x => x.SavingsEntries)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
