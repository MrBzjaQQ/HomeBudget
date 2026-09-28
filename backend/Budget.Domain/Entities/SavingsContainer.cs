namespace Budget.Domain.Entities;

public class SavingsContainer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<SavingsEntry> Entries { get; set; } = new List<SavingsEntry>();
}
