using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for session management operations.
/// </summary>
[ApiController]
[Route("api/session")]
public sealed class SessionController : ControllerBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionManager sessionManager,
        ILogger<SessionController> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Get current session status.
    /// </summary>
    /// <response code="200">Returns session status</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(typeof(SessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionStatusResponse>> GetSessionStatus()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var sessionId = User.FindFirst("session_id")?.Value;

        if (string.IsNullOrEmpty(sessionId))
        {
            return Ok(new SessionStatusResponse
            {
                IsActive = false,
                CanRefresh = false,
                ShouldWarn = false
            });
        }

        var status = await _sessionManager.GetSessionStatusAsync(sessionId);
        return Ok(status);
    }

    /// <summary>
    /// Refresh the current session.
    /// </summary>
    /// <response code="200">Session refreshed successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="400">Session refresh is not allowed or session is invalid</response>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var sessionId = User.FindFirst("session_id")?.Value;

        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest(new { error = "No active session found" });
        }

        var refreshed = await _sessionManager.RefreshSessionAsync(sessionId);

        if (!refreshed)
        {
            return BadRequest(new { error = "Session cannot be refreshed" });
        }

        return Ok(new { message = "Session refreshed successfully" });
    }

    /// <summary>
    /// End the current session (logout).
    /// </summary>
    /// <response code="200">Session ended successfully</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var sessionId = User.FindFirst("session_id")?.Value;

        if (!string.IsNullOrEmpty(sessionId))
        {
            await _sessionManager.EndSessionAsync(sessionId, "User logout");
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get session activity events for the current user.
    /// </summary>
    /// <param name="limit">Maximum number of events to return (default: 100, max: 500)</param>
    /// <response code="200">Returns session activity events</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("events")]
    [Authorize]
    [ProducesResponseType(typeof(List<SessionActivityEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SessionActivityEvent>>> GetSessionEvents([FromQuery] int limit = 100)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        // Limit the maximum number of events that can be requested
        limit = Math.Min(limit, 500);

        var userId = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst("preferred_username")?.Value 
                     ?? User.FindFirst("name")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found in token" });
        }

        var events = await _sessionManager.GetSessionEventsAsync(userId: userId, limit: limit);
        return Ok(events);
    }
}
