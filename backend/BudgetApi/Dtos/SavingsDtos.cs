namespace BudgetApi.Dtos;

public record CreateSavingsContainerRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}

public record UpdateSavingsContainerRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}

public record SavingsContainerDto(
    int Id,
    string Name,
    string? Color,
    int SortOrder,
    DateTime CreatedAt);

public record CreateSavingsEntryRequest
{
    public string? Month { get; set; }
    public int? UserId { get; set; }
    public int? ContainerId { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public record UpdateSavingsEntryRequest
{
    public string? Month { get; set; }
    public int? UserId { get; set; }
    public int? ContainerId { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public record SavingsEntryDto(
    long Id,
    string Month,
    int UserId,
    string UserName,
    int ContainerId,
    string ContainerName,
    string? Color,
    decimal Amount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SavingsMonthResponse(
    string Month,
    IReadOnlyList<SavingsEntryDto> Entries,
    decimal Total);

public record SavingsMonthAmount(string Month, decimal Amount);

public record SavingsMonthDelta(string Month, decimal Delta);

public record SavingsMemberTotal(int UserId, string UserName, decimal Amount);

public record SavingsYearSummary(
    int Year,
    IReadOnlyList<SavingsMonthAmount> Months,
    IReadOnlyList<SavingsMonthDelta> Deltas,
    IReadOnlyList<SavingsMemberTotal> ByMember,
    decimal Total);
