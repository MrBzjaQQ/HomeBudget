using Budget.Application.Common;
using Budget.Application.Budget;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/budget")]
[Authorize]
public class BudgetController(IBudgetService budget) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BudgetMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var result = await budget.GetMonthAsync(month, ct);
        return Ok(result);
    }

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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await budget.DeleteAsync(id, ct);
        return NoContent();
    }
}
