using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing break-glass admin access.
/// Provides emergency access with full audit trail and mandatory justification.
/// </summary>
[ApiController]
[Route("api/break-glass")]
[Authorize]
public sealed class BreakGlassController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<BreakGlassController> _logger;
    private readonly IConfiguration _configuration;

    public BreakGlassController(
        InMemoryReportStore store,
        ILogger<BreakGlassController> logger,
        IConfiguration configuration)
    {
        _store = store;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get current break-glass status for the authenticated user.
    /// </summary>
    /// <response code="200">Returns break-glass status</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(BreakGlassStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<BreakGlassStatusResponse> GetStatus()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in claims" });
        }

        var activeSession = _store.GetActiveBreakGlassSession(userId);
        var isAuthorized = _store.IsAuthorizedForBreakGlass(userId);

        var response = new BreakGlassStatusResponse
        {
            IsActive = activeSession != null,
            IsAuthorized = isAuthorized,
            ActiveSession = activeSession != null ? MapToResponse(activeSession) : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Activate break-glass admin access.
    /// Requires strong authentication and a mandatory, detailed reason.
    /// </summary>
    /// <param name="request">Activation request with mandatory reason</param>
    /// <response code="200">Break-glass access activated successfully</response>
    /// <response code="400">Invalid request or user not authorized</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not authorized for break-glass access</response>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(ActivateBreakGlassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ActivateBreakGlassResponse> Activate([FromBody] ActivateBreakGlassRequest request)
    {
        var userId = GetUserId();
        var userName = GetUserName();
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in claims" });
        }

        // Check if break-glass is enabled
        var config = GetBreakGlassConfig();
        if (!config.Enabled)
        {
            return BadRequest(new ActivateBreakGlassResponse
            {
                Success = false,
                ErrorMessage = "Break-glass access is currently disabled"
            });
        }

        // Check authorization
        if (!_store.IsAuthorizedForBreakGlass(userId))
        {
            _logger.LogWarning("Unauthorized break-glass activation attempt by user {UserId}", userId);
            return StatusCode(StatusCodes.Status403Forbidden, new ActivateBreakGlassResponse
            {
                Success = false,
                ErrorMessage = "User is not authorized to activate break-glass access"
            });
        }

        // Validate reason length
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < config.MinReasonLength)
        {
            return BadRequest(new ActivateBreakGlassResponse
            {
                Success = false,
                ErrorMessage = $"Reason must be at least {config.MinReasonLength} characters long"
            });
        }

        // Check MFA requirement
        if (config.RequireMfa && !IsMfaVerified())
        {
            _logger.LogWarning("Break-glass activation attempt without MFA by user {UserId}", userId);
            return BadRequest(new ActivateBreakGlassResponse
            {
                Success = false,
                ErrorMessage = "MFA verification is required to activate break-glass access"
            });
        }

        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Determine authentication method from claims
        var authMethod = GetAuthenticationMethod();

        // Activate break-glass
        var (success, errorMessage, session) = _store.ActivateBreakGlass(
            userId,
            userName,
            request.Reason,
            authMethod,
            ipAddress
        );

        if (!success)
        {
            return BadRequest(new ActivateBreakGlassResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            });
        }

        _logger.LogWarning(
            "Break-glass access activated for user {UserId} ({UserName}). Reason: {Reason}",
            userId,
            userName,
            request.Reason
        );

        return Ok(new ActivateBreakGlassResponse
        {
            Success = true,
            Session = MapToResponse(session!)
        });
    }

    /// <summary>
    /// Deactivate the current user's active break-glass session.
    /// Records deactivation in audit trail and restores normal access controls.
    /// </summary>
    /// <param name="request">Deactivation request with optional note</param>
    /// <response code="200">Break-glass access deactivated successfully</response>
    /// <response code="400">Invalid request or no active session</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost("deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult Deactivate([FromBody] DeactivateBreakGlassRequest? request = null)
    {
        var userId = GetUserId();
        var userName = GetUserName();
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in claims" });
        }

        var activeSession = _store.GetActiveBreakGlassSession(userId);
        if (activeSession == null)
        {
            return BadRequest(new { error = "No active break-glass session found" });
        }

        var (success, errorMessage) = _store.DeactivateBreakGlass(
            activeSession.Id,
            userId,
            userName,
            request?.Note
        );

        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation(
            "Break-glass access deactivated for user {UserId} ({UserName}). Session ID: {SessionId}, Actions performed: {ActionCount}",
            userId,
            userName,
            activeSession.Id,
            activeSession.ActionCount
        );

        return Ok(new { message = "Break-glass access deactivated successfully" });
    }

    /// <summary>
    /// Get break-glass session history.
    /// Returns sessions for the current user, or all sessions if user is admin.
    /// </summary>
    /// <param name="userId">User ID to filter by (admin only)</param>
    /// <param name="activeOnly">Filter to active sessions only</param>
    /// <response code="200">Returns list of break-glass sessions</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not authorized to view other users' sessions</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<BreakGlassSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<BreakGlassSessionResponse>> GetSessions(
        [FromQuery] string? userId = null,
        [FromQuery] bool? activeOnly = null)
    {
        var currentUserId = GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { error = "User ID not found in claims" });
        }

        // If filtering by another user's ID, check if current user is admin
        if (!string.IsNullOrEmpty(userId) && userId != currentUserId)
        {
            if (!_store.IsAuthorizedForBreakGlass(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Not authorized to view other users' sessions" });
            }
        }

        // If no userId specified and user is not admin, show only their sessions
        var filterUserId = userId;
        if (string.IsNullOrEmpty(filterUserId) && !_store.IsAuthorizedForBreakGlass(currentUserId))
        {
            filterUserId = currentUserId;
        }

        var sessions = _store.GetBreakGlassSessions(filterUserId, activeOnly);
        var response = sessions.Select(MapToResponse).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get break-glass configuration.
    /// Admin users receive full configuration including authorized role IDs.
    /// Non-admin users receive limited configuration with sensitive fields omitted.
    /// </summary>
    /// <response code="200">Returns break-glass configuration</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("config")]
    [ProducesResponseType(typeof(BreakGlassConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<BreakGlassConfig> GetConfig()
    {
        var config = GetBreakGlassConfig();
        
        // Remove sensitive information like authorized role IDs for non-admin users
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId) || !_store.IsAuthorizedForBreakGlass(userId))
        {
            // Return limited config for non-authorized users
            return Ok(new BreakGlassConfig
            {
                Enabled = config.Enabled,
                MinReasonLength = config.MinReasonLength,
                RequireMfa = config.RequireMfa,
                MaxSessionDurationHours = config.MaxSessionDurationHours
                // AuthorizedRoleIds intentionally omitted for security
            });
        }
        
        return Ok(config);
    }

    #region Helper Methods

    private string GetUserId()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value 
               ?? User.Identity?.Name 
               ?? string.Empty;
    }

    private string GetUserName()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "name")?.Value 
               ?? User.Identity?.Name 
               ?? "Unknown User";
    }

    private bool IsMfaVerified()
    {
        // Check for MFA verification claim
        var mfaVerified = User.HasClaim(c => c.Type == "mfa_verified" && c.Value == "true");
        
        // Also check AMR (Authentication Methods Reference) claim for MFA
        var amrClaim = User.Claims.FirstOrDefault(c => c.Type == "amr");
        if (amrClaim != null && (amrClaim.Value.Contains("mfa") || amrClaim.Value.Contains("otp")))
        {
            return true;
        }
        
        return mfaVerified;
    }

    private string GetAuthenticationMethod()
    {
        // Try to determine authentication method from claims
        var amrClaim = User.Claims.FirstOrDefault(c => c.Type == "amr")?.Value;
        if (!string.IsNullOrEmpty(amrClaim))
        {
            return amrClaim;
        }

        // Check for specific MFA claim
        if (User.HasClaim(c => c.Type == "mfa_verified"))
        {
            return "MFA";
        }

        return "Standard";
    }

    private BreakGlassConfig GetBreakGlassConfig()
    {
        var config = _configuration.GetSection("BreakGlass").Get<BreakGlassConfig>();
        return config ?? new BreakGlassConfig();
    }

    private static BreakGlassSessionResponse MapToResponse(BreakGlassSession session)
    {
        return new BreakGlassSessionResponse
        {
            Id = session.Id,
            UserId = session.UserId,
            UserName = session.UserName,
            Reason = session.Reason,
            ActivatedAt = session.ActivatedAt,
            DeactivatedAt = session.DeactivatedAt,
            DeactivatedByName = session.DeactivatedByName,
            IsActive = session.IsActive,
            AuthenticationMethod = session.AuthenticationMethod,
            ActionCount = session.ActionCount
        };
    }

    #endregion
}
