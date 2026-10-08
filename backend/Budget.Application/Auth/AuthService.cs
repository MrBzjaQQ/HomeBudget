using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Application.Security;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Auth;

/// <summary>Реализует проверку учётных данных и поиск текущего пользователя.</summary>
/// <param name="uow">Рабочая единица доступа к пользовательским данным.</param>
/// <param name="passwordHasher">Компонент проверки сохранённых хешей паролей.</param>
public sealed class AuthService(IUnitOfWork uow, IPasswordHasher passwordHasher) : IAuthService
{
    /// <summary>Проверяет пароль и возвращает активную учётную запись пользователя.</summary>
    /// <param name="username">Имя для входа.</param>
    /// <param name="password">Проверяемый пароль.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пользователь, прошедший проверку пароля и активности.</returns>
    public async Task<User> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await uow.Users.Query.FirstOrDefaultAsync(u => u.Username == username.Trim(), ct);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw ApiErrorException.Unauthorized("Неверный логин или пароль");
        }

        if (!user.IsActive)
        {
            throw ApiErrorException.Unauthorized("Аккаунт деактивирован");
        }

        return user;
    }

    /// <summary>Загружает учётную запись по идентификатору авторизованного пользователя.</summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Найденная учётная запись.</returns>
    public async Task<User> GetCurrentUserAsync(int userId, CancellationToken ct)
        => await uow.Users.Query.FirstOrDefaultAsync(u => u.Id == userId, ct)
           ?? throw ApiErrorException.Unauthorized();
}
