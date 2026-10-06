using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BudgetApi.Common;

/// <summary>Maps raw model-binding messages to the friendly Russian unified error envelope.</summary>
public static class ModelStateSanitizer
{
    public const string GenericMessage = "Некорректные данные";
    public const string MalformedBodyMessage = "Некорректные данные запроса";
    public const string EmptyBodyMessage = "Пустое тело запроса";

    public static (string Message, Dictionary<string, string>? Errors) Sanitize(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string>();
        var hadEmptyBody = false;
        var hadMalformedBody = false;

        foreach (var (key, entry) in modelState)
        {
            foreach (var error in entry.Errors)
            {
                var raw = error.ErrorMessage ?? string.Empty;

                if (raw.StartsWith("A non-empty request body is required.", StringComparison.Ordinal))
                {
                    hadEmptyBody = true;
                    continue;
                }

                if (key == "request")
                {
                    if (raw.StartsWith("The request field is required.", StringComparison.Ordinal))
                    {
                        hadEmptyBody = true;
                    }
                    else if (IsRawMessage(raw))
                    {
                        var field = ExtractFieldFromPath(raw);
                        if (field is not null)
                        {
                            errors[field] = FriendlyForRawMessage(raw, field);
                        }
                        else
                        {
                            hadMalformedBody = true;
                        }
                    }

                    continue;
                }

                if (key.StartsWith("$.", StringComparison.Ordinal))
                {
                    var field = key[2..];
                    if (IsRawMessage(raw))
                    {
                        errors[field] = FriendlyForRawMessage(raw, field);
                    }
                    else
                    {
                        errors.TryAdd(field, raw);
                    }

                    continue;
                }

                if (key == "$")
                {
                    hadMalformedBody = true;
                    continue;
                }

                if (raw.StartsWith("The value '", StringComparison.Ordinal))
                {
                    errors.TryAdd(key, FriendlyForFieldValue(key));
                    continue;
                }

                errors.TryAdd(key, raw);
            }
        }

        if (errors.Count > 0)
        {
            return (GenericMessage, errors);
        }

        if (hadMalformedBody)
        {
            return (MalformedBodyMessage, null);
        }

        return (hadEmptyBody ? EmptyBodyMessage : GenericMessage, null);
    }

    private static bool IsRawMessage(string message)
        => message.Contains("Path: $", StringComparison.Ordinal)
            || message.Contains("System.", StringComparison.Ordinal)
            || message.Contains("could not be converted", StringComparison.Ordinal)
            || message.Contains("invalid JSON", StringComparison.Ordinal);

    private static string? ExtractFieldFromPath(string raw)
    {
        const string marker = "Path: $.";
        var index = raw.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var rest = raw[(index + marker.Length)..];
        var end = rest.IndexOfAny(new[] { ' ', '|' });
        var field = end < 0 ? rest : rest[..end];
        return field.Length > 0 ? field : null;
    }

    private static string FriendlyForRawMessage(string raw, string field)
    {
        if (raw.Contains("DateOnly", StringComparison.Ordinal))
        {
            return "Некорректная дата";
        }

        if (raw.Contains("TransactionKind", StringComparison.Ordinal))
        {
            return "Некорректный тип операции";
        }

        return FriendlyForFieldValue(field);
    }

    private static string FriendlyForFieldValue(string field)
        => field.ToLowerInvariant() switch
        {
            "date" or "from" or "to" => "Некорректная дата",
            "kind" => "Некорректный тип операции",
            "amount" => "Некорректная сумма",
            _ => "Некорректное значение",
        };
}
