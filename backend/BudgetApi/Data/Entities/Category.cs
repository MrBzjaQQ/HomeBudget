namespace BudgetApi.Data.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Set on the group (top level) and inherited by children.</summary>
    public TransactionKind Kind { get; set; }

    public int? ParentId { get; set; }

    public Category? Parent { get; set; }

    /// <summary>Hex color, e.g. "#E5484D".</summary>
    public string? Color { get; set; }

    public int SortOrder { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
