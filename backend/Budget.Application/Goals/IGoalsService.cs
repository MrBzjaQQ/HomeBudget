namespace Budget.Application.Goals;

public interface IGoalsService
{
    Task<List<GoalDto>> ListAsync(CancellationToken ct);

    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}
