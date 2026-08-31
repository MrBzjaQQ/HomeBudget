namespace BudgetApi.Data.Entities;

public class Transaction
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public TransactionKind Kind { get; set; }

    /// <summary>Always positive; the sign is derived from <see cref="Kind"/>.</summary>
    public decimal Amount { get; set; }

    public DateOnly TransactionDate { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
