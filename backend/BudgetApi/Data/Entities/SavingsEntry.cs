namespace BudgetApi.Data.Entities;

public class SavingsEntry
{
    public long Id { get; set; }

    /// <summary>Always the first day of the month.</summary>
    public DateOnly Month { get; set; }

    public int ContainerId { get; set; }

    public SavingsContainer Container { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
