using Budget.Application.Common;
using Budget.Application.Savings;
using BudgetApi.Auth;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Предоставляет HTTP-операции со счетами, движениями и сводкой накоплений.</summary>
/// <param name="savings">Сервис управления накоплениями.</param>
[ApiController]
[Route("api/savings")]
[Authorize]
public class SavingsController(ISavingsService savings) : ControllerBase
{
    /// <summary>Возвращает список контейнеров накоплений.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Контейнеры накоплений, доступные в бюджете.</returns>
    [HttpGet("containers")]
    public async Task<ActionResult<IReadOnlyList<SavingsContainerDto>>> ListContainers(CancellationToken ct)
    {
        var result = await savings.ListContainersAsync(ct);
        return Ok(result);
    }

    /// <summary>Создаёт контейнер накоплений.</summary>
    /// <param name="request">Название, цвет и порядок нового контейнера.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданный контейнер с HTTP-статусом 201.</returns>
    [HttpPost("containers")]
    public async Task<IActionResult> CreateContainer([FromBody] CreateSavingsContainerRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await savings.CreateContainerAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет свойства контейнера накоплений.</summary>
    /// <param name="id">Идентификатор изменяемого контейнера.</param>
    /// <param name="request">Новые значения контейнера.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённый контейнер.</returns>
    [HttpPut("containers/{id:int}")]
    public async Task<IActionResult> UpdateContainer(int id, [FromBody] UpdateSavingsContainerRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await savings.UpdateContainerAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Удаляет контейнер накоплений, если его можно удалить.</summary>
    /// <param name="id">Идентификатор удаляемого контейнера.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("containers/{id:int}")]
    public async Task<IActionResult> DeleteContainer(int id, CancellationToken ct)
    {
        await savings.DeleteContainerAsync(id, ct);
        return NoContent();
    }

    /// <summary>Возвращает операции накоплений за выбранный месяц.</summary>
    /// <param name="month">Месяц в формате, поддерживаемом сервисом, либо <see langword="null"/>.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Операции и сводные данные накоплений за месяц.</returns>
    [HttpGet]
    public async Task<ActionResult<SavingsMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var result = await savings.GetMonthAsync(month, ct);
        return Ok(result);
    }

    /// <summary>Создаёт движение средств от имени текущего пользователя.</summary>
    /// <param name="request">Сумма, дата, контейнер и другие данные движения.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная запись с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavingsEntryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await savings.CreateEntryAsync(request, User.GetUserId(), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет движение средств от имени текущего пользователя.</summary>
    /// <param name="id">Идентификатор изменяемого движения.</param>
    /// <param name="request">Новые значения полей движения.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая запись движения.</returns>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSavingsEntryRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await savings.UpdateEntryAsync(id, request, User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Удаляет запись движения средств.</summary>
    /// <param name="id">Идентификатор удаляемого движения.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await savings.DeleteEntryAsync(id, ct);
        return NoContent();
    }

    /// <summary>Возвращает сводку накоплений на указанную дату.</summary>
    /// <param name="to">Необязательная дата, на которую рассчитывается сводка.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Сводные остатки и показатели накоплений.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<SavingsSummary>> Summary([FromQuery] string? to, CancellationToken ct)
    {
        var result = await savings.SummaryAsync(to, ct);
        return Ok(result);
    }
}
