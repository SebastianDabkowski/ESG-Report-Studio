namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Defines how an external system's fields map to canonical entity attributes.
/// Multiple connectors can define mappings to the same canonical attributes.
/// </summary>
public class CanonicalMapping
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector this mapping applies to
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// Type of canonical entity this mapping targets
    /// </summary>
    public CanonicalEntityType TargetEntityType { get; set; }
    
    /// <summary>
    /// Target schema version
    /// </summary>
    public int TargetSchemaVersion { get; set; }
    
    /// <summary>
    /// External field name from the source system
    /// </summary>
    public string ExternalField { get; set; } = string.Empty;
    
    /// <summary>
    /// Canonical attribute name this external field maps to
    /// </summary>
    public string CanonicalAttribute { get; set; } = string.Empty;
    
    /// <summary>
    /// Transformation type to apply (direct, sum, average, lookup, custom)
    /// </summary>
    public string TransformationType { get; set; } = "direct";
    
    /// <summary>
    /// Transformation parameters as JSON (e.g., lookup table, calculation formula)
    /// </summary>
    public string? TransformationParams { get; set; }
    
    /// <summary>
    /// Whether this mapping is required (failure to map causes rejection)
    /// </summary>
    public bool IsRequired { get; set; } = false;
    
    /// <summary>
    /// Default value to use if external field is not present
    /// </summary>
    public string? DefaultValue { get; set; }
    
    /// <summary>
    /// Priority order for this mapping (used when multiple mappings target same attribute)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Whether this mapping is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Notes or documentation for this mapping
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// When this mapping was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who created this mapping
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When this mapping was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Who last updated this mapping
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
}
