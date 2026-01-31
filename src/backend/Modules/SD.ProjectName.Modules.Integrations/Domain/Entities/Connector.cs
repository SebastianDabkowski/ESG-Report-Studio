namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents an external system connector configuration
/// </summary>
public class Connector
{
    public int Id { get; set; }
    
    /// <summary>
    /// User-friendly name for the connector
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of external system (e.g., "HR", "ERP", "Utilities")
    /// </summary>
    public string ConnectorType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the connector
    /// </summary>
    public ConnectorStatus Status { get; set; } = ConnectorStatus.Disabled;
    
    /// <summary>
    /// Base URL for the external system API
    /// </summary>
    public string EndpointBaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication type (e.g., "OAuth2", "ApiKey", "Basic")
    /// </summary>
    public string AuthenticationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to secret store for credentials (never store raw secrets here)
    /// Example: "SecretStore:HR-System-ApiKey"
    /// </summary>
    public string AuthenticationSecretRef { get; set; } = string.Empty;
    
    /// <summary>
    /// Capabilities supported by this connector (comma-separated: "pull", "push", "webhook")
    /// </summary>
    public string Capabilities { get; set; } = string.Empty;
    
    /// <summary>
    /// Rate limit: maximum requests per minute (0 = no limit)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 0;
    
    /// <summary>
    /// Retry policy: maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Retry policy: initial retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Retry policy: whether to use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
    
    /// <summary>
    /// JSON configuration for field mappings (external system fields to internal fields)
    /// </summary>
    public string MappingConfiguration { get; set; } = "{}";
    
    /// <summary>
    /// Optional description or notes
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// When the connector was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who created the connector
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the connector was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Who last updated the connector
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Status of a connector
/// </summary>
public enum ConnectorStatus
{
    /// <summary>
    /// Connector is disabled and will not execute any outbound calls
    /// </summary>
    Disabled = 0,
    
    /// <summary>
    /// Connector is enabled and will execute outbound calls
    /// </summary>
    Enabled = 1
}
