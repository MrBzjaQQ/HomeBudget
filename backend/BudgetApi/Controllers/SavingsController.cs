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
[Route("api/savings")]
[Authorize]
public class SavingsController(BudgetDbContext db) : ControllerBase
{
    private const string AdminUsername = "admin";

    private const string MonthError = "Формат месяца: yyyy-MM";

    [HttpGet("containers")]
    public async Task<ActionResult<IReadOnlyList<SavingsContainerDto>>> ListContainers(CancellationToken ct)
    {
        var containers = await db.SavingsContainers.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SavingsContainerDto(c.Id, c.Name, c.Color, c.SortOrder, c.CreatedAt))
            .ToListAsync(ct);

        return Ok(containers);
    }

    [HttpPost("containers")]
    public async Task<ActionResult<SavingsContainerDto>> CreateContainer([FromBody] CreateSavingsContainerRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var name = request.Name?.Trim();
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название не может быть пустым";
        }
        else if (name.Length > 100)
        {
            errors["name"] = "Название не длиннее 100 символов";
        }

        if (request.Color is not null && !Validation.IsValidHexColor(request.Color))
        {
            errors["color"] = "Цвет в формате #RRGGBB";
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        if (await db.SavingsContainers.AnyAsync(c => c.Name == name, ct))
        {
            throw ApiErrorException.Conflict("Счёт с таким именем уже есть");
        }

        var container = new SavingsContainer
        {
            Name = name!,
            Color = request.Color,
            SortOrder = request.SortOrder ?? (await db.SavingsContainers.MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 1,
        };

        db.SavingsContainers.Add(container);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, await GetContainerDtoAsync(container.Id, ct));
    }

    [HttpPut("containers/{id:int}")]
    public async Task<ActionResult<SavingsContainerDto>> UpdateContainer(int id, [FromBody] UpdateSavingsContainerRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var container = await db.SavingsContainers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Счёт не найден");

        var errors = new Dictionary<string, string>();
        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errors["name"] = "Название не может быть пустым";
            }
            else if (name.Length > 100)
            {
                errors["name"] = "Название не длиннее 100 символов";
            }
            else if (name != container.Name &&
                     await db.SavingsContainers.AnyAsync(c => c.Name == name && c.Id != id, ct))
            {
                errors["name"] = "Счёт с таким именем уже есть";
            }
            else
            {
                container.Name = name;
            }
        }

        if (request.Color is not null)
        {
            if (request.Color.Length == 0)
            {
                container.Color = null;
            }
            else if (!Validation.IsValidHexColor(request.Color))
            {
                errors["color"] = "Цвет в формате #RRGGBB";
            }
            else
            {
                container.Color = request.Color;
            }
        }

        if (request.SortOrder.HasValue)
        {
            container.SortOrder = request.SortOrder.Value;
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        await db.SaveChangesAsync(ct);
        return Ok(await GetContainerDtoAsync(container.Id, ct));
    }

    [HttpDelete("containers/{id:int}")]
    public async Task<IActionResult> DeleteContainer(int id, CancellationToken ct)
    {
        var container = await db.SavingsContainers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Счёт не найден");

        if (await db.SavingsEntries.AnyAsync(x => x.ContainerId == id, ct))
        {
            throw ApiErrorException.Conflict("В счете есть записи накоплений — сначала удалите их");
        }

        db.SavingsContainers.Remove(container);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<SavingsMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var firstDay = ParseMonth(month);
        if (firstDay is null)
        {
            throw ApiErrorException.BadRequest(MonthError, new Dictionary<string, string> { ["month"] = MonthError });
        }

        var monthStart = firstDay.Value;
        var monthKey = monthStart.ToString("yyyy-MM");

        var entries = await db.SavingsEntries.AsNoTracking()
            .Include(x => x.Container)
            .Include(x => x.User)
            .Where(x => x.Month == monthStart)
            .ToListAsync(ct);

        var dtos = entries
            .OrderBy(x => x.User.DisplayName)
            .ThenBy(x => x.Container.SortOrder)
            .ThenBy(x => x.Container.Name)
            .ThenBy(x => x.Id)
            .Select(x => new SavingsEntryDto(
                x.Id, monthKey, x.UserId, x.User.DisplayName,
                x.ContainerId, x.Container.Name, x.Container.Color,
                x.Amount, x.Note, x.CreatedAt, x.UpdatedAt))
            .ToList();

        return Ok(new SavingsMonthResponse(monthKey, dtos, entries.Sum(x => x.Amount)));
    }

    [HttpPost]
    public async Task<ActionResult<SavingsEntryDto>> Create([FromBody] CreateSavingsEntryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var errors = new Dictionary<string, string>();
        var validated = await ValidateEntryFields(request.Month, request.UserId, request.ContainerId, request.Amount, request.Note, null, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        if (await db.SavingsEntries.AnyAsync(x => x.Month == validated.Month && x.ContainerId == validated.ContainerId && x.UserId == validated.UserId, ct))
        {
            throw ApiErrorException.Conflict("У этого человека по этому счёту на этот месяц уже есть запись");
        }

        var now = DateTime.UtcNow;
        var entry = new SavingsEntry
        {
            Month = validated.Month,
            UserId = validated.UserId,
            ContainerId = validated.ContainerId,
            Amount = validated.Amount,
            Note = string.IsNullOrWhiteSpace(validated.Note) ? null : validated.Note.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.SavingsEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, await GetEntryDtoAsync(entry.Id, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SavingsEntryDto>> Update(long id, [FromBody] UpdateSavingsEntryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var entry = await db.SavingsEntries.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Запись накоплений не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateEntryFields(request.Month, request.UserId, request.ContainerId, request.Amount, request.Note, entry, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        if (await db.SavingsEntries.AnyAsync(x => x.Id != id && x.Month == validated.Month && x.ContainerId == validated.ContainerId && x.UserId == validated.UserId, ct))
        {
            throw ApiErrorException.Conflict("У этого человека по этому счёту на этот месяц уже есть запись");
        }

        entry.Month = validated.Month;
        entry.UserId = validated.UserId;
        entry.ContainerId = validated.ContainerId;
        entry.Amount = validated.Amount;
        entry.Note = validated.Note;

        await db.SaveChangesAsync(ct);
        return Ok(await GetEntryDtoAsync(entry.Id, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var entry = await db.SavingsEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
        {
            throw ApiErrorException.NotFound("Запись накоплений не найдена");
        }

        db.SavingsEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SavingsSummary>> Summary([FromQuery] string? to, CancellationToken ct)
    {
        var today = Validation.Today();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var requestedEnd = currentMonth;
        if (!string.IsNullOrWhiteSpace(to))
        {
            requestedEnd = ParseMonth(to)
                ?? throw ApiErrorException.BadRequest(MonthError, new Dictionary<string, string> { ["to"] = MonthError });
        }

        var windowEnd = requestedEnd < currentMonth ? requestedEnd : currentMonth;
        var windowStart = windowEnd.AddMonths(-11);
        var beforeWindow = windowStart.AddMonths(-1);

        var entries = await db.SavingsEntries.AsNoTracking()
            .Where(x => x.Month >= windowStart && x.Month <= windowEnd && x.User.Username != AdminUsername)
            .Select(x => new { x.Month, x.UserId, UserName = x.User.DisplayName, x.Amount })
            .ToListAsync(ct);

        var amountsByMonth = entries
            .GroupBy(x => x.Month)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var beforeWindowTotal = await db.SavingsEntries.AsNoTracking()
            .Where(x => x.Month == beforeWindow && x.User.Username != AdminUsername)
            .Select(x => x.Amount)
            .SumAsync(ct);

        var months = new List<SavingsMonthAmount>(12);
        var deltas = new List<SavingsMonthDelta>(12);
        for (var m = 0; m < 12; m++)
        {
            var monthKey = windowStart.AddMonths(m);
            var amount = amountsByMonth.GetValueOrDefault(monthKey, 0m);
            var previousAmount = m == 0 ? beforeWindowTotal : amountsByMonth.GetValueOrDefault(monthKey.AddMonths(-1), 0m);
            months.Add(new SavingsMonthAmount(monthKey.ToString("yyyy-MM"), amount));
            deltas.Add(new SavingsMonthDelta(monthKey.ToString("yyyy-MM"), amount - previousAmount));
        }

        var byMember = entries
            .GroupBy(x => (x.UserId, x.UserName))
            .Select(g => new SavingsMemberTotal(g.Key.UserId, g.Key.UserName, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.UserName)
            .ToList();

        return Ok(new SavingsSummary(windowEnd.ToString("yyyy-MM"), months, deltas, byMember, byMember.Sum(x => x.Amount)));
    }

    private sealed record ValidatedSavingsEntry(DateOnly Month, int UserId, int ContainerId, decimal Amount, string? Note);

    private async Task<ValidatedSavingsEntry?> ValidateEntryFields(
        string? month, int? userId, int? containerId, decimal? amount, string? note,
        SavingsEntry? existing, Dictionary<string, string> errors, CancellationToken ct)
    {
        DateOnly? entryMonth = existing?.Month;
        if (month is not null)
        {
            var parsed = ParseMonth(month);
            if (parsed is null)
            {
                errors["month"] = MonthError;
            }
            else
            {
                entryMonth = parsed.Value;
            }
        }
        else if (existing is null)
        {
            errors["month"] = MonthError;
        }

        int? entryUserId = existing?.UserId;
        if (existing is null || userId.HasValue)
        {
            var target = await db.Users.FirstOrDefaultAsync(u => u.Id == (userId ?? User.GetUserId()), ct);
            if (target is null)
            {
                errors["userId"] = "Пользователь не найден";
            }
            else if (target.Username == AdminUsername)
            {
                errors["userId"] = "Админ не участвует в накоплениях";
            }
            else
            {
                entryUserId = target.Id;
            }
        }

        int? entryContainerId = existing?.ContainerId;
        if (containerId.HasValue)
        {
            var container = await db.SavingsContainers.FirstOrDefaultAsync(c => c.Id == containerId.Value, ct);
            if (container is null)
            {
                errors["containerId"] = "Счёт не найден";
            }
            else
            {
                entryContainerId = container.Id;
            }
        }
        else if (existing is null)
        {
            errors["containerId"] = "Счёт не найден";
        }

        decimal? entryAmount = existing?.Amount;
        if (amount.HasValue)
        {
            if (amount.Value <= 0)
            {
                errors["amount"] = "Сумма должна быть больше нуля";
            }
            else if (amount.Value > Validation.MaxAmount)
            {
                errors["amount"] = $"Сумма не должна превышать {Validation.MaxAmount:N2}";
            }
            else
            {
                entryAmount = amount.Value;
            }
        }
        else if (existing is null)
        {
            errors["amount"] = "Сумма должна быть больше нуля";
        }

        string? entryNote = existing?.Note;
        if (note is not null)
        {
            if (note.Length > Validation.MaxNoteLength)
            {
                errors["note"] = $"Комментарий не может превышать {Validation.MaxNoteLength} символов";
            }
            else
            {
                entryNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            }
        }

        if (entryMonth is null || entryUserId is null || entryContainerId is null || entryAmount is null || errors.Count > 0)
        {
            return null;
        }

        return new ValidatedSavingsEntry(entryMonth.Value, entryUserId.Value, entryContainerId.Value, entryAmount.Value, entryNote);
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

    private async Task<SavingsContainerDto> GetContainerDtoAsync(int id, CancellationToken ct)
    {
        var container = await db.SavingsContainers.FirstAsync(c => c.Id == id, ct);
        return new SavingsContainerDto(container.Id, container.Name, container.Color, container.SortOrder, container.CreatedAt);
    }

    private async Task<SavingsEntryDto> GetEntryDtoAsync(long id, CancellationToken ct)
    {
        var entry = await db.SavingsEntries
            .Include(x => x.Container)
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id, ct);
        return new SavingsEntryDto(
            entry.Id, entry.Month.ToString("yyyy-MM"),
            entry.UserId, entry.User.DisplayName,
            entry.ContainerId, entry.Container.Name, entry.Container.Color,
            entry.Amount, entry.Note, entry.CreatedAt, entry.UpdatedAt);
    }
}
