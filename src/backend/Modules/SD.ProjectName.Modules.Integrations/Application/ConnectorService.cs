using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Application service for managing connectors
/// </summary>
public class ConnectorService
{
    private readonly IConnectorRepository _connectorRepository;

    public ConnectorService(IConnectorRepository connectorRepository)
    {
        _connectorRepository = connectorRepository;
    }

    /// <summary>
    /// Create a new connector
    /// </summary>
    public async Task<Connector> CreateConnectorAsync(
        string name,
        string connectorType,
        string endpointBaseUrl,
        string authenticationType,
        string authenticationSecretRef,
        string capabilities,
        string createdBy,
        int rateLimitPerMinute = 0,
        int maxRetryAttempts = 3,
        int retryDelaySeconds = 5,
        bool useExponentialBackoff = true,
        string? description = null)
    {
        var connector = new Connector
        {
            Name = name,
            ConnectorType = connectorType,
            Status = ConnectorStatus.Disabled, // Default to disabled for safety
            EndpointBaseUrl = endpointBaseUrl,
            AuthenticationType = authenticationType,
            AuthenticationSecretRef = authenticationSecretRef,
            Capabilities = capabilities,
            RateLimitPerMinute = rateLimitPerMinute,
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelaySeconds = retryDelaySeconds,
            UseExponentialBackoff = useExponentialBackoff,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        return await _connectorRepository.CreateAsync(connector);
    }

    /// <summary>
    /// Get a connector by ID
    /// </summary>
    public async Task<Connector?> GetConnectorByIdAsync(int id)
    {
        return await _connectorRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Get all connectors
    /// </summary>
    public async Task<List<Connector>> GetAllConnectorsAsync()
    {
        return await _connectorRepository.GetAllAsync();
    }

    /// <summary>
    /// Enable a connector
    /// </summary>
    public async Task<Connector> EnableConnectorAsync(int id, string updatedBy)
    {
        var connector = await _connectorRepository.GetByIdAsync(id);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {id} not found");
        }

        connector.Status = ConnectorStatus.Enabled;
        connector.UpdatedBy = updatedBy;
        connector.UpdatedAt = DateTime.UtcNow;

        return await _connectorRepository.UpdateAsync(connector);
    }

    /// <summary>
    /// Disable a connector
    /// </summary>
    public async Task<Connector> DisableConnectorAsync(int id, string updatedBy)
    {
        var connector = await _connectorRepository.GetByIdAsync(id);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {id} not found");
        }

        connector.Status = ConnectorStatus.Disabled;
        connector.UpdatedBy = updatedBy;
        connector.UpdatedAt = DateTime.UtcNow;

        return await _connectorRepository.UpdateAsync(connector);
    }

    /// <summary>
    /// Update connector configuration
    /// </summary>
    public async Task<Connector> UpdateConnectorAsync(
        int id,
        string name,
        string endpointBaseUrl,
        string authenticationType,
        string authenticationSecretRef,
        string capabilities,
        string updatedBy,
        int rateLimitPerMinute = 0,
        int maxRetryAttempts = 3,
        int retryDelaySeconds = 5,
        bool useExponentialBackoff = true,
        string? description = null,
        string? mappingConfiguration = null)
    {
        var connector = await _connectorRepository.GetByIdAsync(id);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {id} not found");
        }

        connector.Name = name;
        connector.EndpointBaseUrl = endpointBaseUrl;
        connector.AuthenticationType = authenticationType;
        connector.AuthenticationSecretRef = authenticationSecretRef;
        connector.Capabilities = capabilities;
        connector.RateLimitPerMinute = rateLimitPerMinute;
        connector.MaxRetryAttempts = maxRetryAttempts;
        connector.RetryDelaySeconds = retryDelaySeconds;
        connector.UseExponentialBackoff = useExponentialBackoff;
        connector.Description = description;
        if (mappingConfiguration != null)
        {
            connector.MappingConfiguration = mappingConfiguration;
        }
        connector.UpdatedBy = updatedBy;
        connector.UpdatedAt = DateTime.UtcNow;

        return await _connectorRepository.UpdateAsync(connector);
    }
}
