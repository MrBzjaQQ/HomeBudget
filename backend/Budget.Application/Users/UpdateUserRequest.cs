namespace Budget.Application.Users;

/// <summary>Необязательные изменения учётной записи пользователя.</summary>
public sealed record UpdateUserRequest
{
    /// <summary>Новое отображаемое имя; null означает оставить без изменения.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Новый пароль; null или пустая строка означает оставить прежний.</summary>
    public string? Password { get; set; }

    /// <summary>Новое состояние активности; null означает оставить текущее.</summary>
    public bool? IsActive { get; set; }
}
