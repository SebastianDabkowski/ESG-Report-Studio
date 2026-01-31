namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

using SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Repository for managing canonical field mappings
/// </summary>
public interface ICanonicalMappingRepository
{
    /// <summary>
    /// Get a mapping by ID
    /// </summary>
    Task<CanonicalMapping?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get all active mappings for a connector
    /// </summary>
    Task<List<CanonicalMapping>> GetByConnectorAsync(int connectorId);
    
    /// <summary>
    /// Get mappings for a connector targeting a specific entity type
    /// </summary>
    Task<List<CanonicalMapping>> GetByConnectorAndTypeAsync(int connectorId, CanonicalEntityType targetEntityType);
    
    /// <summary>
    /// Get mappings for a connector and entity type at a specific schema version
    /// </summary>
    Task<List<CanonicalMapping>> GetByConnectorTypeAndVersionAsync(
        int connectorId, 
        CanonicalEntityType targetEntityType, 
        int targetSchemaVersion);
    
    /// <summary>
    /// Create a new mapping
    /// </summary>
    Task<CanonicalMapping> CreateAsync(CanonicalMapping mapping);
    
    /// <summary>
    /// Update an existing mapping
    /// </summary>
    Task UpdateAsync(CanonicalMapping mapping);
    
    /// <summary>
    /// Delete a mapping
    /// </summary>
    Task DeleteAsync(int id);
    
    /// <summary>
    /// Deactivate a mapping (soft delete)
    /// </summary>
    Task DeactivateAsync(int id);
    
    /// <summary>
    /// Get required mappings for a connector and entity type
    /// </summary>
    Task<List<CanonicalMapping>> GetRequiredMappingsAsync(int connectorId, CanonicalEntityType targetEntityType);
}
