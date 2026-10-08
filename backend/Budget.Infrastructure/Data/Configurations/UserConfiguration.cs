using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budget.Infrastructure.Data.Configurations;

/// <summary>Настраивает ограничения и индексы сущности пользователя.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Задаёт длины полей, уникальность логина и значения по умолчанию пользователя.</summary>
    /// <param name="builder">Построитель отображения пользователя в модель EF Core.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
    }
}
