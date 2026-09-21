using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StatementVault.Api.Hosting;

public sealed class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is FormatException)
        {
            logger.LogInformation(exception, "Rejected request due to a format error.");
            await WriteAsync(httpContext, StatusCodes.Status400BadRequest, "Invalid request.", exception.Message, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception.");
        await WriteAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "See logs for the correlation id.",
            cancellationToken);
        return true;
    }

    private static async Task WriteAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}"
        };
        problem.Extensions["correlationId"] = CorrelationIdMiddleware.Get(httpContext);
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}
