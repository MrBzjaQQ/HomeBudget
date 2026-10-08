namespace Budget.Application.Common;

/// <summary>Стабильные машинно-читаемые коды ошибок приложения.</summary>
public static class ApiErrorCodes
{
    /// <summary>Запрос не прошёл валидацию.</summary>
    public const string ValidationFailed = "ValidationFailed";

    /// <summary>Пользователь не аутентифицирован или не имеет доступа.</summary>
    public const string Unauthorized = "Unauthorized";

    /// <summary>Запрошенный ресурс не найден.</summary>
    public const string NotFound = "NotFound";

    /// <summary>Операция конфликтует с текущими данными.</summary>
    public const string Conflict = "Conflict";

    /// <summary>Необработанная внутренняя ошибка приложения.</summary>
    public const string InternalServerError = "InternalServerError";
}
