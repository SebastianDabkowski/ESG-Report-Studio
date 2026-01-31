using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for IntegrationLog entity
/// </summary>
public interface IIntegrationLogRepository
{
    /// <summary>
    /// Get an integration log by ID
    /// </summary>
    Task<IntegrationLog?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get integration logs by correlation ID
    /// </summary>
    Task<List<IntegrationLog>> GetByCorrelationIdAsync(string correlationId);
    
    /// <summary>
    /// Get integration logs for a connector
    /// </summary>
    Task<List<IntegrationLog>> GetByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Create a new integration log entry
    /// </summary>
    Task<IntegrationLog> CreateAsync(IntegrationLog log);
    
    /// <summary>
    /// Update an existing integration log entry
    /// </summary>
    Task<IntegrationLog> UpdateAsync(IntegrationLog log);
}
