using BudgetApi.Common;
using BudgetApi.Data;
using BudgetApi.Dtos;
using BudgetApi.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController(BudgetDbContext db, ExcelExportService exporter) : ControllerBase
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
        var data = await BuildExportDataAsync(from, to, userId, ct);
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
        var data = await BuildExportDataAsync(from, to, userId, ct);
        var bytes = exporter.BuildYearFile(data, year);
        var fileName = $"Бюджет_{year:D4}.xlsx";
        return File(bytes, XlsxMimeType, fileName);
    }

    private void ValidateYear(int year)
    {
        if (year is < 2000 or > 2100)
        {
            throw ApiErrorException.BadRequest("Некорректный год");
        }
    }

    /// <summary>Single query for the whole period; month splitting and sheet building happen in C#.</summary>
    private async Task<ExportData> BuildExportDataAsync(DateOnly from, DateOnly to, int? userId, CancellationToken ct)
    {
        var whoLabel = "вся семья";
        if (userId.HasValue)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                ?? throw ApiErrorException.BadRequest("Пользователь не найден");
            whoLabel = user.DisplayName;
        }

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .Where(t => userId.HasValue ? t.UserId == userId.Value : true)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Select(t => new TransactionExportRowDto(
                t.TransactionDate, t.Kind, t.Amount, t.CategoryId, t.Note, t.User.DisplayName))
            .ToListAsync(ct);

        var categories = await db.Categories.AsNoTracking().Include(c => c.Parent).ToListAsync(ct);
        var pathById = categories.ToDictionary(
            c => c.Id,
            c => c.ParentId is null ? c.Name : $"{categories.First(x => x.Id == c.ParentId.Value).Name} / {c.Name}");

        var rows = transactions
            .Select(t => new ExportTransactionRow(
                t.Date,
                t.Kind == Data.Entities.TransactionKind.Income,
                t.Amount,
                pathById.TryGetValue(t.CategoryId, out var path) ? path : "Без категории",
                t.UserName,
                t.Note))
            .ToList();

        return new ExportData { Rows = rows, WhoLabel = whoLabel };
    }
}

public record TransactionExportRowDto(
    DateOnly Date,
    Data.Entities.TransactionKind Kind,
    decimal Amount,
    int CategoryId,
    string? Note,
    string UserName);
