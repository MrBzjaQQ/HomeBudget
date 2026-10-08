using Budget.Application.Common;
using Budget.Application.Transactions;
using BudgetApi.Auth;
using BudgetApi.Common;
using Budget.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Обрабатывает HTTP-запросы для журнала финансовых операций.</summary>
/// <param name="transactions">Сервис прикладных операций с доходами и расходами.</param>
[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(ITransactionService transactions) : ControllerBase
{
    /// <summary>Возвращает страницу операций с заданными фильтрами и сортировкой.</summary>
    /// <param name="from">Необязательная нижняя граница даты включительно.</param>
    /// <param name="to">Необязательная верхняя граница даты включительно.</param>
    /// <param name="userId">Необязательный идентификатор пользователя для фильтрации.</param>
    /// <param name="categoryId">Необязательный список идентификаторов категорий.</param>
    /// <param name="kind">Необязательный тип операций: доход или расход.</param>
    /// <param name="q">Необязательная поисковая строка.</param>
    /// <param name="page">Номер страницы, начиная с единицы.</param>
    /// <param name="pageSize">Максимальное число элементов на странице.</param>
    /// <param name="sort">Необязательное поле сортировки.</param>
    /// <param name="dir">Необязательное направление сортировки.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Страница операций и сведения о пагинации.</returns>
    [HttpGet]
    public async Task<ActionResult<TransactionListResponse>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? userId,
        [FromQuery] List<int>? categoryId,
        [FromQuery] TransactionKind? kind,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken ct = default)
    {
        var result = await transactions.ListAsync(from, to, userId, categoryId, kind, q, page, pageSize, sort, dir, ct);
        return Ok(result);
    }

    /// <summary>Возвращает одну операцию по её идентификатору.</summary>
    /// <param name="id">Идентификатор операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Найденная операция.</returns>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransactionDto>> Get(long id, CancellationToken ct)
    {
        var result = await transactions.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Возвращает сводные суммы операций за указанный период.</summary>
    /// <param name="from">Необязательная начальная дата периода.</param>
    /// <param name="to">Необязательная конечная дата периода.</param>
    /// <param name="groupBy">Необязательный ключ группировки результатов.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Сводка операций с выбранной группировкой.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<SummaryResponse>> Summary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? groupBy,
        CancellationToken ct = default)
    {
        var result = await transactions.SummaryAsync(from, to, groupBy, ct);
        return Ok(result);
    }

    /// <summary>Создаёт операцию от имени текущего пользователя.</summary>
    /// <param name="request">Поля создаваемой операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная операция с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await transactions.CreateAsync(request, User.GetUserId(), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет существующую операцию от имени текущего пользователя.</summary>
    /// <param name="id">Идентификатор изменяемой операции.</param>
    /// <param name="request">Новые значения полей операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая операция.</returns>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await transactions.UpdateAsync(id, request, User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Удаляет операцию по идентификатору.</summary>
    /// <param name="id">Идентификатор удаляемой операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ с HTTP-статусом 204 после удаления.</returns>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await transactions.DeleteAsync(id, ct);
        return NoContent();
    }
}
