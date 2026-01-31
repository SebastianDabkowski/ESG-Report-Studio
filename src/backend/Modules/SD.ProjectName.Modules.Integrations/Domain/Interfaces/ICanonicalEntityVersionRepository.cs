namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

using SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Repository for managing canonical entity versions
/// </summary>
public interface ICanonicalEntityVersionRepository
{
    /// <summary>
    /// Get a version by ID
    /// </summary>
    Task<CanonicalEntityVersion?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get a specific version for an entity type
    /// </summary>
    Task<CanonicalEntityVersion?> GetVersionAsync(CanonicalEntityType entityType, int version);
    
    /// <summary>
    /// Get the latest active version for an entity type
    /// </summary>
    Task<CanonicalEntityVersion?> GetLatestActiveVersionAsync(CanonicalEntityType entityType);
    
    /// <summary>
    /// Get all versions for an entity type
    /// </summary>
    Task<List<CanonicalEntityVersion>> GetAllVersionsAsync(CanonicalEntityType entityType);
    
    /// <summary>
    /// Create a new version
    /// </summary>
    Task<CanonicalEntityVersion> CreateAsync(CanonicalEntityVersion version);
    
    /// <summary>
    /// Update an existing version
    /// </summary>
    Task UpdateAsync(CanonicalEntityVersion version);
    
    /// <summary>
    /// Deprecate a version
    /// </summary>
    Task DeprecateVersionAsync(CanonicalEntityType entityType, int version);
}
