namespace Budget.Application.Common;

public static class Validation
{
    public const decimal MaxAmount = 999_999_999_999.99m;
    public const int MaxNoteLength = 500;

    /// <summary>Today's date in the server's local timezone (server runs in MSK).</summary>
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    public static bool IsValidHexColor(string? color)
        => color is not null && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$");
}
