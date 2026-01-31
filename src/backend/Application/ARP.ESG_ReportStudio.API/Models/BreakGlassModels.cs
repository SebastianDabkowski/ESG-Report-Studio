namespace ARP.ESG_ReportStudio.API.Models;

/// <summary>
/// Request to activate break-glass admin access.
/// Requires strong authentication and mandatory reason.
/// </summary>
public sealed class ActivateBreakGlassRequest
{
    /// <summary>
    /// Mandatory reason explaining why break-glass access is needed.
    /// Must be detailed and justifiable for audit purposes.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication method used (e.g., "MFA", "Hardware-Token", "Biometric").
    /// Populated by authentication middleware.
    /// </summary>
    public string? AuthenticationMethod { get; set; }
}

/// <summary>
/// Request to deactivate an active break-glass session.
/// </summary>
public sealed class DeactivateBreakGlassRequest
{
    /// <summary>
    /// Optional note explaining the deactivation or summarizing actions taken.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Response containing break-glass session details.
/// </summary>
public sealed class BreakGlassSessionResponse
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who activated the session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User name who activated the session.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for activation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when activated.
    /// </summary>
    public string ActivatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when deactivated (null if still active).
    /// </summary>
    public string? DeactivatedAt { get; set; }
    
    /// <summary>
    /// User who deactivated the session (null if still active).
    /// </summary>
    public string? DeactivatedByName { get; set; }
    
    /// <summary>
    /// Whether the session is currently active.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Authentication method used.
    /// </summary>
    public string? AuthenticationMethod { get; set; }
    
    /// <summary>
    /// Number of actions performed during this session.
    /// </summary>
    public int ActionCount { get; set; }
}

/// <summary>
/// Response containing current break-glass status for a user.
/// </summary>
public sealed class BreakGlassStatusResponse
{
    /// <summary>
    /// Whether break-glass access is currently active for the user.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Active session details (null if not active).
    /// </summary>
    public BreakGlassSessionResponse? ActiveSession { get; set; }
    
    /// <summary>
    /// Whether the user is authorized to activate break-glass.
    /// </summary>
    public bool IsAuthorized { get; set; }
}

/// <summary>
/// Response from activating break-glass access.
/// </summary>
public sealed class ActivateBreakGlassResponse
{
    /// <summary>
    /// Whether activation was successful.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if activation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Created session details (null if failed).
    /// </summary>
    public BreakGlassSessionResponse? Session { get; set; }
}

/// <summary>
/// Configuration for break-glass access control.
/// </summary>
public sealed class BreakGlassConfig
{
    /// <summary>
    /// Whether break-glass access is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Minimum reason length in characters.
    /// </summary>
    public int MinReasonLength { get; set; } = 20;
    
    /// <summary>
    /// Role IDs that are authorized to use break-glass.
    /// Typically limited to admin or emergency response roles.
    /// </summary>
    public List<string> AuthorizedRoleIds { get; set; } = new() { "role-admin" };
    
    /// <summary>
    /// Whether MFA is required to activate break-glass.
    /// </summary>
    public bool RequireMfa { get; set; } = true;
    
    /// <summary>
    /// Maximum duration in hours for a break-glass session (0 = no limit).
    /// </summary>
    public int MaxSessionDurationHours { get; set; } = 0;
}
