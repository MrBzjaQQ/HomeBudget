namespace BudgetApi.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "budget-api";

    public string Audience { get; set; } = "budget-web";

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(24);
}
