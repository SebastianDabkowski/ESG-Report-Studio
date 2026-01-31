using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for HR sync records
/// </summary>
public interface IHRSyncRecordRepository
{
    Task<HRSyncRecord> CreateAsync(HRSyncRecord record);
    Task<HRSyncRecord?> GetByIdAsync(int id);
    Task<List<HRSyncRecord>> GetByConnectorIdAsync(int connectorId, int limit = 100);
    Task<List<HRSyncRecord>> GetByCorrelationIdAsync(string correlationId);
    Task<List<HRSyncRecord>> GetRejectedRecordsAsync(int connectorId, int limit = 100);
}
