using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Export;

public sealed class ExportService(IUnitOfWork uow) : IExportService
{
    private sealed record TransactionExportRow(
        DateOnly Date,
        TransactionKind Kind,
        decimal Amount,
        int CategoryId,
        string? Note,
        string UserName);

    /// <summary>Single query for the whole period; month splitting and sheet building happen in the exporter.</summary>
    public async Task<ExportData> GetExportDataAsync(DateOnly from, DateOnly to, int? userId, CancellationToken ct)
    {
        var whoLabel = "вся семья";
        if (userId.HasValue)
        {
            var user = await uow.Users.Query.FirstOrDefaultAsync(u => u.Id == userId, ct)
                ?? throw ApiErrorException.BadRequest("Пользователь не найден");
            whoLabel = user.DisplayName;
        }

        var transactions = await uow.Transactions.Query
            .AsNoTracking()
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .Where(t => userId.HasValue ? t.UserId == userId.Value : true)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Select(t => new TransactionExportRow(
                t.TransactionDate, t.Kind, t.Amount, t.CategoryId, t.Note, t.User.DisplayName))
            .ToListAsync(ct);

        var categories = await uow.Categories.Query.AsNoTracking().Include(c => c.Parent).ToListAsync(ct);
        var pathById = categories.ToDictionary(
            c => c.Id,
            c => c.ParentId is null ? c.Name : $"{categories.First(x => x.Id == c.ParentId.Value).Name} / {c.Name}");

        var rows = transactions
            .Select(t => new ExportTransactionRow(
                t.Date,
                t.Kind == TransactionKind.Income,
                t.Amount,
                pathById.TryGetValue(t.CategoryId, out var path) ? path : "Без категории",
                t.UserName,
                t.Note))
            .ToList();

        return new ExportData { Rows = rows, WhoLabel = whoLabel };
    }
}
