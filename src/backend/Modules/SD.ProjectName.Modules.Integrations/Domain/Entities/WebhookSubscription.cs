namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents a webhook subscription for outbound events
/// </summary>
public class WebhookSubscription
{
    public int Id { get; set; }
    
    /// <summary>
    /// User-friendly name for the subscription
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Target URL where webhook events will be delivered
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Comma-separated list of event types subscribed to
    /// (e.g., "DataChange,Approval,Export")
    /// </summary>
    public string SubscribedEvents { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the subscription
    /// </summary>
    public WebhookSubscriptionStatus Status { get; set; } = WebhookSubscriptionStatus.PendingVerification;
    
    /// <summary>
    /// Secret used for HMAC signature generation
    /// </summary>
    public string SigningSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// When the signing secret was last rotated
    /// </summary>
    public DateTime? SecretRotatedAt { get; set; }
    
    /// <summary>
    /// Verification token sent during handshake
    /// </summary>
    public string? VerificationToken { get; set; }
    
    /// <summary>
    /// When the subscription was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
    
    /// <summary>
    /// Maximum number of retry attempts for failed deliveries
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Initial retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int ConsecutiveFailures { get; set; } = 0;
    
    /// <summary>
    /// When the subscription was last marked as degraded
    /// </summary>
    public DateTime? DegradedAt { get; set; }
    
    /// <summary>
    /// Description or reason for degraded status
    /// </summary>
    public string? DegradedReason { get; set; }
    
    /// <summary>
    /// Optional description or notes
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who created the subscription
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the subscription was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Who last updated the subscription
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Status of a webhook subscription
/// </summary>
public enum WebhookSubscriptionStatus
{
    /// <summary>
    /// Subscription is pending verification handshake
    /// </summary>
    PendingVerification = 0,
    
    /// <summary>
    /// Subscription is active and receiving events
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Subscription is paused and not receiving events
    /// </summary>
    Paused = 2,
    
    /// <summary>
    /// Subscription is degraded due to consecutive failures
    /// </summary>
    Degraded = 3,
    
    /// <summary>
    /// Subscription is disabled
    /// </summary>
    Disabled = 4
}
