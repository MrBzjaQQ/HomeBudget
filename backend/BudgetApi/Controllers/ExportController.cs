using Budget.Application.Common;
using Budget.Application.Export;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController(IExportService export, IExcelExportService exporter) : ControllerBase
{
    private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("month")]
    public async Task<IActionResult> Month([FromQuery] int year, [FromQuery] int month, [FromQuery] int? userId, CancellationToken ct)
    {
        ValidateYear(year);
        if (month is < 1 or > 12)
        {
            throw ApiErrorException.BadRequest("Месяц должен быть от 1 до 12");
        }

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var data = await export.GetExportDataAsync(from, to, userId, ct);
        var bytes = exporter.BuildMonthFile(data, year, month);
        var fileName = $"Бюджет_{year:D4}-{month:D2}.xlsx";
        return File(bytes, XlsxMimeType, fileName);
    }

    [HttpGet("year")]
    public async Task<IActionResult> Year([FromQuery] int year, [FromQuery] int? userId, CancellationToken ct)
    {
        ValidateYear(year);
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var data = await export.GetExportDataAsync(from, to, userId, ct);
        var bytes = exporter.BuildYearFile(data, year);
        var fileName = $"Бюджет_{year:D4}.xlsx";
        return File(bytes, XlsxMimeType, fileName);
    }

    private static void ValidateYear(int year)
    {
        if (year is < 2000 or > 2100)
        {
            throw ApiErrorException.BadRequest("Некорректный год");
        }
    }
}
