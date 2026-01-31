namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Defines a version of a canonical entity schema.
/// Supports schema evolution while maintaining backward compatibility.
/// </summary>
public class CanonicalEntityVersion
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type of entity this version applies to
    /// </summary>
    public CanonicalEntityType EntityType { get; set; }
    
    /// <summary>
    /// Version number (incremental, e.g., 1, 2, 3)
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// JSON schema definition for this version
    /// Defines required and optional attributes
    /// </summary>
    public string SchemaDefinition { get; set; } = "{}";
    
    /// <summary>
    /// Human-readable description of this version
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this version is currently active for new mappings
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this version is deprecated (still supported but not recommended)
    /// </summary>
    public bool IsDeprecated { get; set; } = false;
    
    /// <summary>
    /// Minimum version that this version is backward compatible with
    /// Null means no backward compatibility guarantees
    /// </summary>
    public int? BackwardCompatibleWithVersion { get; set; }
    
    /// <summary>
    /// Migration rules to convert from previous versions to this version
    /// JSON array of transformation rules
    /// </summary>
    public string? MigrationRules { get; set; }
    
    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who created this version
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When this version was deprecated (if applicable)
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to canonical entities using this version
    /// </summary>
    public ICollection<CanonicalEntity> Entities { get; set; } = new List<CanonicalEntity>();
}
