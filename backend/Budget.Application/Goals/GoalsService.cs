using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Goals;

public sealed class GoalsService(IUnitOfWork uow) : IGoalsService
{
    private const string DueDateFormat = "yyyy-MM-dd";
    private const string DueDateError = "Формат даты: yyyy-MM-dd";

    public async Task<List<GoalDto>> ListAsync(CancellationToken ct)
    {
        var goals = await uow.Goals.Query.AsNoTracking()
            .Include(g => g.Container)
            .OrderBy(g => g.CreatedAt)
            .ThenBy(g => g.Id)
            .ToListAsync(ct);

        return await BuildDtosAsync(goals, ct);
    }

    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название не может быть пустым";
        }
        else if (name.Length > 100)
        {
            errors["name"] = "Название не длиннее 100 символов";
        }

        if (request.TargetAmount is null || request.TargetAmount <= 0)
        {
            errors["targetAmount"] = "Целевая сумма должна быть больше нуля";
        }
        else if (request.TargetAmount > Validation.MaxAmount)
        {
            errors["targetAmount"] = $"Сумма не должна превышать {Validation.MaxAmount:N2}";
        }

        int? containerId = null;
        if (request.ContainerId is null)
        {
            errors["containerId"] = "Счёт не найден";
        }
        else
        {
            var container = await uow.SavingsContainers.Query.FirstOrDefaultAsync(c => c.Id == request.ContainerId.Value, ct);
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
        if (request.DueDate is not null)
        {
            if (DateOnly.TryParseExact(request.DueDate, DueDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDueDate))
            {
                dueDate = parsedDueDate;
            }
            else
            {
                errors["dueDate"] = DueDateError;
            }
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        var goal = new FinancialGoal
        {
            Name = name!,
            TargetAmount = request.TargetAmount!.Value,
            ContainerId = containerId!.Value,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow,
        };

        await uow.Goals.AddAsync(goal, ct);
        await uow.SaveChangesAsync(ct);

        var created = await uow.Goals.Query.AsNoTracking()
            .Include(g => g.Container)
            .FirstAsync(g => g.Id == goal.Id, ct);

        return (await BuildDtosAsync([created], ct)).Single();
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var goal = await uow.Goals.Query.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Цель не найдена");

        uow.Goals.Remove(goal);
        await uow.SaveChangesAsync(ct);
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
                    g.DueDate?.ToString(DueDateFormat),
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
