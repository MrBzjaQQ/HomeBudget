using Budget.Domain.Entities;

namespace Budget.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryTreeDto>> ListAsync(TransactionKind? kind, bool includeArchived, CancellationToken ct);

    Task<CategoryTreeDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct);

    Task<CategoryTreeDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}
