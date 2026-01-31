namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Defines a standard attribute for a canonical entity type.
/// Used to document the canonical schema and support mapping configuration.
/// </summary>
public class CanonicalAttribute
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type of entity this attribute belongs to
    /// </summary>
    public CanonicalEntityType EntityType { get; set; }
    
    /// <summary>
    /// Schema version this attribute was introduced in
    /// </summary>
    public int SchemaVersion { get; set; }
    
    /// <summary>
    /// Name of the attribute in the canonical model
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Data type of the attribute (string, number, boolean, date, array, object)
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this attribute is required
    /// </summary>
    public bool IsRequired { get; set; } = false;
    
    /// <summary>
    /// Human-readable description of the attribute
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Example values for this attribute
    /// </summary>
    public string? ExampleValues { get; set; }
    
    /// <summary>
    /// Validation rules as JSON (e.g., min/max, regex, enum values)
    /// </summary>
    public string? ValidationRules { get; set; }
    
    /// <summary>
    /// Default value if not provided
    /// </summary>
    public string? DefaultValue { get; set; }
    
    /// <summary>
    /// Whether this attribute was deprecated in a later version
    /// </summary>
    public bool IsDeprecated { get; set; } = false;
    
    /// <summary>
    /// Schema version where this attribute was deprecated (if applicable)
    /// </summary>
    public int? DeprecatedInVersion { get; set; }
    
    /// <summary>
    /// Replacement attribute name if deprecated
    /// </summary>
    public string? ReplacedBy { get; set; }
    
    /// <summary>
    /// When this attribute definition was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this attribute definition was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
