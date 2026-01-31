using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for FinanceEntity
/// </summary>
public interface IFinanceEntityRepository
{
    /// <summary>
    /// Get finance entity by ID
    /// </summary>
    Task<FinanceEntity?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get finance entity by connector ID and external ID
    /// </summary>
    Task<FinanceEntity?> GetByExternalIdAsync(int connectorId, string externalId);
    
    /// <summary>
    /// Get all finance entities for a connector
    /// </summary>
    Task<List<FinanceEntity>> GetByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Get approved finance entities for a connector
    /// </summary>
    Task<List<FinanceEntity>> GetApprovedByConnectorIdAsync(int connectorId, int limit = 100);
    
    /// <summary>
    /// Add a new finance entity
    /// </summary>
    Task<FinanceEntity> AddAsync(FinanceEntity entity);
    
    /// <summary>
    /// Update an existing finance entity
    /// </summary>
    Task<FinanceEntity> UpdateAsync(FinanceEntity entity);
    
    /// <summary>
    /// Delete a finance entity
    /// </summary>
    Task DeleteAsync(int id);
    
    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync();
}
