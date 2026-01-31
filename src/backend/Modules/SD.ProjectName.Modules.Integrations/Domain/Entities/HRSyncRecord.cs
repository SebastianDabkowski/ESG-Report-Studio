namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Tracks HR synchronization operations and records rejections
/// </summary>
public class HRSyncRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector used for sync
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the sync operation
    /// </summary>
    public HRSyncStatus Status { get; set; }
    
    /// <summary>
    /// External identifier from the HR system (if applicable)
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// Raw data from HR system
    /// </summary>
    public string? RawData { get; set; }
    
    /// <summary>
    /// Rejection reason if mapping failed
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Whether this record overwrote existing approved data
    /// </summary>
    public bool OverwroteApprovedData { get; set; } = false;
    
    /// <summary>
    /// When the sync was initiated
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User or service that initiated the sync
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the created/updated HREntity (if successful)
    /// </summary>
    public int? HREntityId { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
    
    /// <summary>
    /// Navigation property to HREntity
    /// </summary>
    public HREntity? HREntity { get; set; }
}

/// <summary>
/// Status of an HR sync operation
/// </summary>
public enum HRSyncStatus
{
    /// <summary>
    /// Sync is pending
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Sync succeeded and data was imported/updated
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Sync failed due to technical error
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Record was rejected because it could not be mapped
    /// </summary>
    Rejected = 3
}
