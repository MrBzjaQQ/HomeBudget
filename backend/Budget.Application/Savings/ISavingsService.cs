namespace Budget.Application.Savings;

public interface ISavingsService
{
    Task<IReadOnlyList<SavingsContainerDto>> ListContainersAsync(CancellationToken ct);

    Task<SavingsContainerDto> CreateContainerAsync(CreateSavingsContainerRequest request, CancellationToken ct);

    Task<SavingsContainerDto> UpdateContainerAsync(int id, UpdateSavingsContainerRequest request, CancellationToken ct);

    Task DeleteContainerAsync(int id, CancellationToken ct);

    Task<SavingsMonthResponse> GetMonthAsync(string? month, CancellationToken ct);

    Task<SavingsEntryDto> CreateEntryAsync(CreateSavingsEntryRequest request, int currentUserId, CancellationToken ct);

    Task<SavingsEntryDto> UpdateEntryAsync(long id, UpdateSavingsEntryRequest request, int currentUserId, CancellationToken ct);

    Task DeleteEntryAsync(long id, CancellationToken ct);

    Task<SavingsSummary> SummaryAsync(string? to, CancellationToken ct);
}
