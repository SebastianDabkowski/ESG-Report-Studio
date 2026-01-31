using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for authentication operations and status.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IUserProfileSyncService _userProfileSync;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IUserProfileSyncService userProfileSync,
        IConfiguration configuration,
        ILogger<AuthenticationController> logger)
    {
        _userProfileSync = userProfileSync;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get authentication configuration status.
    /// </summary>
    /// <response code="200">Returns authentication configuration</response>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthConfigResponse> GetAuthConfig()
    {
        var authSettings = _configuration.GetSection("Authentication").Get<AuthenticationSettings>();
        
        return Ok(new AuthConfigResponse
        {
            OidcEnabled = authSettings?.Oidc?.Enabled ?? false,
            LocalAuthEnabled = authSettings?.EnableLocalAuth ?? false,
            Authority = authSettings?.Oidc?.Authority ?? string.Empty,
            ClientId = authSettings?.Oidc?.ClientId ?? string.Empty
        });
    }

    /// <summary>
    /// Get current authenticated user information.
    /// </summary>
    /// <response code="200">Returns current user information</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var claims = User.Claims;
        var user = await _userProfileSync.SyncUserFromClaimsAsync(claims);

        return Ok(new CurrentUserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            RoleIds = user.RoleIds
        });
    }

    /// <summary>
    /// Check authentication status.
    /// </summary>
    /// <response code="200">Returns authentication status</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AuthStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthStatusResponse> GetAuthStatus()
    {
        return Ok(new AuthStatusResponse
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType ?? "None"
        });
    }
}

/// <summary>
/// Response model for authentication configuration.
/// </summary>
public sealed class AuthConfigResponse
{
    public bool OidcEnabled { get; set; }
    public bool LocalAuthEnabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Response model for current user information.
/// </summary>
public sealed class CurrentUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> RoleIds { get; set; } = new();
}

/// <summary>
/// Response model for authentication status.
/// </summary>
public sealed class AuthStatusResponse
{
    public bool IsAuthenticated { get; set; }
    public string AuthenticationType { get; set; } = string.Empty;
}
