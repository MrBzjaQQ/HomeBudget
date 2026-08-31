using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace BudgetApi.Common;

/// <summary>Maps <see cref="ApiErrorException"/> to the unified JSON error format.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            var payload = new ApiError { Code = "InternalServerError", Message = "Внутренняя ошибка сервера" };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
