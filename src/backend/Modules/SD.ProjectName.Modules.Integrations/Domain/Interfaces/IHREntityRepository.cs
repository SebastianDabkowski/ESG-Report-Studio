using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for HR entities
/// </summary>
public interface IHREntityRepository
{
    Task<HREntity> CreateAsync(HREntity entity);
    Task<HREntity> UpdateAsync(HREntity entity);
    Task<HREntity?> GetByIdAsync(int id);
    Task<HREntity?> GetByExternalIdAsync(int connectorId, string externalId);
    Task<List<HREntity>> GetByConnectorIdAsync(int connectorId, int limit = 100);
}
