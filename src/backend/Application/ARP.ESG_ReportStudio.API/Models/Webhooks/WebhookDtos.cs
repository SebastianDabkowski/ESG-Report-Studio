namespace ARP.ESG_ReportStudio.API.Models.Webhooks;

/// <summary>
/// Request to create a new webhook subscription
/// </summary>
public class CreateWebhookSubscriptionRequest
{
    /// <summary>
    /// User-friendly name for the subscription
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Target URL where webhook events will be delivered
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Event types to subscribe to
    /// </summary>
    public string[] SubscribedEvents { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum retry attempts (default: 3)
    /// </summary>
    public int? MaxRetryAttempts { get; set; }
    
    /// <summary>
    /// Initial retry delay in seconds (default: 5)
    /// </summary>
    public int? RetryDelaySeconds { get; set; }
    
    /// <summary>
    /// Use exponential backoff for retries (default: true)
    /// </summary>
    public bool? UseExponentialBackoff { get; set; }
}

/// <summary>
/// Request to update a webhook subscription
/// </summary>
public class UpdateWebhookSubscriptionRequest
{
    /// <summary>
    /// User-friendly name for the subscription
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Target URL where webhook events will be delivered
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Event types to subscribe to
    /// </summary>
    public string[] SubscribedEvents { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int? MaxRetryAttempts { get; set; }
    
    /// <summary>
    /// Initial retry delay in seconds
    /// </summary>
    public int? RetryDelaySeconds { get; set; }
    
    /// <summary>
    /// Use exponential backoff for retries
    /// </summary>
    public bool? UseExponentialBackoff { get; set; }
}

/// <summary>
/// Webhook subscription response
/// </summary>
public class WebhookSubscriptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string[] SubscribedEvents { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? SecretRotatedAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime? DegradedAt { get; set; }
    public string? DegradedReason { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Webhook delivery response
/// </summary>
public class WebhookDeliveryDto
{
    public int Id { get; set; }
    public int WebhookSubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int? LastHttpStatusCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Webhook event catalogue
/// </summary>
public class WebhookEventCatalogueDto
{
    public string[] SupportedEvents { get; set; } = Array.Empty<string>();
}
