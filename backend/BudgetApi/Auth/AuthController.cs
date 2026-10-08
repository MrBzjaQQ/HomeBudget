using Budget.Application.Auth;
using Budget.Application.Common;
using Budget.Application.Users;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Auth;

/// <summary>Предоставляет HTTP-операции входа и получения профиля текущего пользователя.</summary>
/// <param name="auth">Сервис аутентификации и поиска профиля пользователя.</param>
/// <param name="tokenService">Служба выпуска JWT для вошедшего пользователя.</param>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, JwtTokenService tokenService) : ControllerBase
{
    /// <summary>Проверяет учётные данные и возвращает токен вместе с профилем пользователя.</summary>
    /// <param name="request">Логин и пароль для входа.</param>
    /// <param name="ct">Токен отмены обработки запроса.</param>
    /// <returns>Ответ с JWT, данными пользователя и серверной датой.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            throw ApiErrorException.BadRequest("Введите логин и пароль");
        }

        var user = await auth.LoginAsync(request.Username, request.Password, ct);
        var token = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, UserDtos.ToDto(user), ServerDate()));
    }

    /// <summary>Возвращает профиль пользователя, соответствующего токену авторизации.</summary>
    /// <param name="ct">Токен отмены обработки запроса.</param>
    /// <returns>Профиль текущего пользователя и серверная дата.</returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var user = await auth.GetCurrentUserAsync(User.GetUserId(), ct);
        return Ok(new MeResponse(UserDtos.ToDto(user), ServerDate()));
    }

    private static string ServerDate() => Validation.Today().ToString(DateFormats.Date);
}
