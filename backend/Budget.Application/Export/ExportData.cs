namespace Budget.Application.Export;

/// <summary>Набор нормализованных строк, подготовленных для формирования файла экспорта.</summary>
public sealed class ExportData
{
    /// <summary>Операции, упорядоченные и готовые к выгрузке.</summary>
    public required IReadOnlyList<ExportTransactionRow> Rows { get; init; }

    /// <summary>Текстовое обозначение пользователя или семьи для заголовка отчёта.</summary>
    public required string WhoLabel { get; init; }
}
