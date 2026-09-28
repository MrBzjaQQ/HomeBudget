namespace Budget.Application.Export;

/// <summary>Формирование Excel-файлов из данных экспорта.</summary>
public interface IExcelExportService
{
    byte[] BuildMonthFile(ExportData data, int year, int month);

    byte[] BuildYearFile(ExportData data, int year);
}
