using Budget.Application.Common;
using Budget.Application.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController(IGoalsService goals) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GoalDto>>> List(CancellationToken ct)
    {
        var result = await goals.ListAsync(ct);
        return Ok(result);
    }

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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await goals.DeleteAsync(id, ct);
        return NoContent();
    }
}
