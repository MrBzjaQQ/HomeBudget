namespace Budget.Application.Auth;

/// <summary>Данные учётной записи, передаваемые для входа в приложение.</summary>
public class LoginRequest
{
    /// <summary>Имя пользователя, используемое при аутентификации.</summary>
    public string? Username { get; set; }

    /// <summary>Пароль пользователя для проверки.</summary>
    public string? Password { get; set; }
}
