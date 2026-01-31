namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

using SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Repository for managing canonical attribute definitions
/// </summary>
public interface ICanonicalAttributeRepository
{
    /// <summary>
    /// Get an attribute by ID
    /// </summary>
    Task<CanonicalAttribute?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get all attributes for an entity type and version
    /// </summary>
    Task<List<CanonicalAttribute>> GetAttributesAsync(CanonicalEntityType entityType, int schemaVersion);
    
    /// <summary>
    /// Get required attributes for an entity type and version
    /// </summary>
    Task<List<CanonicalAttribute>> GetRequiredAttributesAsync(CanonicalEntityType entityType, int schemaVersion);
    
    /// <summary>
    /// Get an attribute by name
    /// </summary>
    Task<CanonicalAttribute?> GetByNameAsync(CanonicalEntityType entityType, int schemaVersion, string attributeName);
    
    /// <summary>
    /// Create a new attribute
    /// </summary>
    Task<CanonicalAttribute> CreateAsync(CanonicalAttribute attribute);
    
    /// <summary>
    /// Update an existing attribute
    /// </summary>
    Task UpdateAsync(CanonicalAttribute attribute);
    
    /// <summary>
    /// Deprecate an attribute
    /// </summary>
    Task DeprecateAttributeAsync(int id, int deprecatedInVersion, string? replacedBy = null);
}
