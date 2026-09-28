using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Transactions;

public sealed class TransactionService(IUnitOfWork uow) : ITransactionService
{
    public async Task<TransactionListResponse> ListAsync(
        DateOnly? from,
        DateOnly? to,
        int? userId,
        List<int>? categoryIds,
        TransactionKind? kind,
        string? q,
        int page,
        int pageSize,
        string? sort,
        string? dir,
        CancellationToken ct)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw ApiErrorException.BadRequest("Начало периода позже его конца");
        }

        pageSize = Math.Clamp(pageSize, 1, 500);
        page = Math.Max(page, 1);

        var query = await BuildFilteredQueryAsync(from, to, userId, categoryIds, kind, q, ct);

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

        return new TransactionListResponse(items, total, page, pageSize, new TotalsDto(income, expense, income - expense));
    }

    public async Task<TransactionDto> GetAsync(long id, CancellationToken ct)
    {
        var exists = await uow.Transactions.Query.AnyAsync(t => t.Id == id, ct);
        if (!exists)
        {
            throw ApiErrorException.NotFound("Операция не найдена");
        }

        return await GetDtoAsync(id, ct);
    }

    public async Task<SummaryResponse> SummaryAsync(DateOnly? from, DateOnly? to, string? groupBy, CancellationToken ct)
    {
        if (groupBy is not null
            && !string.Equals(groupBy, "category", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase))
        {
            throw ApiErrorException.BadRequest("Некорректное значение groupBy (допустимо: category, month)");
        }

        var group = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "category";
        var query = await BuildFilteredQueryAsync(from, to, null, null, null, null, ct);

        if (group == "month")
        {
            var monthRows = await query
                .GroupBy(t => new { Y = t.TransactionDate.Year, M = t.TransactionDate.Month })
                .OrderBy(g => g.Key.Y)
                .ThenBy(g => g.Key.M)
                .Select(g => new SummaryMonthDto(
                    $"{g.Key.Y:D4}-{g.Key.M:D2}",
                    g.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Amount),
                    g.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Amount)))
                .ToListAsync(ct);
            return new SummaryResponse(group, monthRows);
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
        var allCategories = await uow.Categories.Query.AsNoTracking().Include(c => c.Parent).ToListAsync(ct);
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
            .OrderBy(r => r.Expense).ThenBy(r => r.Income).ThenBy(r => r.Name)
            .ToList();

        return new SummaryResponse(group, rows);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request, int currentUserId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequestAsync(request, currentUserId, errors, ct);
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

        await uow.Transactions.AddAsync(transaction, ct);
        await uow.SaveChangesAsync(ct);

        return await GetDtoAsync(transaction.Id, ct);
    }

    public async Task<TransactionDto> UpdateAsync(long id, CreateTransactionRequest request, int currentUserId, CancellationToken ct)
    {
        var transaction = await uow.Transactions.Query.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Операция не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequestAsync(request, currentUserId, errors, ct);
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

        await uow.SaveChangesAsync(ct);
        return await GetDtoAsync(transaction.Id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var transaction = await uow.Transactions.Query.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (transaction is null)
        {
            throw ApiErrorException.NotFound("Операция не найдена");
        }

        uow.Transactions.Remove(transaction);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<IQueryable<Transaction>> BuildFilteredQueryAsync(
        DateOnly? from,
        DateOnly? to,
        int? userId,
        List<int>? categoryIds,
        TransactionKind? kind,
        string? search,
        CancellationToken ct)
    {
        var query = uow.Transactions.Query.AsNoTracking();
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
            var ids = categoryIds.Distinct().ToHashSet();
            var childIds = await uow.Categories.Query.AsNoTracking()
                .Where(c => c.ParentId != null && ids.Contains(c.ParentId.Value))
                .Select(c => c.Id)
                .ToListAsync(ct);
            foreach (var childId in childIds)
            {
                ids.Add(childId);
            }

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

    private async Task<ValidatedTransaction?> ValidateRequestAsync(
        CreateTransactionRequest request, int currentUserId, Dictionary<string, string> errors, CancellationToken ct)
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
            var category = await uow.Categories.Query.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, ct);
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

        var userId = currentUserId;
        if (request.UserId.HasValue)
        {
            var exists = await uow.Users.Query.AnyAsync(u => u.Id == request.UserId.Value, ct);
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
        var noteOk = true;
        if (note is not null && note.Length > Validation.MaxNoteLength)
        {
            errors["note"] = $"Комментарий не может превышать {Validation.MaxNoteLength} символов";
            note = null;
            noteOk = false;
        }

        if (kind is null || amount is null || date is null || categoryId is null || !noteOk)
        {
            return null;
        }

        return new ValidatedTransaction(kind.Value, amount.Value, date.Value, categoryId.Value, userId, note);
    }

    private async Task<TransactionDto> GetDtoAsync(long id, CancellationToken ct)
    {
        var t = await uow.Transactions.Query
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
