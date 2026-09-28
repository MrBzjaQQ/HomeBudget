using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Application.Security;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Auth;

public sealed class AuthService(IUnitOfWork uow, IPasswordHasher passwordHasher) : IAuthService
{
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

    public async Task<User> GetCurrentUserAsync(int userId, CancellationToken ct)
        => await uow.Users.Query.FirstOrDefaultAsync(u => u.Id == userId, ct)
           ?? throw ApiErrorException.Unauthorized();
}
