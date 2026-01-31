namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents a webhook delivery attempt
/// </summary>
public class WebhookDelivery
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to the webhook subscription
    /// </summary>
    public int WebhookSubscriptionId { get; set; }
    
    /// <summary>
    /// Navigation property to the webhook subscription
    /// </summary>
    public WebhookSubscription? WebhookSubscription { get; set; }
    
    /// <summary>
    /// Type of event that triggered the webhook
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique correlation ID for tracking this delivery
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON payload sent to the webhook endpoint
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// HMAC signature of the payload
    /// </summary>
    public string Signature { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the delivery
    /// </summary>
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    
    /// <summary>
    /// Number of delivery attempts made
    /// </summary>
    public int AttemptCount { get; set; } = 0;
    
    /// <summary>
    /// HTTP status code from the last delivery attempt
    /// </summary>
    public int? LastHttpStatusCode { get; set; }
    
    /// <summary>
    /// Response body from the last delivery attempt
    /// </summary>
    public string? LastResponseBody { get; set; }
    
    /// <summary>
    /// Error message from the last delivery attempt
    /// </summary>
    public string? LastErrorMessage { get; set; }
    
    /// <summary>
    /// When the delivery was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the last delivery attempt was made
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }
    
    /// <summary>
    /// When the delivery was completed (successfully or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// When the next retry attempt should be made
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Status of a webhook delivery
/// </summary>
public enum WebhookDeliveryStatus
{
    /// <summary>
    /// Delivery is pending first attempt
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Delivery is in progress
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// Delivery was successful
    /// </summary>
    Succeeded = 2,
    
    /// <summary>
    /// Delivery failed and is waiting for retry
    /// </summary>
    Retrying = 3,
    
    /// <summary>
    /// Delivery failed after all retry attempts
    /// </summary>
    Failed = 4
}
