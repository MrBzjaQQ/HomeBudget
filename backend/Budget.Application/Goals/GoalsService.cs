using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Goals;

/// <summary>Ведёт цели накоплений и рассчитывает текущий прогресс по связанным счетам.</summary>
/// <param name="uow">Рабочая единица доступа к целям, счетам и накоплениям.</param>
public sealed class GoalsService(IUnitOfWork uow) : IGoalsService
{
    /// <summary>Загружает финансовые цели и строит их представления с прогрессом.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список целей с текущей суммой накоплений.</returns>
    public async Task<List<GoalDto>> ListAsync(CancellationToken ct)
    {
        var goals = await uow.Goals.Query.AsNoTracking()
            .Include(g => g.Container)
            .OrderBy(g => g.CreatedAt)
            .ThenBy(g => g.Id)
            .ToListAsync(ct);

        return await BuildDtosAsync(goals, ct);
    }

    /// <summary>Проверяет данные и создаёт финансовую цель.</summary>
    /// <param name="request">Поля создаваемой цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная цель с рассчитанным прогрессом.</returns>
    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct)
    {
        var validated = await ValidateRequestAsync(request.Name, request.TargetAmount, request.ContainerId, request.DueDate, ct);

        var goal = new FinancialGoal
        {
            Name = validated.Name,
            TargetAmount = validated.TargetAmount,
            ContainerId = validated.ContainerId,
            DueDate = validated.DueDate,
            CreatedAt = DateTime.UtcNow,
        };

        await uow.Goals.AddAsync(goal, ct);
        await uow.SaveChangesAsync(ct);

        var created = await uow.Goals.Query.AsNoTracking()
            .Include(g => g.Container)
            .FirstAsync(g => g.Id == goal.Id, ct);

        return (await BuildDtosAsync([created], ct)).Single();
    }

    /// <summary>Проверяет и сохраняет новые значения финансовой цели.</summary>
    /// <param name="id">Идентификатор изменяемой цели.</param>
    /// <param name="request">Новые значения цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая цель с рассчитанным прогрессом.</returns>
    public async Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken ct)
    {
        var goal = await uow.Goals.Query.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Цель не найдена");

        var validated = await ValidateRequestAsync(request.Name, request.TargetAmount, request.ContainerId, request.DueDate, ct);

        goal.Name = validated.Name;
        goal.TargetAmount = validated.TargetAmount;
        goal.ContainerId = validated.ContainerId;
        goal.DueDate = validated.DueDate;

        uow.Goals.Update(goal);
        await uow.SaveChangesAsync(ct);

        var updated = await uow.Goals.Query.AsNoTracking()
            .Include(g => g.Container)
            .FirstAsync(g => g.Id == goal.Id, ct);

        return (await BuildDtosAsync([updated], ct)).Single();
    }

    /// <summary>Удаляет финансовую цель.</summary>
    /// <param name="id">Идентификатор удаляемой цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var goal = await uow.Goals.Query.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Цель не найдена");

        uow.Goals.Remove(goal);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<(string Name, decimal TargetAmount, int ContainerId, DateOnly? DueDate)> ValidateRequestAsync(
        string? requestName,
        decimal? requestTargetAmount,
        int? requestContainerId,
        string? requestDueDate,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        var name = requestName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название не может быть пустым";
        }
        else if (name.Length > Validation.MaxNameLength)
        {
            errors["name"] = "Название не длиннее 100 символов";
        }

        if (requestTargetAmount is null || requestTargetAmount <= 0)
        {
            errors["targetAmount"] = "Целевая сумма должна быть больше нуля";
        }
        else if (requestTargetAmount > Validation.MaxAmount)
        {
            errors["targetAmount"] = $"Сумма не должна превышать {Validation.MaxAmount:N2}";
        }

        int? containerId = null;
        if (requestContainerId is null)
        {
            errors["containerId"] = "Счёт не найден";
        }
        else
        {
            var container = await uow.SavingsContainers.Query.FirstOrDefaultAsync(c => c.Id == requestContainerId.Value, ct);
            if (container is null)
            {
                errors["containerId"] = "Счёт не найден";
            }
            else
            {
                containerId = container.Id;
            }
        }

        DateOnly? dueDate = null;
        if (requestDueDate is not null)
        {
            if (DateOnly.TryParseExact(requestDueDate, DateFormats.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDueDate))
            {
                dueDate = parsedDueDate;
            }
            else
            {
                errors["dueDate"] = DateValidationMessages.DueDateFormat;
            }
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        return (name!, requestTargetAmount!.Value, containerId!.Value, dueDate);
    }

    private async Task<List<GoalDto>> BuildDtosAsync(List<FinancialGoal> goals, CancellationToken ct)
    {
        var today = Validation.Today();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var containerIds = goals.Select(g => g.ContainerId).Distinct().ToList();
        var entries = await uow.SavingsEntries.Query.AsNoTracking()
            .Where(x => containerIds.Contains(x.ContainerId) && x.Month <= currentMonth)
            .Select(x => new { x.ContainerId, x.Month, x.Amount })
            .ToListAsync(ct);

        var savedByContainer = entries
            .GroupBy(x => x.ContainerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var lastMonth = g.Max(x => x.Month);
                    return g.Sum(x => x.Month == lastMonth ? x.Amount : 0m);
                });

        return goals
            .Select(g =>
            {
                var saved = savedByContainer.GetValueOrDefault(g.ContainerId, 0m);
                var achieved = saved >= g.TargetAmount;
                return new GoalDto(
                    g.Id,
                    g.Name,
                    g.TargetAmount,
                    g.ContainerId,
                    g.Container.Name,
                    g.Container.Color,
                    g.DueDate?.ToString(DateFormats.Date),
                    saved,
                    ComputeMonthlyRequired(achieved, g.DueDate, g.TargetAmount, saved),
                    achieved,
                    g.CreatedAt);
            })
            .ToList();
    }

    private static decimal? ComputeMonthlyRequired(bool achieved, DateOnly? dueDate, decimal targetAmount, decimal saved)
    {
        if (achieved || dueDate is null)
        {
            return null;
        }

        var today = Validation.Today();
        if (dueDate < today)
        {
            return null;
        }

        var months = (dueDate.Value.Year - today.Year) * 12 + (dueDate.Value.Month - today.Month) + 1;
        return (decimal)Math.Ceiling((targetAmount - saved) / months);
    }
}
