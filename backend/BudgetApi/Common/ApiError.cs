namespace BudgetApi.Common;

/// <summary>Application error with a machine-readable code, serialized as {"code","message"}.</summary>
public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public Dictionary<string, string>? Errors { get; init; }
}

public sealed class ApiErrorException(string code, string message, int statusCode, Dictionary<string, string>? errors = null)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
    public Dictionary<string, string>? Errors { get; } = errors;

    public static ApiErrorException BadRequest(string message, Dictionary<string, string>? errors = null)
        => new("ValidationFailed", message, StatusCodes.Status400BadRequest, errors);

    public static ApiErrorException Unauthorized(string message = "Не авторизован")
        => new("Unauthorized", message, StatusCodes.Status401Unauthorized);

    public static ApiErrorException NotFound(string message = "Не найдено")
        => new("NotFound", message, StatusCodes.Status404NotFound);

    public static ApiErrorException Conflict(string message)
        => new("Conflict", message, StatusCodes.Status409Conflict);
}
