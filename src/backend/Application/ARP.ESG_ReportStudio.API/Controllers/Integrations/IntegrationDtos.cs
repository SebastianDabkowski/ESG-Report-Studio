namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

/// <summary>
/// Request DTO for creating a new connector
/// </summary>
public class CreateConnectorRequest
{
    public string Name { get; set; } = string.Empty;
    public string ConnectorType { get; set; } = string.Empty;
    public string EndpointBaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = string.Empty;
    public string AuthenticationSecretRef { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; } = 0;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool UseExponentialBackoff { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating a connector
/// </summary>
public class UpdateConnectorRequest
{
    public string Name { get; set; } = string.Empty;
    public string EndpointBaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = string.Empty;
    public string AuthenticationSecretRef { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; } = 0;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool UseExponentialBackoff { get; set; } = true;
    public string? Description { get; set; }
    public string? MappingConfiguration { get; set; }
}

/// <summary>
/// Response DTO for connector
/// </summary>
public class ConnectorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectorType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EndpointBaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = string.Empty;
    public string AuthenticationSecretRef { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public bool UseExponentialBackoff { get; set; }
    public string MappingConfiguration { get; set; } = "{}";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Response DTO for integration log
/// </summary>
public class IntegrationLogResponse
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string? ConnectorName { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? HttpStatusCode { get; set; }
    public int RetryAttempts { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
}
