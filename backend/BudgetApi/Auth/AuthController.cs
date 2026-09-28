using Budget.Application.Auth;
using Budget.Application.Common;
using Budget.Application.Users;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, JwtTokenService tokenService) : ControllerBase
{
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

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var user = await auth.GetCurrentUserAsync(User.GetUserId(), ct);
        return Ok(new MeResponse(UserDtos.ToDto(user), ServerDate()));
    }

    private static string ServerDate() => Validation.Today().ToString("yyyy-MM-dd");
}
