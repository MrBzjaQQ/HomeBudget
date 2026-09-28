using Budget.Application.Common;
using Budget.Application.Savings;
using BudgetApi.Auth;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

[ApiController]
[Route("api/savings")]
[Authorize]
public class SavingsController(ISavingsService savings) : ControllerBase
{
    [HttpGet("containers")]
    public async Task<ActionResult<IReadOnlyList<SavingsContainerDto>>> ListContainers(CancellationToken ct)
    {
        var result = await savings.ListContainersAsync(ct);
        return Ok(result);
    }

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

    [HttpDelete("containers/{id:int}")]
    public async Task<IActionResult> DeleteContainer(int id, CancellationToken ct)
    {
        await savings.DeleteContainerAsync(id, ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<SavingsMonthResponse>> GetMonth([FromQuery] string? month, CancellationToken ct)
    {
        var result = await savings.GetMonthAsync(month, ct);
        return Ok(result);
    }

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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await savings.DeleteEntryAsync(id, ct);
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SavingsSummary>> Summary([FromQuery] string? to, CancellationToken ct)
    {
        var result = await savings.SummaryAsync(to, ct);
        return Ok(result);
    }
}
