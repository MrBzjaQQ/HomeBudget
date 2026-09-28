namespace BudgetApi.Common;

/// <summary>Единый формат ошибок API: {"code","message","errors"}.</summary>
public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public Dictionary<string, string>? Errors { get; init; }
}
