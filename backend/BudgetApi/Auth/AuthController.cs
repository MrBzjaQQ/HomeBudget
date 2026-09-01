using BudgetApi.Common;
using BudgetApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(BudgetDbContext db, JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            throw ApiErrorException.BadRequest("Введите логин и пароль");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username.Trim(), ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw ApiErrorException.Unauthorized("Неверный логин или пароль");
        }

        if (!user.IsActive)
        {
            throw ApiErrorException.Unauthorized("Аккаунт деактивирован");
        }

        var token = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, UserDtos.ToDto(user), ServerDate()));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var id = User.GetUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            throw ApiErrorException.Unauthorized();
        }

        return Ok(new MeResponse(UserDtos.ToDto(user), ServerDate()));
    }

    private static string ServerDate() => Validation.Today().ToString("yyyy-MM-dd");
}

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LoginResponse(string token, UserDto user, string serverDate)
{
    public string Token { get; } = token;
    public UserDto User { get; } = user;
    public string ServerDate { get; } = serverDate;
}

public record MeResponse(UserDto User, string ServerDate);
