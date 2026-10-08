namespace BudgetApi.Auth;

/// <summary>Параметры выпуска и проверки JWT из секции конфигурации <c>Jwt</c>.</summary>
public class JwtOptions
{
    /// <summary>Имя секции конфигурации токена.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Секретный ключ подписи токена; значение задаётся конфигурацией окружения.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Ожидаемый издатель токена.</summary>
    public string Issuer { get; set; } = "budget-api";

    /// <summary>Ожидаемая аудитория токена.</summary>
    public string Audience { get; set; } = "budget-web";

    /// <summary>Время действия выпущенного токена.</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(24);
}
