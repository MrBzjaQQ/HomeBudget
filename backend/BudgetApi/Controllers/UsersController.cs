using BudgetApi.Auth;
using BudgetApi.Common;
using BudgetApi.Data;
using BudgetApi.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(BudgetDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

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
        else if (await db.Users.AnyAsync(u => u.Username == username, ct))
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, UserDtos.ToDto(user));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
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
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
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

        await db.SaveChangesAsync(ct);
        return Ok(UserDtos.ToDto(user));
    }
}

public record CreateUserRequest
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
}

public record UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
}
