namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents financial data imported from an external finance/ERP system.
/// Stores data in staging area with provenance metadata for auditability.
/// </summary>
public class FinanceEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector used for import
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// External identifier from the finance system
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of financial entity (e.g., "Spend", "Revenue", "CapEx", "OpEx", "Supplier")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON data containing the imported financial fields
    /// </summary>
    public string Data { get; set; } = "{}";
    
    /// <summary>
    /// JSON data containing the mapped ESG fields
    /// </summary>
    public string MappedData { get; set; } = "{}";
    
    /// <summary>
    /// Whether this entity has been approved for use in ESG reporting.
    /// Manual entries with IsApproved=true cannot be automatically overwritten.
    /// </summary>
    public bool IsApproved { get; set; } = false;
    
    /// <summary>
    /// Source system identifier for provenance tracking
    /// </summary>
    public string SourceSystem { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when data was extracted from source system
    /// </summary>
    public DateTime? ExtractTimestamp { get; set; }
    
    /// <summary>
    /// Import job identifier for auditability
    /// </summary>
    public string ImportJobId { get; set; } = string.Empty;
    
    /// <summary>
    /// When the entity was imported into staging
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Reference to the canonical entity this finance entity maps to (optional)
    /// Enables multiple external systems to map into same canonical concepts
    /// </summary>
    public int? CanonicalEntityId { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
    
    /// <summary>
    /// Navigation property to CanonicalEntity
    /// </summary>
    public CanonicalEntity? CanonicalEntity { get; set; }
}
