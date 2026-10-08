using System.Security.Claims;
using System.Text;
using Budget.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BudgetApi.Auth;

/// <summary>Формирует подписанные JWT для аутентифицированных пользователей.</summary>
/// <param name="optionsAccessor">Настройки издателя, аудитории, ключа и срока действия токена.</param>
public sealed class JwtTokenService(IOptions<JwtOptions> optionsAccessor)
{
    private readonly JwtOptions _options = optionsAccessor.Value;

    /// <summary>Создаёт JWT с идентификатором, логином и отображаемым именем пользователя.</summary>
    /// <param name="user">Пользователь, чьи данные включаются в claims токена.</param>
    /// <returns>Подписанный компактный JWT в виде строки.</returns>
    public string CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var handler = new JsonWebTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(_options.Lifetime),
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("display_name", user.DisplayName),
            }),
        };

        return handler.CreateToken(descriptor);
    }
}
