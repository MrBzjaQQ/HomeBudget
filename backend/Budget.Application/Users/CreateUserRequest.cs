namespace Budget.Application.Users;

/// <summary>Поля для создания новой учётной записи.</summary>
public sealed record CreateUserRequest
{
    /// <summary>Уникальное имя для входа.</summary>
    public string? Username { get; set; }

    /// <summary>Имя, показываемое в журнале и сводках.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Пароль новой учётной записи.</summary>
    public string? Password { get; set; }
}
