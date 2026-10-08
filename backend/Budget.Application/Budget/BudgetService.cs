using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Budget;

/// <summary>Формирует месячный план расходов и управляет его позициями.</summary>
/// <param name="uow">Рабочая единица доступа к планам, операциям и категориям.</param>
public sealed class BudgetService(IUnitOfWork uow) : IBudgetService
{
    /// <summary>Сопоставляет бюджетный план с фактическими расходами за месяц.</summary>
    /// <param name="month">Месяц в формате yyyy-MM.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Позиции плана, суммы по категориям и общие итоги.</returns>
    public async Task<BudgetMonthResponse> GetMonthAsync(string? month, CancellationToken ct)
    {
        var firstDay = ParseMonth(month);
        if (firstDay is null)
        {
            throw ApiErrorException.BadRequest(DateValidationMessages.MonthFormat, new Dictionary<string, string> { ["month"] = DateValidationMessages.MonthFormat });
        }

        var monthStart = firstDay.Value;
        var monthEnd = monthStart.AddMonths(1);
        var monthKey = monthStart.ToString(DateFormats.Month);

        var items = await uow.BudgetItems.Query.AsNoTracking()
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

        var plannedByCategory = await uow.BudgetItems.Query.AsNoTracking()
            .Where(x => x.Month == monthStart)
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Planned = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var actualByCategory = await uow.Transactions.Query.AsNoTracking()
            .Where(t => t.Kind == TransactionKind.Expense && t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Actual = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var plannedById = plannedByCategory.ToDictionary(x => x.CategoryId, x => x.Planned);
        var actualById = actualByCategory.ToDictionary(x => x.CategoryId, x => x.Actual);
        var categoryIds = plannedById.Keys.Concat(actualById.Keys).Distinct().ToHashSet();

        var categories = await uow.Categories.Query.AsNoTracking()
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

        return new BudgetMonthResponse(monthKey, items, rows, totals);
    }

    /// <summary>Проверяет и сохраняет новую плановую позицию.</summary>
    /// <param name="request">Месяц, категория, сумма и комментарий.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной позиции.</returns>
    public async Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequestAsync(request, errors, ct);
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

        await uow.BudgetItems.AddAsync(item, ct);
        await uow.SaveChangesAsync(ct);

        return await GetDtoAsync(item.Id, ct);
    }

    /// <summary>Проверяет и обновляет существующую плановую позицию.</summary>
    /// <param name="id">Идентификатор изменяемой позиции.</param>
    /// <param name="request">Новые значения позиции бюджета.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление позиции.</returns>
    public async Task<BudgetItemDto> UpdateAsync(long id, CreateBudgetItemRequest request, CancellationToken ct)
    {
        var item = await uow.BudgetItems.Query.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Позиция бюджета не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateRequestAsync(request, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        item.Month = validated.Month;
        item.CategoryId = validated.CategoryId;
        item.Amount = validated.Amount;
        item.Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim();

        await uow.SaveChangesAsync(ct);
        return await GetDtoAsync(item.Id, ct);
    }

    /// <summary>Удаляет плановую позицию по её идентификатору.</summary>
    /// <param name="id">Идентификатор удаляемой позиции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var item = await uow.BudgetItems.Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            throw ApiErrorException.NotFound("Позиция бюджета не найдена");
        }

        uow.BudgetItems.Remove(item);
        await uow.SaveChangesAsync(ct);
    }

    private sealed record ValidatedBudgetItem(DateOnly Month, int CategoryId, decimal Amount, string? Note);

    private async Task<ValidatedBudgetItem?> ValidateRequestAsync(
        CreateBudgetItemRequest request, Dictionary<string, string> errors, CancellationToken ct)
    {
        DateOnly? month = null;
        if (string.IsNullOrWhiteSpace(request.Month))
        {
            errors["month"] = DateValidationMessages.MonthFormat;
        }
        else
        {
            var parsed = ParseMonth(request.Month);
            if (parsed is null)
            {
                errors["month"] = DateValidationMessages.MonthFormat;
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
            var category = await uow.Categories.Query.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, ct);
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
        var item = await uow.BudgetItems.Query
            .Include(x => x.Category)
            .ThenInclude(c => c!.Parent)
            .FirstAsync(x => x.Id == id, ct);
        return new BudgetItemDto(
            item.Id, item.Month.ToString(DateFormats.Month), item.CategoryId,
            item.Category.Name, item.Category.Parent?.Name, item.Category.Color,
            item.Amount, item.Note, item.CreatedAt, item.UpdatedAt);
    }
}
