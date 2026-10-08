namespace Budget.Domain.Entities;

/// <summary>Учётная запись участника семейного бюджета.</summary>
public class User
{
    /// <summary>Уникальный идентификатор пользователя.</summary>
    public int Id { get; set; }

    /// <summary>Уникальное имя для входа в приложение.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Имя пользователя, отображаемое в интерфейсе.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Хеш пароля, используемый для проверки учётных данных.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Признак того, что пользователь может войти в приложение.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Дата и время создания учётной записи.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Записи накоплений, связанные с пользователем.</summary>
    public ICollection<SavingsEntry> SavingsEntries { get; set; } = new List<SavingsEntry>();
}
