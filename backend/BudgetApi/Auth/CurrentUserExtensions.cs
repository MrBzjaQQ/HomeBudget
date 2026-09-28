using System.Security.Claims;
using Budget.Application.Common;

namespace BudgetApi.Auth;

public static class CurrentUserExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue("sub")
           ?? throw ApiErrorException.Unauthorized("Токен не содержит идентификатор пользователя"));
}
