using Budget.Application.Common;
using Budget.Application.Transactions;
using BudgetApi.Auth;
using BudgetApi.Common;
using Budget.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(ITransactionService transactions) : ControllerBase
{
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

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransactionDto>> Get(long id, CancellationToken ct)
    {
        var result = await transactions.GetAsync(id, ct);
        return Ok(result);
    }

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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await transactions.DeleteAsync(id, ct);
        return NoContent();
    }
}
