using Budget.Application.Common;
using Budget.Application.Categories;
using BudgetApi.Common;
using Budget.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Предоставляет HTTP-операции чтения и управления категориями.</summary>
/// <param name="categories">Сервис работы со справочником категорий.</param>
[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(ICategoryService categories) : ControllerBase
{
    /// <summary>Возвращает дерево категорий с необязательным фильтром по типу и архиву.</summary>
    /// <param name="kind">Необязательный тип операций для отбора категорий.</param>
    /// <param name="includeArchived">Включать ли архивированные категории в результат.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список корневых узлов дерева категорий.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryTreeDto>>> List(
        [FromQuery] TransactionKind? kind,
        [FromQuery] bool includeArchived,
        CancellationToken ct)
    {
        var result = await categories.ListAsync(kind, includeArchived, ct);
        return Ok(result);
    }

    /// <summary>Создаёт категорию и возвращает результат создания.</summary>
    /// <param name="request">Поля новой категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная категория с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await categories.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет свойства существующей категории.</summary>
    /// <param name="id">Идентификатор изменяемой категории.</param>
    /// <param name="request">Новые значения категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая категория.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await categories.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Удаляет категорию, если это допускают правила сервиса.</summary>
    /// <param name="id">Идентификатор удаляемой категории.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
