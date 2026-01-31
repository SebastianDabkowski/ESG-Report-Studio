using ARP.ESG_ReportStudio.API.Services;
using System.Security.Claims;

namespace ARP.ESG_ReportStudio.API.Middleware;

/// <summary>
/// Middleware to track session activity and enforce session timeouts.
/// </summary>
public sealed class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;

    public SessionActivityMiddleware(RequestDelegate next, ILogger<SessionActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionManager sessionManager)
    {
        // Skip session tracking for anonymous endpoints
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Get session ID from claims (set during authentication)
        var sessionId = context.User.FindFirst("session_id")?.Value;

        if (!string.IsNullOrEmpty(sessionId))
        {
            // Update session activity
            var isActive = await sessionManager.UpdateActivityAsync(sessionId);

            if (!isActive)
            {
                // Session expired or invalid
                _logger.LogWarning(
                    "Session {SessionId} is no longer active, rejecting request",
                    sessionId);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers["X-Session-Expired"] = "true";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Session expired",
                    message = "Your session has expired due to inactivity. Please sign in again."
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the session activity middleware.
/// </summary>
public static class SessionActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionActivity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionActivityMiddleware>();
    }
}
