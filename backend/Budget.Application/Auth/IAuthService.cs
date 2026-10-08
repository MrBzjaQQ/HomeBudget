using Budget.Domain.Entities;

namespace Budget.Application.Auth;

/// <summary>Предоставляет операции входа и получения текущей учётной записи.</summary>
public interface IAuthService
{
    /// <summary>Проверяет пароль и возвращает учётную запись, если она активна.</summary>
    /// <param name="username">Имя пользователя для входа.</param>
    /// <param name="password">Пароль в открытом виде для проверки хешером.</param>
    /// <param name="ct">Токен отмены асинхронной операции.</param>
    /// <returns>Проверенная учётная запись пользователя.</returns>
    Task<User> LoginAsync(string username, string password, CancellationToken ct);

    /// <summary>Находит учётную запись пользователя по идентификатору.</summary>
    /// <param name="userId">Идентификатор пользователя из текущего контекста авторизации.</param>
    /// <param name="ct">Токен отмены асинхронной операции.</param>
    /// <returns>Найденная учётная запись.</returns>
    Task<User> GetCurrentUserAsync(int userId, CancellationToken ct);
}
