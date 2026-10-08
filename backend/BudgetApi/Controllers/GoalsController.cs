using Budget.Application.Common;
using Budget.Application.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Обрабатывает HTTP-запросы для финансовых целей пользователя.</summary>
/// <param name="goals">Сервис создания и управления финансовыми целями.</param>
[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController(IGoalsService goals) : ControllerBase
{
    /// <summary>Возвращает перечень финансовых целей.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список целей и их текущего состояния.</returns>
    [HttpGet]
    public async Task<ActionResult<List<GoalDto>>> List(CancellationToken ct)
    {
        var result = await goals.ListAsync(ct);
        return Ok(result);
    }

    /// <summary>Создаёт финансовую цель.</summary>
    /// <param name="request">Название, сумма, контейнер и срок цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная цель с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await goals.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет финансовую цель.</summary>
    /// <param name="id">Идентификатор изменяемой цели.</param>
    /// <param name="request">Новые значения цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая цель.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GoalDto>> Update(int id, [FromBody] UpdateGoalRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await goals.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Удаляет финансовую цель.</summary>
    /// <param name="id">Идентификатор удаляемой цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await goals.DeleteAsync(id, ct);
        return NoContent();
    }
}
