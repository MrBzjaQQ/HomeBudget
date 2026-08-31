using BudgetApi.Auth;
using BudgetApi.Common;
using BudgetApi.Data;
using BudgetApi.Data.Entities;
using BudgetApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(BudgetDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TransactionListResponse>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? userId,
        [FromQuery] List<int>? categoryId,
        [FromQuery] TransactionKind? kind,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken ct = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw ApiErrorException.BadRequest("Начало периода позже его конца");
        }

        pageSize = Math.Clamp(pageSize, 1, 500);
        page = Math.Max(page, 1);

        var query = BuildFilteredQuery(from, to, userId, categoryId, kind, q);

        var total = await query.CountAsync(ct);

        var totalsByKind = await query
            .GroupBy(t => t.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        decimal income = totalsByKind.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Total);
        decimal expense = totalsByKind.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Total);

        var sortField = string.Equals(sort, "amount", StringComparison.OrdinalIgnoreCase) ? "amount" : "date";
        var ascending = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);

        query = sortField == "amount"
            ? (ascending ? query.OrderBy(t => t.Amount).ThenBy(t => t.TransactionDate)
                         : query.OrderByDescending(t => t.Amount).ThenBy(t => t.TransactionDate))
            : (ascending ? query.OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
                         : query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id));

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Kind,
                t.Amount,
                t.TransactionDate,
                t.CategoryId,
                t.Category.Name,
                t.Category.Parent != null ? t.Category.Parent.Name : null,
                t.Category.Color,
                t.UserId,
                t.User.DisplayName,
                t.Note,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(ct);

        return Ok(new TransactionListResponse(items, total, page, pageSize, new TotalsDto(income, expense, income - expense)));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransactionDto>> Get(long id, CancellationToken ct)
    {
        var exists = await db.Transactions.AnyAsync(t => t.Id == id, ct);
        if (!exists)
        {
            throw ApiErrorException.NotFound("Операция не найдена");
        }

        return Ok(await GetDtoAsync(id, ct));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SummaryResponse>> Summary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? groupBy,
        CancellationToken ct = default)
    {
        var group = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "category";
        var query = BuildFilteredQuery(from, to, null, null, null, null);

        if (group == "month")
        {
            var monthRows = await query
                .GroupBy(t => new { Y = t.TransactionDate.Year, M = t.TransactionDate.Month })
                .Select(g => new SummaryMonthDto(
                    $"{g.Key.Y:D4}-{g.Key.M:D2}",
                    g.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Amount),
                    g.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Amount)))
                .ToListAsync(ct);
            return Ok(new SummaryResponse(group, monthRows));
        }

        var perCategory = await query
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                Id = g.Key,
                Income = g.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Amount),
                Expense = g.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Amount),
            })
            .ToListAsync(ct);

        var byId = perCategory.ToDictionary(x => x.Id);
        var allCategories = await db.Categories.AsNoTracking().Include(c => c.Parent).ToListAsync(ct);
        var rows = allCategories
            .Where(c => byId.ContainsKey(c.Id))
            .Select(c =>
            {
                var sums = byId[c.Id];
                return new SummaryCategoryDto(
                    c.Id,
                    c.Name,
                    c.Kind.ToString().ToUpperInvariant(),
                    c.ParentId,
                    c.Parent?.Name,
                    c.Color,
                    sums.Income,
                    sums.Expense);
            })
            .OrderBy(r => r.Expense).ThenBy(r => r.Income)
            .ToList();

        return Ok(new SummaryResponse(group, rows));
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
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
        var transaction = new Transaction
        {
            UserId = validated.UserId,
            CategoryId = validated.CategoryId,
            Kind = validated.Kind,
            Amount = validated.Amount,
            TransactionDate = validated.Date,
            Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        var dto = await GetDtoAsync(transaction.Id, ct);
        return CreatedWithLocation(dto);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<TransactionDto>> Update(long id, [FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Операция не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequest(request, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        transaction.Kind = validated.Kind;
        transaction.Amount = validated.Amount;
        transaction.TransactionDate = validated.Date;
        transaction.CategoryId = validated.CategoryId;
        transaction.UserId = validated.UserId;
        transaction.Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim();

        await db.SaveChangesAsync(ct);
        return Ok(await GetDtoAsync(transaction.Id, ct));
    }

    private ObjectResult CreatedWithLocation(TransactionDto dto)
        => StatusCode(StatusCodes.Status201Created, dto);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (transaction is null)
        {
            throw ApiErrorException.NotFound("Операция не найдена");
        }

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private IQueryable<Transaction> BuildFilteredQuery(
        DateOnly? from,
        DateOnly? to,
        int? userId,
        List<int>? categoryIds,
        TransactionKind? kind,
        string? search)
    {
        var query = db.Transactions.AsNoTracking();
        if (from.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= to.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }

        if (categoryIds is { Count: > 0 })
        {
            var ids = categoryIds.Distinct().ToList();
            query = query.Where(t => ids.Contains(t.CategoryId));
        }

        if (kind.HasValue)
        {
            query = query.Where(t => t.Kind == kind.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Translates to ILIKE on PostgreSQL and works on the in-memory provider.
            var pattern = search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Note != null && t.Note.ToLower().Contains(pattern));
        }

        return query;
    }

    private sealed record ValidatedTransaction(
        TransactionKind Kind, decimal Amount, DateOnly Date, int CategoryId, int UserId, string? Note);

    private async Task<ValidatedTransaction?> ValidateRequest(
        CreateTransactionRequest request, Dictionary<string, string> errors, CancellationToken ct)
    {
        TransactionKind? kind = null;
        if (request.Kind is null || !Enum.IsDefined(request.Kind.Value))
        {
            errors["kind"] = "Укажите тип операции";
        }
        else
        {
            kind = request.Kind.Value;
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

        DateOnly? date = null;
        var today = Validation.Today();
        if (request.Date is null)
        {
            errors["date"] = "Укажите дату операции";
        }
        else if (request.Date.Value > today)
        {
            errors["date"] = "Дата операции не может быть в будущем";
        }
        else
        {
            date = request.Date.Value;
        }

        int? categoryId = null;
        if (request.CategoryId is null)
        {
            errors["categoryId"] = "Выберите категорию";
        }
        else if (kind.HasValue)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, ct);
            if (category is null)
            {
                errors["categoryId"] = "Категория не найдена";
            }
            else if (category.Kind != kind)
            {
                errors["categoryId"] = "Категория не подходит для выбранного типа операции";
            }
            else
            {
                categoryId = category.Id;
            }
        }

        var userId = User.GetUserId();
        if (request.UserId.HasValue)
        {
            var exists = await db.Users.AnyAsync(u => u.Id == request.UserId.Value, ct);
            if (!exists)
            {
                errors["userId"] = "Пользователь не найден";
            }
            else
            {
                userId = request.UserId.Value;
            }
        }

        string? note = request.Note;
        if (note is not null && note.Length > Validation.MaxNoteLength)
        {
            errors["note"] = $"Комментарий не длиннее {Validation.MaxNoteLength} символов";
            note = null;
        }

        if (kind is null || amount is null || date is null || categoryId is null)
        {
            return null;
        }

        return new ValidatedTransaction(kind.Value, amount.Value, date.Value, categoryId.Value, userId, note);
    }

    private async Task<TransactionDto> GetDtoAsync(long id, CancellationToken ct)
    {
        var t = await db.Transactions
            .Include(x => x.Category)
            .ThenInclude(c => c!.Parent)
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id, ct);
        return new TransactionDto(
            t.Id, t.Kind, t.Amount, t.TransactionDate,
            t.CategoryId, t.Category.Name, t.Category.Parent?.Name, t.Category.Color,
            t.UserId, t.User.DisplayName, t.Note, t.CreatedAt, t.UpdatedAt);
    }
}
