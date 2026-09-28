using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Application.Security;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Users;

public sealed class UserService(IUnitOfWork uow, IPasswordHasher passwordHasher) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct)
    {
        var users = await uow.Users.Query.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
        return users;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var username = request.Username?.Trim();
        var displayName = request.DisplayName?.Trim();
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors["username"] = "Введите логин";
        }
        else if (username.Length > 50)
        {
            errors["username"] = "Логин не длиннее 50 символов";
        }
        else if (await uow.Users.Query.AnyAsync(u => u.Username == username, ct))
        {
            errors["username"] = "Такой логин уже занят";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors["displayName"] = "Введите отображаемое имя";
        }
        else if (displayName.Length > 100)
        {
            errors["displayName"] = "Имя не длиннее 100 символов";
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
        {
            errors["password"] = "Пароль — минимум 6 символов";
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        var user = new User
        {
            Username = username!,
            DisplayName = displayName!,
            PasswordHash = passwordHasher.Hash(request.Password!),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return UserDtos.ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await uow.Users.Query.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Пользователь не найден");

        var errors = new Dictionary<string, string>();

        if (request.DisplayName is not null)
        {
            var displayName = request.DisplayName.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors["displayName"] = "Имя не может быть пустым";
            }
            else if (displayName.Length > 100)
            {
                errors["displayName"] = "Имя не длиннее 100 символов";
            }
            else
            {
                user.DisplayName = displayName;
            }
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            if (request.Password.Length < 6)
            {
                errors["password"] = "Пароль — минимум 6 символов";
            }
            else
            {
                user.PasswordHash = passwordHasher.Hash(request.Password);
            }
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        await uow.SaveChangesAsync(ct);
        return UserDtos.ToDto(user);
    }
}
