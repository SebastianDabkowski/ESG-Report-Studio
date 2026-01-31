using ARP.ESG_ReportStudio.API.Models;
using System.Net;
using System.Text.Json;

namespace ARP.ESG_ReportStudio.API.Middleware;

/// <summary>
/// Global exception handler middleware that ensures consistent error responses
/// with correlation IDs across all API endpoints
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Get correlation ID from context
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        // Log the exception with correlation ID
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);

        // Determine status code and title based on exception type
        var (statusCode, title) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Conflict"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        // Create standardized error response
        var errorResponse = new ErrorResponse
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path
        };

        // In development, include stack trace
        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            errorResponse.Errors = new Dictionary<string, object>
            {
                ["stackTrace"] = exception.StackTrace ?? string.Empty
            };
        }

        // Set response
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}

/// <summary>
/// Extension method to add global exception handler middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
