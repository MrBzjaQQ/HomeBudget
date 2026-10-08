using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает хранение контейнеров и счетов накоплений.</summary>
public sealed class SavingsContainerConfiguration : IEntityTypeConfiguration<SavingsContainer>
{
    /// <summary>Определяет ограничения полей и связи контейнера накоплений.</summary>
    /// <param name="builder">Построитель отображения контейнера накоплений.</param>
    public void Configure(EntityTypeBuilder<SavingsContainer> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7);
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_savings_container_name");
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
    }
}
