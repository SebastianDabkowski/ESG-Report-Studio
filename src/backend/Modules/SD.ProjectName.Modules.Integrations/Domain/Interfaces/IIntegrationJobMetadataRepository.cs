using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for IntegrationJobMetadata entity
/// </summary>
public interface IIntegrationJobMetadataRepository
{
    /// <summary>
    /// Get a job by ID
    /// </summary>
    Task<IntegrationJobMetadata?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get a job by job ID (unique identifier)
    /// </summary>
    Task<IntegrationJobMetadata?> GetByJobIdAsync(string jobId);
    
    /// <summary>
    /// Get jobs by correlation ID
    /// </summary>
    Task<List<IntegrationJobMetadata>> GetByCorrelationIdAsync(string correlationId);
    
    /// <summary>
    /// Get jobs for a connector
    /// </summary>
    Task<List<IntegrationJobMetadata>> GetByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Search jobs with filtering
    /// </summary>
    Task<List<IntegrationJobMetadata>> SearchJobsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationJobStatus? status = null,
        int? connectorId = null,
        string? jobType = null,
        string? initiatedBy = null,
        int skip = 0,
        int take = 50);
    
    /// <summary>
    /// Create a new job metadata entry
    /// </summary>
    Task<IntegrationJobMetadata> CreateAsync(IntegrationJobMetadata job);
    
    /// <summary>
    /// Update an existing job metadata entry
    /// </summary>
    Task<IntegrationJobMetadata> UpdateAsync(IntegrationJobMetadata job);
    
    /// <summary>
    /// Get total count of jobs matching search criteria
    /// </summary>
    Task<int> GetJobCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationJobStatus? status = null,
        int? connectorId = null,
        string? jobType = null,
        string? initiatedBy = null);
}
