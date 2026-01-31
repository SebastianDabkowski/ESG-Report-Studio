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
    
    /// <summary>
    /// Search integration logs with filtering
    /// </summary>
    Task<List<IntegrationLog>> SearchLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationStatus? status = null,
        int? connectorId = null,
        string? operationType = null,
        string? initiatedBy = null,
        int skip = 0,
        int take = 50);
    
    /// <summary>
    /// Get total count of logs matching search criteria
    /// </summary>
    Task<int> GetLogCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationStatus? status = null,
        int? connectorId = null,
        string? operationType = null,
        string? initiatedBy = null);
}
