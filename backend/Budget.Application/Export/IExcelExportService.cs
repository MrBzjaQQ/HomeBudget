namespace Budget.Application.Export;

/// <summary>Преобразует данные операций в книги Excel.</summary>
public interface IExcelExportService
{
    /// <summary>Формирует книгу Excel с листами за каждый месяц заданного периода.</summary>
    /// <param name="data">Подготовленные к экспорту операции и подпись охвата.</param>
    /// <param name="year">Год выбранного периода.</param>
    /// <param name="month">Месяц для файла месячного экспорта.</param>
    /// <returns>Содержимое книги Excel в формате XLSX.</returns>
    byte[] BuildMonthFile(ExportData data, int year, int month);

    /// <summary>Формирует книгу Excel со сводными данными за год.</summary>
    /// <param name="data">Подготовленные к экспорту операции и подпись охвата.</param>
    /// <param name="year">Год выбранного периода.</param>
    /// <returns>Содержимое книги Excel в формате XLSX.</returns>
    byte[] BuildYearFile(ExportData data, int year);
}
