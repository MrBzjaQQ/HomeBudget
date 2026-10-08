using Budget.Application.Common;
using Budget.Application.Users;
using BudgetApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApi.Controllers;

/// <summary>Предоставляет HTTP-операции просмотра, создания и изменения пользователей.</summary>
/// <param name="users">Сервис управления учётными записями пользователей.</param>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService users) : ControllerBase
{
    /// <summary>Возвращает список пользователей.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список пользовательских профилей.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct)
    {
        var result = await users.ListAsync(ct);
        return Ok(result);
    }

    /// <summary>Создаёт учётную запись пользователя.</summary>
    /// <param name="request">Логин, отображаемое имя и пароль нового пользователя.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданный профиль с HTTP-статусом 201.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await users.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Изменяет профиль или состояние учётной записи пользователя.</summary>
    /// <param name="id">Идентификатор изменяемого пользователя.</param>
    /// <param name="request">Новые значения профиля.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённый профиль пользователя.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw ApiErrorException.BadRequest("Пустое тело запроса");
        }

        var result = await users.UpdateAsync(id, request, ct);
        return Ok(result);
    }
}
