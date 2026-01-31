namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents HR data imported from an external HR system
/// </summary>
public class HREntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector used for import
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// External identifier from the HR system
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of HR entity (e.g., "Employee", "Department", "OrgUnit")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON data containing the imported HR fields
    /// </summary>
    public string Data { get; set; } = "{}";
    
    /// <summary>
    /// JSON data containing the mapped ESG fields
    /// </summary>
    public string MappedData { get; set; } = "{}";
    
    /// <summary>
    /// Whether this entity has been approved for use in ESG reporting
    /// </summary>
    public bool IsApproved { get; set; } = false;
    
    /// <summary>
    /// When the entity was imported
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
}
