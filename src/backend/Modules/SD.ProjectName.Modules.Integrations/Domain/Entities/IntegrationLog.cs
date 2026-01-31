namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Audit log for integration calls to external systems
/// </summary>
public class IntegrationLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector used for this call
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// Correlation ID for tracing across systems
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of operation (e.g., "pull", "push", "webhook")
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the integration call
    /// </summary>
    public IntegrationStatus Status { get; set; }
    
    /// <summary>
    /// HTTP method used (if applicable)
    /// </summary>
    public string? HttpMethod { get; set; }
    
    /// <summary>
    /// Endpoint called (relative to base URL)
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// HTTP status code received (if applicable)
    /// </summary>
    public int? HttpStatusCode { get; set; }
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; } = 0;
    
    /// <summary>
    /// Error message if the call failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Stack trace or additional error details
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// Request payload or summary (sanitized, no secrets)
    /// </summary>
    public string? RequestSummary { get; set; }
    
    /// <summary>
    /// Response payload or summary (sanitized)
    /// </summary>
    public string? ResponseSummary { get; set; }
    
    /// <summary>
    /// Duration of the call in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// When the call was initiated
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the call completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// User or service that initiated the call
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
}

/// <summary>
/// Status of an integration call
/// </summary>
public enum IntegrationStatus
{
    /// <summary>
    /// Call is in progress
    /// </summary>
    InProgress = 0,
    
    /// <summary>
    /// Call succeeded
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Call failed (after all retries)
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Call was skipped (e.g., connector disabled)
    /// </summary>
    Skipped = 3
}
