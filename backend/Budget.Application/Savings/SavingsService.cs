using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Savings;

/// <summary>Ведёт счета и ежемесячные записи семейных накоплений.</summary>
/// <param name="uow">Рабочая единица доступа к счетам, записям и пользователям.</param>
public sealed class SavingsService(IUnitOfWork uow) : ISavingsService
{
    /// <summary>Возвращает все счета накоплений в порядке отображения.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представления счетов накоплений.</returns>
    public async Task<IReadOnlyList<SavingsContainerDto>> ListContainersAsync(CancellationToken ct)
    {
        var containers = await uow.SavingsContainers.Query.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SavingsContainerDto(c.Id, c.Name, c.Color, c.SortOrder, c.CreatedAt))
            .ToListAsync(ct);

        return containers;
    }

    /// <summary>Проверяет и создаёт общий счёт накоплений.</summary>
    /// <param name="request">Название, цвет и порядок нового счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданное представление счёта.</returns>
    public async Task<SavingsContainerDto> CreateContainerAsync(CreateSavingsContainerRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название не может быть пустым";
        }
        else if (name.Length > Validation.MaxNameLength)
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

        if (await uow.SavingsContainers.Query.AnyAsync(c => c.Name == name, ct))
        {
            throw ApiErrorException.Conflict("Счёт с таким именем уже есть");
        }

        var container = new SavingsContainer
        {
            Name = name!,
            Color = request.Color,
            SortOrder = request.SortOrder ?? (await uow.SavingsContainers.Query.MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 1,
        };

        await uow.SavingsContainers.AddAsync(container, ct);
        await uow.SaveChangesAsync(ct);

        return await GetContainerDtoAsync(container.Id, ct);
    }

    /// <summary>Применяет изменения к счёту накоплений.</summary>
    /// <param name="id">Идентификатор счёта.</param>
    /// <param name="request">Изменённые свойства счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление счёта.</returns>
    public async Task<SavingsContainerDto> UpdateContainerAsync(int id, UpdateSavingsContainerRequest request, CancellationToken ct)
    {
        var container = await uow.SavingsContainers.Query.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Счёт не найден");

        var errors = new Dictionary<string, string>();
        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errors["name"] = "Название не может быть пустым";
            }
            else if (name.Length > Validation.MaxNameLength)
            {
                errors["name"] = "Название не длиннее 100 символов";
            }
            else if (name != container.Name &&
                     await uow.SavingsContainers.Query.AnyAsync(c => c.Name == name && c.Id != id, ct))
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

        await uow.SaveChangesAsync(ct);
        return await GetContainerDtoAsync(container.Id, ct);
    }

    /// <summary>Удаляет пустой счёт, не связанный с записями или целями.</summary>
    /// <param name="id">Идентификатор счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    public async Task DeleteContainerAsync(int id, CancellationToken ct)
    {
        var container = await uow.SavingsContainers.Query.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Счёт не найден");

        if (await uow.SavingsEntries.Query.AnyAsync(x => x.ContainerId == id, ct))
        {
            throw ApiErrorException.Conflict("В счете есть записи накоплений — сначала удалите их");
        }

        if (await uow.Goals.Query.AnyAsync(x => x.ContainerId == id, ct))
        {
            throw ApiErrorException.Conflict("По счёту есть финансовые цели — сначала удалите их");
        }

        uow.SavingsContainers.Remove(container);
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>Возвращает записи и сумму накоплений за выбранный месяц.</summary>
    /// <param name="month">Месяц в формате yyyy-MM.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Ответ со списком записей месяца и их общей суммой.</returns>
    public async Task<SavingsMonthResponse> GetMonthAsync(string? month, CancellationToken ct)
    {
        var firstDay = ParseMonth(month);
        if (firstDay is null)
        {
            throw ApiErrorException.BadRequest(DateValidationMessages.MonthFormat, new Dictionary<string, string> { ["month"] = DateValidationMessages.MonthFormat });
        }

        var monthStart = firstDay.Value;
        var monthKey = monthStart.ToString(DateFormats.Month);

        var entries = await uow.SavingsEntries.Query.AsNoTracking()
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

        return new SavingsMonthResponse(monthKey, dtos, entries.Sum(x => x.Amount));
    }

    /// <summary>Проверяет и создаёт ежемесячную запись накоплений участника.</summary>
    /// <param name="request">Месяц, участник, счёт, сумма и комментарий.</param>
    /// <param name="currentUserId">Идентификатор пользователя, выполняющего запрос.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной записи.</returns>
    public async Task<SavingsEntryDto> CreateEntryAsync(CreateSavingsEntryRequest request, int currentUserId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();
        var validated = await ValidateEntryFieldsAsync(request.Month, request.UserId, request.ContainerId, request.Amount, request.Note, currentUserId, null, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        if (await uow.SavingsEntries.Query.AnyAsync(x => x.Month == validated.Month && x.ContainerId == validated.ContainerId && x.UserId == validated.UserId, ct))
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

        await uow.SavingsEntries.AddAsync(entry, ct);
        await uow.SaveChangesAsync(ct);

        return await GetEntryDtoAsync(entry.Id, ct);
    }

    /// <summary>Проверяет и обновляет запись накоплений.</summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="request">Поля записи, которые следует изменить.</param>
    /// <param name="currentUserId">Идентификатор пользователя, выполняющего запрос.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление записи.</returns>
    public async Task<SavingsEntryDto> UpdateEntryAsync(long id, UpdateSavingsEntryRequest request, int currentUserId, CancellationToken ct)
    {
        var entry = await uow.SavingsEntries.Query.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Запись накоплений не найдена");

        var errors = new Dictionary<string, string>();
        var validated = await ValidateEntryFieldsAsync(request.Month, request.UserId, request.ContainerId, request.Amount, request.Note, currentUserId, entry, errors, ct);
        if (validated is null)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        if (await uow.SavingsEntries.Query.AnyAsync(x => x.Id != id && x.Month == validated.Month && x.ContainerId == validated.ContainerId && x.UserId == validated.UserId, ct))
        {
            throw ApiErrorException.Conflict("У этого человека по этому счёту на этот месяц уже есть запись");
        }

        entry.Month = validated.Month;
        entry.UserId = validated.UserId;
        entry.ContainerId = validated.ContainerId;
        entry.Amount = validated.Amount;
        entry.Note = validated.Note;

        await uow.SaveChangesAsync(ct);
        return await GetEntryDtoAsync(entry.Id, ct);
    }

    /// <summary>Удаляет запись накоплений по её идентификатору.</summary>
    /// <param name="id">Идентификатор удаляемой записи.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    public async Task DeleteEntryAsync(long id, CancellationToken ct)
    {
        var entry = await uow.SavingsEntries.Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
        {
            throw ApiErrorException.NotFound("Запись накоплений не найдена");
        }

        uow.SavingsEntries.Remove(entry);
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>Рассчитывает динамику накоплений за последовательное окно месяцев.</summary>
    /// <param name="to">Последний месяц сводки в формате yyyy-MM; по умолчанию текущий месяц.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Сводка по месяцам и участникам без системного администратора.</returns>
    public async Task<SavingsSummary> SummaryAsync(string? to, CancellationToken ct)
    {
        var today = Validation.Today();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var requestedEnd = currentMonth;
        if (!string.IsNullOrWhiteSpace(to))
        {
            requestedEnd = ParseMonth(to)
                ?? throw ApiErrorException.BadRequest(DateValidationMessages.MonthFormat, new Dictionary<string, string> { ["to"] = DateValidationMessages.MonthFormat });
        }

        var windowEnd = requestedEnd < currentMonth ? requestedEnd : currentMonth;
        var windowStart = windowEnd.AddMonths(-(SavingsConstants.SummaryMonthCount - 1));
        var beforeWindow = windowStart.AddMonths(-1);

        var entries = await uow.SavingsEntries.Query.AsNoTracking()
            .Where(x => x.Month >= windowStart && x.Month <= windowEnd && x.User.Username != SystemUsernames.Admin)
            .Select(x => new { x.Month, x.UserId, UserName = x.User.DisplayName, x.Amount })
            .ToListAsync(ct);

        var amountsByMonth = entries
            .GroupBy(x => x.Month)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var beforeWindowTotal = await uow.SavingsEntries.Query.AsNoTracking()
            .Where(x => x.Month == beforeWindow && x.User.Username != SystemUsernames.Admin)
            .Select(x => x.Amount)
            .SumAsync(ct);

        var months = new List<SavingsMonthAmount>(SavingsConstants.SummaryMonthCount);
        var deltas = new List<SavingsMonthDelta>(SavingsConstants.SummaryMonthCount);
        for (var m = 0; m < SavingsConstants.SummaryMonthCount; m++)
        {
            var monthKey = windowStart.AddMonths(m);
            var amount = amountsByMonth.GetValueOrDefault(monthKey, 0m);
            var previousAmount = m == 0 ? beforeWindowTotal : amountsByMonth.GetValueOrDefault(monthKey.AddMonths(-1), 0m);
            months.Add(new SavingsMonthAmount(monthKey.ToString(DateFormats.Month), amount));
            deltas.Add(new SavingsMonthDelta(monthKey.ToString(DateFormats.Month), amount - previousAmount));
        }

        var byMember = entries
            .GroupBy(x => (x.UserId, x.UserName))
            .Select(g => new SavingsMemberTotal(g.Key.UserId, g.Key.UserName, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.UserName)
            .ToList();

        return new SavingsSummary(windowEnd.ToString(DateFormats.Month), months, deltas, byMember, byMember.Sum(x => x.Amount));
    }

    private sealed record ValidatedSavingsEntry(DateOnly Month, int UserId, int ContainerId, decimal Amount, string? Note);

    private async Task<ValidatedSavingsEntry?> ValidateEntryFieldsAsync(
        string? month, int? userId, int? containerId, decimal? amount, string? note,
        int currentUserId, SavingsEntry? existing, Dictionary<string, string> errors, CancellationToken ct)
    {
        DateOnly? entryMonth = existing?.Month;
        if (month is not null)
        {
            var parsed = ParseMonth(month);
            if (parsed is null)
            {
                errors["month"] = DateValidationMessages.MonthFormat;
            }
            else
            {
                entryMonth = parsed.Value;
            }
        }
        else if (existing is null)
        {
            errors["month"] = DateValidationMessages.MonthFormat;
        }

        int? entryUserId = existing?.UserId;
        if (existing is null || userId.HasValue)
        {
            var target = await uow.Users.Query.FirstOrDefaultAsync(u => u.Id == (userId ?? currentUserId), ct);
            if (target is null)
            {
                errors["userId"] = "Пользователь не найден";
            }
            else if (target.Username == SystemUsernames.Admin)
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
            var container = await uow.SavingsContainers.Query.FirstOrDefaultAsync(c => c.Id == containerId.Value, ct);
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
        var container = await uow.SavingsContainers.Query.FirstAsync(c => c.Id == id, ct);
        return new SavingsContainerDto(container.Id, container.Name, container.Color, container.SortOrder, container.CreatedAt);
    }

    private async Task<SavingsEntryDto> GetEntryDtoAsync(long id, CancellationToken ct)
    {
        var entry = await uow.SavingsEntries.Query
            .Include(x => x.Container)
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id, ct);
        return new SavingsEntryDto(
            entry.Id, entry.Month.ToString(DateFormats.Month),
            entry.UserId, entry.User.DisplayName,
            entry.ContainerId, entry.Container.Name, entry.Container.Color,
            entry.Amount, entry.Note, entry.CreatedAt, entry.UpdatedAt);
    }
}
