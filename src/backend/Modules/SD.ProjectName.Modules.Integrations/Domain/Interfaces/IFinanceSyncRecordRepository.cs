using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for FinanceSyncRecord
/// </summary>
public interface IFinanceSyncRecordRepository
{
    /// <summary>
    /// Get finance sync record by ID
    /// </summary>
    Task<FinanceSyncRecord?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get sync history for a connector
    /// </summary>
    Task<List<FinanceSyncRecord>> GetByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Get sync records by correlation ID
    /// </summary>
    Task<List<FinanceSyncRecord>> GetByCorrelationIdAsync(string correlationId);
    
    /// <summary>
    /// Get rejected sync records for a connector
    /// </summary>
    Task<List<FinanceSyncRecord>> GetRejectedByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Get conflict records for a connector
    /// </summary>
    Task<List<FinanceSyncRecord>> GetConflictsByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Add a new sync record
    /// </summary>
    Task<FinanceSyncRecord> AddAsync(FinanceSyncRecord record);
    
    /// <summary>
    /// Update an existing sync record
    /// </summary>
    Task<FinanceSyncRecord> UpdateAsync(FinanceSyncRecord record);
    
    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync();
    
    /// <summary>
    /// Search sync records with filtering
    /// </summary>
    Task<List<FinanceSyncRecord>> SearchRecordsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        FinanceSyncStatus? status = null,
        int? connectorId = null,
        bool? conflictDetected = null,
        string? approvedOverrideBy = null,
        int skip = 0,
        int take = 50);
}
