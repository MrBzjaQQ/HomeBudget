namespace BudgetApi.Data.Entities;

public class BudgetItem
{
    public long Id { get; set; }

    /// <summary>Always the first day of the month.</summary>
    public DateOnly Month { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
