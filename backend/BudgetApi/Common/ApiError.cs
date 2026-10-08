namespace BudgetApi.Common;

/// <summary>Представляет единый JSON-конверт ошибки API с кодом, сообщением и полевыми ошибками.</summary>
public sealed class ApiError
{
    /// <summary>Машиночитаемый код ошибки.</summary>
    public required string Code { get; init; }

    /// <summary>Понятное клиенту описание ошибки.</summary>
    public required string Message { get; init; }

    /// <summary>Необязательное сопоставление имён полей с сообщениями валидации.</summary>
    public Dictionary<string, string>? Errors { get; init; }
}
