namespace Budget.Application.Goals;

public record CreateGoalRequest
{
    public string? Name { get; set; }
    public decimal? TargetAmount { get; set; }
    public int? ContainerId { get; set; }
    public string? DueDate { get; set; }
}

public record GoalDto(
    int Id,
    string Name,
    decimal TargetAmount,
    int ContainerId,
    string ContainerName,
    string? ContainerColor,
    string? DueDate,
    decimal Saved,
    decimal? MonthlyRequired,
    bool Achieved,
    DateTime CreatedAt);
