namespace Budget.Application.Export;

public sealed record ExportTransactionRow(
    DateOnly Date,
    bool IsIncome,
    decimal Amount,
    string CategoryPath,
    string UserName,
    string? Note);

public sealed class ExportData
{
    public required IReadOnlyList<ExportTransactionRow> Rows { get; init; }
    public required string WhoLabel { get; init; }
}
