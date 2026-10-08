using System.Text.Json;
using Budget.Application.Common;
using Microsoft.AspNetCore.Http;

namespace BudgetApi.Common;

/// <summary>Преобразует исключения API в унифицированный JSON-ответ с подходящим HTTP-статусом.</summary>
/// <param name="next">Следующий компонент конвейера обработки HTTP-запроса.</param>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Выполняет следующий компонент и преобразует необработанные ошибки в ответ API.</summary>
    /// <param name="context">Контекст текущего HTTP-запроса и формируемого ответа.</param>
    /// <param name="logger">Журнал для регистрации непредвиденных исключений.</param>
    /// <returns>Асинхронная операция обработки запроса.</returns>
    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (ApiErrorException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = new ApiError { Code = ex.Code, Message = ex.Message, Errors = ex.Errors };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = new ApiError { Code = ApiErrorCodes.InternalServerError, Message = "Внутренняя ошибка сервера" };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
