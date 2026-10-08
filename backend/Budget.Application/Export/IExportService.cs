namespace Budget.Application.Export;

/// <summary>Собирает доменные данные операций в представление для экспорта.</summary>
public interface IExportService
{
    /// <summary>Получает операции пользователя или всей семьи в заданном интервале дат.</summary>
    /// <param name="from">Начальная дата периода, включительно.</param>
    /// <param name="to">Конечная дата периода, включительно.</param>
    /// <param name="userId">Идентификатор пользователя либо null для всей семьи.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Набор строк и подпись выбранного охвата экспорта.</returns>
    Task<ExportData> GetExportDataAsync(DateOnly from, DateOnly to, int? userId, CancellationToken ct);
}
