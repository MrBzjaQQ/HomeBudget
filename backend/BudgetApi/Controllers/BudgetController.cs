using Budget.Application.Common;
using Budget.Application.Budget;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Предоставляет HTTP-операции чтения и изменения планового бюджета.</summary>
/// <param name="budget">Сервис бизнес-операций с плановыми статьями бюджета.</param>
[ApiController]
[Route("api/budget")]
[Authorize]
public class BudgetController(IBudgetService budget) : ControllerBase
{
    /// <summary>Возвращает бюджет за заданный месяц.</summary>
    /// <param name="month">Месяц в формате, поддерживаемом сервисом, либо <see langword="null"/>.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Статьи и сводные данные бюджета за месяц.</returns>
    [HttpGet]
    public async Task<ActionResult<BudgetMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var result = await budget.GetMonthAsync(month, ct);
        return Ok(result);
    }

    /// <summary>Добавляет статью в плановый бюджет.</summary>
    /// <param name="request">Поля создаваемой статьи.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная статья с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetItemRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await budget.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет плановую статью бюджета.</summary>
    /// <param name="id">Идентификатор изменяемой статьи.</param>
    /// <param name="request">Новые значения статьи бюджета.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая статья бюджета.</returns>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CreateBudgetItemRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await budget.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Удаляет плановую статью бюджета.</summary>
    /// <param name="id">Идентификатор удаляемой статьи.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await budget.DeleteAsync(id, ct);
        return NoContent();
    }
}
