using System.Security.Claims;
using Budget.Application.Common;

namespace BudgetApi.Auth;

/// <summary>Предоставляет методы извлечения данных текущего пользователя из claims.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Возвращает числовой идентификатор из claim <c>sub</c>.</summary>
    /// <param name="principal">Пользовательский контекст HTTP-запроса.</param>
    /// <returns>Идентификатор текущего пользователя.</returns>
    /// <exception cref="ApiErrorException">Выбрасывается, если claim идентификатора отсутствует.</exception>
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue("sub")
           ?? throw ApiErrorException.Unauthorized("Токен не содержит идентификатор пользователя"));
}
