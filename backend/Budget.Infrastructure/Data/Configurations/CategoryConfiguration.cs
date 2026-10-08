using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает хранение категорий, их иерархию и уникальность.</summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Задаёт свойства, индексы и связь категории с дочерними категориями.</summary>
    /// <param name="builder">Построитель отображения категории в модель EF Core.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
        builder.HasIndex(x => x.Kind).HasDatabaseName("idx_cat_kind");
        builder.Property(x => x.Color).HasMaxLength(7);
        builder.HasIndex(x => new { x.Name, x.Kind, x.ParentId }).IsUnique();
        builder.HasMany(x => x.Children)
            .WithOne(x => x.Parent)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
    }
}
