namespace Budget.Application.Common;

/// <summary>
/// Ошибка приложения с машинно-читаемым кодом.
/// Сериализуется в единый JSON-формат ошибок API: {"code","message","errors"}.
/// </summary>
public sealed class ApiErrorException(string code, string message, int statusCode, Dictionary<string, string>? errors = null)
    : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public Dictionary<string, string>? Errors { get; } = errors;

    public static ApiErrorException BadRequest(string message, Dictionary<string, string>? errors = null)
        => new("ValidationFailed", message, 400, errors);

    public static ApiErrorException Unauthorized(string message = "Не авторизован")
        => new("Unauthorized", message, 401);

    public static ApiErrorException NotFound(string message = "Не найдено")
        => new("NotFound", message, 404);

    public static ApiErrorException Conflict(string message)
        => new("Conflict", message, 409);
}
