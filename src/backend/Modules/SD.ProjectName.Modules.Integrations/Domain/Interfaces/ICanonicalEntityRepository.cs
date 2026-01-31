namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

using SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Repository for managing canonical entities
/// </summary>
public interface ICanonicalEntityRepository
{
    /// <summary>
    /// Get a canonical entity by ID
    /// </summary>
    Task<CanonicalEntity?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get canonical entities by type
    /// </summary>
    Task<List<CanonicalEntity>> GetByTypeAsync(CanonicalEntityType entityType, int? schemaVersion = null);
    
    /// <summary>
    /// Get canonical entity by external ID and source system
    /// </summary>
    Task<CanonicalEntity?> GetByExternalIdAsync(string externalId, string sourceSystem);
    
    /// <summary>
    /// Get canonical entities by import job ID
    /// </summary>
    Task<List<CanonicalEntity>> GetByImportJobIdAsync(string importJobId);
    
    /// <summary>
    /// Create a new canonical entity
    /// </summary>
    Task<CanonicalEntity> CreateAsync(CanonicalEntity entity);
    
    /// <summary>
    /// Update an existing canonical entity
    /// </summary>
    Task UpdateAsync(CanonicalEntity entity);
    
    /// <summary>
    /// Delete a canonical entity
    /// </summary>
    Task DeleteAsync(int id);
    
    /// <summary>
    /// Check if a canonical entity exists by external ID and source
    /// </summary>
    Task<bool> ExistsAsync(string externalId, string sourceSystem);
}
