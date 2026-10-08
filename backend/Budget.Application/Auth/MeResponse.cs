using Budget.Application.Users;

namespace Budget.Application.Auth;

/// <summary>Ответ с данными текущего пользователя и датой сервера.</summary>
/// <param name="User">Публичные сведения текущего пользователя.</param>
/// <param name="ServerDate">Текущая дата сервера в формате API.</param>
public sealed record MeResponse(UserDto User, string ServerDate);
