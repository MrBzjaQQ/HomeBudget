using Budget.Domain.Entities;

namespace Budget.Application.Auth;

public interface IAuthService
{
    /// <summary>Проверяет учётные данные и возвращает активного пользователя.</summary>
    Task<User> LoginAsync(string username, string password, CancellationToken ct);

    /// <summary>Возвращает пользователя по идентификатору из токена.</summary>
    Task<User> GetCurrentUserAsync(int userId, CancellationToken ct);
}
