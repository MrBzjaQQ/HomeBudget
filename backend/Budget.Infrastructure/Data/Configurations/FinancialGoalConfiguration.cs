using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает ограничения и связь финансовой цели с контейнером накоплений.</summary>
public sealed class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
{
    /// <summary>Задаёт имя, сумму цели, ограничение положительной суммы и связь с контейнером.</summary>
    /// <param name="builder">Построитель отображения финансовой цели.</param>
    public void Configure(EntityTypeBuilder<FinancialGoal> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetAmount).HasPrecision(15, 2);
        builder.ToTable(t => t.HasCheckConstraint("chk_goal_amount_positive", "target_amount > 0"));
        builder.HasIndex(x => x.ContainerId);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasOne(x => x.Container)
            .WithMany(x => x.Goals)
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
