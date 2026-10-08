namespace Budget.Application.Common;

/// <summary>
/// Ошибка приложения с машинно-читаемым кодом.
/// Сериализуется в единый JSON-формат ошибок API: {"code","message","errors"}.
/// </summary>
/// <param name="code">Стабильный код ошибки для клиента.</param>
/// <param name="message">Человекочитаемое описание причины ошибки.</param>
/// <param name="statusCode">HTTP-код ответа, связанный с ошибкой.</param>
/// <param name="errors">Необязательные сообщения по отдельным полям запроса.</param>
public sealed class ApiErrorException(string code, string message, int statusCode, Dictionary<string, string>? errors = null)
    : Exception(message)
{
    /// <summary>Машинно-читаемый код ошибки.</summary>
    public string Code { get; } = code;

    /// <summary>HTTP-код ответа для передачи клиенту.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Ошибки отдельных полей, если они имеются.</summary>
    public Dictionary<string, string>? Errors { get; } = errors;

    /// <summary>Создаёт ошибку некорректного запроса.</summary>
    /// <param name="message">Общее описание ошибки валидации.</param>
    /// <param name="errors">Необязательные ошибки отдельных полей.</param>
    /// <returns>Исключение с кодом ValidationFailed и статусом 400.</returns>
    public static ApiErrorException BadRequest(string message, Dictionary<string, string>? errors = null)
        => new(ApiErrorCodes.ValidationFailed, message, 400, errors);

    /// <summary>Создаёт ошибку отсутствующей или недействительной авторизации.</summary>
    /// <param name="message">Описание причины отказа, либо стандартный текст.</param>
    /// <returns>Исключение с кодом Unauthorized и статусом 401.</returns>
    public static ApiErrorException Unauthorized(string message = "Не авторизован")
        => new(ApiErrorCodes.Unauthorized, message, 401);

    /// <summary>Создаёт ошибку отсутствующего ресурса.</summary>
    /// <param name="message">Описание отсутствующего ресурса, либо стандартный текст.</param>
    /// <returns>Исключение с кодом NotFound и статусом 404.</returns>
    public static ApiErrorException NotFound(string message = "Не найдено")
        => new(ApiErrorCodes.NotFound, message, 404);

    /// <summary>Создаёт ошибку конфликта с текущим состоянием данных.</summary>
    /// <param name="message">Описание причины конфликта.</param>
    /// <returns>Исключение с кодом Conflict и статусом 409.</returns>
    public static ApiErrorException Conflict(string message)
        => new(ApiErrorCodes.Conflict, message, 409);
}
