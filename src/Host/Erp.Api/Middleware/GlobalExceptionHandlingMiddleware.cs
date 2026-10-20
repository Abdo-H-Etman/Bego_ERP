using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<GlobalExceptionHandlingMiddleware>();

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred.";
        var detail = exception.Message;

        if (exception is DbUpdateConcurrencyException)
        {
            statusCode = HttpStatusCode.Conflict;
            title = "Concurrency Conflict";
            detail = "The record was modified by another user or process. Please reload and try again.";
            _logger.Warning(exception, "[{CorrelationId}] Concurrency conflict detected at {Path}", correlationId, context.Request.Path);
        }
        else
        {
            _logger.Error(exception, "[{CorrelationId}] Unhandled exception at {Path}: {Message}", correlationId, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail,
            instance = context.Request.Path.Value,
            correlationId,
            timestampUtc = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
