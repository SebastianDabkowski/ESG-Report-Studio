namespace ARP.ESG_ReportStudio.API.Models;

/// <summary>
/// Represents an active user session.
/// </summary>
public sealed class UserSession
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID associated with this session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User name for audit logging.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// When the session was created (ISO 8601 format).
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Last activity timestamp (ISO 8601 format).
    /// </summary>
    public string LastActivityAt { get; set; } = string.Empty;
    
    /// <summary>
    /// When the session will expire due to inactivity (ISO 8601 format).
    /// </summary>
    public string ExpiresAt { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address from which the session was initiated.
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string from the client.
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Whether this session has MFA verification.
    /// </summary>
    public bool MfaVerified { get; set; }
}

/// <summary>
/// Session activity event for audit logging.
/// </summary>
public sealed class SessionActivityEvent
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID this event belongs to.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID associated with this event.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User name for audit logging.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Event type: "login", "logout", "timeout", "activity", "refresh".
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// When the event occurred (ISO 8601 format).
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address from which the event originated.
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Additional event details.
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Session status response.
/// </summary>
public sealed class SessionStatusResponse
{
    /// <summary>
    /// Whether the session is active.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Session ID if active.
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// When the session will expire (ISO 8601 format).
    /// </summary>
    public string? ExpiresAt { get; set; }
    
    /// <summary>
    /// Minutes until session expires.
    /// </summary>
    public int? MinutesUntilExpiration { get; set; }
    
    /// <summary>
    /// Whether a warning should be shown (session expiring soon).
    /// </summary>
    public bool ShouldWarn { get; set; }
    
    /// <summary>
    /// Whether session refresh is allowed.
    /// </summary>
    public bool CanRefresh { get; set; }
}
