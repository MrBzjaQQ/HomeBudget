namespace Budget.Application.Export;

/// <summary>Сборка данных для экспорта операций за период.</summary>
public interface IExportService
{
    Task<ExportData> GetExportDataAsync(DateOnly from, DateOnly to, int? userId, CancellationToken ct);
}
