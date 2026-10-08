using Budget.Application.Users;

namespace Budget.Application.Auth;

/// <summary>Ответ успешной аутентификации с токеном и данными пользователя.</summary>
/// <param name="token">Выданный токен авторизации.</param>
/// <param name="user">Публичные сведения вошедшего пользователя.</param>
/// <param name="serverDate">Текущая дата сервера в формате API.</param>
public class LoginResponse(string token, UserDto user, string serverDate)
{
    /// <summary>Токен доступа для последующих запросов.</summary>
    public string Token { get; } = token;

    /// <summary>Сведения об аутентифицированном пользователе.</summary>
    public UserDto User { get; } = user;

    /// <summary>Текущая дата сервера.</summary>
    public string ServerDate { get; } = serverDate;
}
