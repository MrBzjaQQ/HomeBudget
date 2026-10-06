namespace Budget.Domain.Entities;

public class FinancialGoal
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public int ContainerId { get; set; }

    public SavingsContainer Container { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
