namespace Budget.Application.Common;

/// <summary>Общие ограничения и проверки значений, используемые прикладными сервисами.</summary>
public static class Validation
{
    /// <summary>Наибольшая допустимая денежная сумма, сохраняемая в приложении.</summary>
    public const decimal MaxAmount = 999_999_999_999.99m;

    /// <summary>Максимальная длина имени пользователя.</summary>
    public const int MaxUsernameLength = 50;

    /// <summary>Максимальная длина отображаемого или предметного названия.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Максимальная длина комментария.</summary>
    public const int MaxNoteLength = 500;

    /// <summary>Возвращает текущую календарную дату в локальном часовом поясе сервера.</summary>
    /// <returns>Текущая дата без временной составляющей.</returns>
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Проверяет, задан ли цвет шестизначным RGB-кодом с ведущим символом решётки.</summary>
    /// <param name="color">Строка цвета для проверки; null считается недопустимым значением.</param>
    /// <returns><see langword="true"/>, если строка соответствует формату #RRGGBB.</returns>
    public static bool IsValidHexColor(string? color)
        => color is not null && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$");
}
