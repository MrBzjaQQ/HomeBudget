using Budget.Application.Common;
using Budget.Application.Infrastructure.Database.Contract;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budget.Application.Categories;

public sealed class CategoryService(IUnitOfWork uow) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryTreeDto>> ListAsync(TransactionKind? kind, bool includeArchived, CancellationToken ct)
    {
        var query = uow.Categories.Query.AsNoTracking().Include(c => c.Children).AsQueryable();
        if (kind.HasValue)
        {
            query = query.Where(c => c.Kind == kind.Value);
        }

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        var all = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync(ct);
        var groups = all.Where(c => c.ParentId is null).ToList();
        var childrenById = all.Where(c => c.ParentId is not null)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Category>)g.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList());

        return groups.Select(g => ToTreeDto(g, childrenById)).ToList();
    }

    public async Task<CategoryTreeDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = "Название не может быть пустым";
        }
        else if (name.Length > 100)
        {
            errors["name"] = "Название не длиннее 100 символов";
        }

        if (request.Kind is null || !Enum.IsDefined(request.Kind.Value))
        {
            errors["kind"] = "Укажите тип категории";
        }

        if (request.Color is not null && !Validation.IsValidHexColor(request.Color))
        {
            errors["color"] = "Цвет в формате #RRGGBB";
        }

        int? parentId = request.ParentId;
        TransactionKind? kind = request.Kind;
        if (parentId.HasValue)
        {
            var parent = await uow.Categories.Query.FirstOrDefaultAsync(c => c.Id == parentId.Value, ct);
            if (parent is null)
            {
                errors["parentId"] = "Родительская категория не найдена";
            }
            else if (parent.ParentId is not null)
            {
                errors["parentId"] = "Подкатегория может быть создана только внутри группы";
            }
            else
            {
                kind = parent.Kind;
                if (request.Kind is not null && request.Kind != parent.Kind)
                {
                    errors["kind"] = "Тип подкатегории наследуется от группы";
                }
            }
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        var exists = await uow.Categories.Query.AnyAsync(c => c.Name == name && c.Kind == kind && c.ParentId == parentId, ct);
        if (exists)
        {
            throw ApiErrorException.Conflict("Категория с таким именем уже есть в этом разделе");
        }

        var category = new Category
        {
            Name = name!,
            Kind = kind!.Value,
            ParentId = parentId,
            Color = request.Color,
            SortOrder = request.SortOrder ?? 0,
        };

        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        return await GetTreeDtoAsync(category.Id, ct);
    }

    public async Task<CategoryTreeDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await uow.Categories.Query.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Категория не найдена");

        var errors = new Dictionary<string, string>();
        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errors["name"] = "Название не может быть пустым";
            }
            else if (name.Length > 100)
            {
                errors["name"] = "Название не длиннее 100 символов";
            }
            else if (name != category.Name &&
                     await uow.Categories.Query.AnyAsync(c => c.Name == name && c.Kind == category.Kind && c.ParentId == category.ParentId && c.Id != id, ct))
            {
                errors["name"] = "Категория с таким именем уже есть в этом разделе";
            }
            else
            {
                category.Name = name;
            }
        }

        if (request.Color is not null)
        {
            if (request.Color.Length == 0)
            {
                category.Color = null;
            }
            else if (!Validation.IsValidHexColor(request.Color))
            {
                errors["color"] = "Цвет в формате #RRGGBB";
            }
            else
            {
                category.Color = request.Color;
            }
        }

        if (request.SortOrder.HasValue)
        {
            category.SortOrder = request.SortOrder.Value;
        }

        if (request.IsArchived.HasValue)
        {
            category.IsArchived = request.IsArchived.Value;
        }

        if (errors.Count > 0)
        {
            throw ApiErrorException.BadRequest("Некорректные данные", errors);
        }

        await uow.SaveChangesAsync(ct);
        return await GetTreeDtoAsync(category.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await uow.Categories.Query.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiErrorException.NotFound("Категория не найдена");

        if (category.Children.Any())
        {
            throw ApiErrorException.Conflict("Сначала удалите или перенесите подкатегории");
        }

        var hasBudgetItems = await uow.BudgetItems.Query.AnyAsync(b => b.CategoryId == id, ct);
        if (hasBudgetItems)
        {
            throw ApiErrorException.Conflict("В категории есть плановые бюджеты — сначала удалите их");
        }

        var hasTransactions = await uow.Transactions.Query.AnyAsync(t => t.CategoryId == id, ct);
        if (hasTransactions)
        {
            throw ApiErrorException.Conflict("В категории есть операции — архивируйте её вместо удаления");
        }

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync(ct);
    }

    private static CategoryTreeDto ToTreeDto(Category c, Dictionary<int, IReadOnlyList<Category>> childrenById)
        => new(
            c.Id, c.Name, c.Kind.ToString().ToUpperInvariant(), c.ParentId,
            c.Color, c.SortOrder, c.IsArchived,
            childrenById.TryGetValue(c.Id, out var children)
                ? children.Select(ch => new CategoryTreeDto(ch.Id, ch.Name, ch.Kind.ToString().ToUpperInvariant(), ch.ParentId, ch.Color, ch.SortOrder, ch.IsArchived, null)).ToList()
                : null);

    private async Task<CategoryTreeDto> GetTreeDtoAsync(int id, CancellationToken ct)
    {
        var category = await uow.Categories.Query
            .Include(c => c.Children)
            .Include(c => c.Parent)
            .FirstAsync(c => c.Id == id, ct);
        var children = category.Children
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(ch => new CategoryTreeDto(ch.Id, ch.Name, ch.Kind.ToString().ToUpperInvariant(), ch.ParentId, ch.Color, ch.SortOrder, ch.IsArchived, null))
            .ToList();
        return new CategoryTreeDto(
            category.Id, category.Name, category.Kind.ToString().ToUpperInvariant(), category.ParentId,
            category.Color, category.SortOrder, category.IsArchived, children);
    }
}
