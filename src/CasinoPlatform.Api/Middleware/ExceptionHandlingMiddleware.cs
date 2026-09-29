using System.Net;
using System.Text.Json;

namespace CasinoPlatform.Api.Middleware;

/// <summary>
/// Catches anything that bubbles past controller-level try/catch so the API
/// never leaks stack traces to the client (Stage 9, item 51).
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                message = "An unexpected error occurred. Please try again.",
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
