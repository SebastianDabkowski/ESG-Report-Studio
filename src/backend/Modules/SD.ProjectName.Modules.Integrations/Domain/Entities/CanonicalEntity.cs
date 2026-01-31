namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents a canonical internal entity that multiple external systems can map into.
/// Provides a unified data model for HR, Finance, and other integration data.
/// </summary>
public class CanonicalEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type of canonical entity (e.g., Employee, Department, Spend, Revenue)
    /// </summary>
    public CanonicalEntityType EntityType { get; set; }
    
    /// <summary>
    /// Version of the canonical schema this entity conforms to
    /// </summary>
    public int SchemaVersion { get; set; }
    
    /// <summary>
    /// Unique external identifier (optional, may be null for entities created from multiple sources)
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// JSON data containing the canonical entity attributes
    /// Structure is defined by the schema version
    /// </summary>
    public string Data { get; set; } = "{}";
    
    /// <summary>
    /// Source system that provided this data (for provenance tracking)
    /// </summary>
    public string SourceSystem { get; set; } = string.Empty;
    
    /// <summary>
    /// Version identifier from source system (e.g., API version, export version)
    /// </summary>
    public string? SourceVersion { get; set; }
    
    /// <summary>
    /// Timestamp when data was imported into the canonical model
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Import job identifier for batch import tracking
    /// </summary>
    public string? ImportedByJobId { get; set; }
    
    /// <summary>
    /// Vendor-specific extension data that doesn't fit into canonical schema
    /// Stored as JSON to avoid leaking vendor-specific fields into core domain
    /// </summary>
    public string? VendorExtensions { get; set; }
    
    /// <summary>
    /// Whether this entity has been validated and approved for use
    /// </summary>
    public bool IsApproved { get; set; } = false;
    
    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// User who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Navigation property to schema version
    /// </summary>
    public CanonicalEntityVersion? Schema { get; set; }
}

/// <summary>
/// Types of canonical entities that can be imported from external systems
/// </summary>
public enum CanonicalEntityType
{
    // HR-related entities
    Employee = 1,
    Department = 2,
    OrganizationalUnit = 3,
    Position = 4,
    TrainingRecord = 5,
    
    // Finance-related entities
    Spend = 100,
    Revenue = 101,
    CapitalExpenditure = 102,
    OperationalExpenditure = 103,
    Supplier = 104,
    Invoice = 105,
    
    // Environmental entities (future)
    EnergyConsumption = 200,
    WaterUsage = 201,
    WasteGeneration = 202,
    EmissionsRecord = 203,
    
    // Social entities (future)
    SafetyIncident = 300,
    CommunityEngagement = 301,
    
    // Governance entities (future)
    ComplianceRecord = 400,
    PolicyDocument = 401
}
