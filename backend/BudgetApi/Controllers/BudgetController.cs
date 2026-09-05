using BudgetApi.Common;
using BudgetApi.Data;
using BudgetApi.Data.Entities;
using BudgetApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/budget")]
[Authorize]
public class BudgetController(BudgetDbContext db) : ControllerBase
{
    private const string MonthError = "Формат месяца: yyyy-MM";

    [HttpGet]
    public async Task<ActionResult<BudgetMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var firstDay = ParseMonth(month);
        if (firstDay is null)
        {
            throw ApiErrorException.BadRequest(MonthError, new Dictionary<string, string> { ["month"] = MonthError });
        }

        var monthStart = firstDay.Value;
        var monthEnd = monthStart.AddMonths(1);
        var monthKey = monthStart.ToString("yyyy-MM");

        var items = await db.BudgetItems.AsNoTracking()
            .Include(x => x.Category)
            .ThenInclude(c => c!.Parent)
            .Where(x => x.Month == monthStart)
            .OrderBy(x => x.CategoryId)
            .ThenBy(x => x.Id)
            .Select(x => new BudgetItemDto(
                x.Id, monthKey, x.CategoryId,
                x.Category.Name, x.Category.Parent != null ? x.Category.Parent.Name : null, x.Category.Color,
                x.Amount, x.Note, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        var plannedByCategory = await db.BudgetItems.AsNoTracking()
            .Where(x => x.Month == monthStart)
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Planned = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var actualByCategory = await db.Transactions.AsNoTracking()
            .Where(t => t.Kind == TransactionKind.Expense && t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Actual = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var plannedById = plannedByCategory.ToDictionary(x => x.CategoryId, x => x.Planned);
        var actualById = actualByCategory.ToDictionary(x => x.CategoryId, x => x.Actual);
        var categoryIds = plannedById.Keys.Concat(actualById.Keys).Distinct().ToHashSet();

        var categories = await db.Categories.AsNoTracking()
            .Include(c => c.Parent)
            .Where(c => c.Kind == TransactionKind.Expense && categoryIds.Contains(c.Id))
            .ToListAsync(ct);

        var rows = categories
            .Select(c =>
            {
                var planned = plannedById.GetValueOrDefault(c.Id);
                var actual = actualById.GetValueOrDefault(c.Id);
                return new BudgetCategoryDto(c.Id, c.Name, c.Parent?.Name, c.Color, planned, actual, planned - actual);
            })
            .OrderByDescending(r => r.Planned)
            .ThenByDescending(r => r.Actual)
            .ThenBy(r => r.Name)
            .ToList();

        var plannedTotal = plannedById.Values.Sum();
        var actualTotal = actualById.Values.Sum();
        var totals = new BudgetTotalsDto(plannedTotal, actualTotal, plannedTotal - actualTotal);

        return Ok(new BudgetMonthResponse(monthKey, items, rows, totals));
    }

    [HttpPost]
    public async Task<ActionResult<BudgetItemDto>> Create([FromBody] CreateBudgetItemRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequest(request, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        var now = DateTime.UtcNow;
        var item = new BudgetItem
        {
            Month = validated.Month,
            CategoryId = validated.CategoryId,
            Amount = validated.Amount,
            Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.BudgetItems.Add(item);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, await GetDtoAsync(item.Id, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BudgetItemDto>> Update(long id, [FromBody] CreateBudgetItemRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var item = await db.BudgetItems.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Позиция бюджета не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequest(request, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        item.Month = validated.Month;
        item.CategoryId = validated.CategoryId;
        item.Amount = validated.Amount;
        item.Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim();

        await db.SaveChangesAsync(ct);
        return Ok(await GetDtoAsync(item.Id, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await db.BudgetItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            throw ApiErrorException.NotFound("Позиция бюджета не найдена");
        }

        db.BudgetItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private sealed record ValidatedBudgetItem(DateOnly Month, int CategoryId, decimal Amount, string? Note);

    private async Task<ValidatedBudgetItem?> ValidateRequest(
        CreateBudgetItemRequest request, Dictionary<string, string> errors, CancellationToken ct)
    {
        DateOnly? month = null;
        if (string.IsNullOrWhiteSpace(request.Month))
        {
            errors["month"] = MonthError;
        }
        else
        {
            var parsed = ParseMonth(request.Month);
            if (parsed is null)
            {
                errors["month"] = MonthError;
            }
            else
            {
                month = parsed.Value;
            }
        }

        decimal? amount = null;
        if (request.Amount is null || request.Amount <= 0)
        {
            errors["amount"] = "Сумма должна быть больше нуля";
        }
        else if (request.Amount > Validation.MaxAmount)
        {
            errors["amount"] = $"Сумма не должна превышать {Validation.MaxAmount:N2}";
        }
        else
        {
            amount = request.Amount.Value;
        }

        int? categoryId = null;
        if (request.CategoryId is null)
        {
            errors["categoryId"] = "Выберите категорию";
        }
        else
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, ct);
            if (category is null)
            {
                errors["categoryId"] = "Категория не найдена";
            }
            else if (category.Kind != TransactionKind.Expense)
            {
                errors["categoryId"] = "Бюджет можно планировать только по категориям расходов";
            }
            else
            {
                categoryId = category.Id;
            }
        }

        string? note = request.Note;
        var noteOk = true;
        if (note is not null && note.Length > Validation.MaxNoteLength)
        {
            errors["note"] = $"Комментарий не может превышать {Validation.MaxNoteLength} символов";
            note = null;
            noteOk = false;
        }

        if (month is null || amount is null || categoryId is null || !noteOk)
        {
            return null;
        }

        return new ValidatedBudgetItem(month.Value, categoryId.Value, amount.Value, note);
    }

    private static DateOnly? ParseMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month) || month.Length != 7 || month[4] != '-')
        {
            return null;
        }

        if (!int.TryParse(month[..4], out var year) || year is < 1 or > 9999)
        {
            return null;
        }

        if (!int.TryParse(month[5..], out var value) || value is < 1 or > 12)
        {
            return null;
        }

        return new DateOnly(year, value, 1);
    }

    private async Task<BudgetItemDto> GetDtoAsync(long id, CancellationToken ct)
    {
        var item = await db.BudgetItems
            .Include(x => x.Category)
            .ThenInclude(c => c!.Parent)
            .FirstAsync(x => x.Id == id, ct);
        return new BudgetItemDto(
            item.Id, item.Month.ToString("yyyy-MM"), item.CategoryId,
            item.Category.Name, item.Category.Parent?.Name, item.Category.Color,
            item.Amount, item.Note, item.CreatedAt, item.UpdatedAt);
    }
}
