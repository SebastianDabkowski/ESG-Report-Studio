namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Represents a record of a finance data sync operation.
/// Tracks conflicts with manual entries and conflict resolution.
/// </summary>
public class FinanceSyncRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the connector that performed the sync
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// Correlation ID for tracing across distributed operations
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the sync operation
    /// </summary>
    public FinanceSyncStatus Status { get; set; } = FinanceSyncStatus.Pending;
    
    /// <summary>
    /// External identifier from the finance system (if applicable)
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// Raw JSON data from the external system
    /// </summary>
    public string? RawData { get; set; }
    
    /// <summary>
    /// Reason for rejection if status is Rejected
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Whether this sync attempted to overwrite approved data
    /// </summary>
    public bool OverwroteApprovedData { get; set; } = false;
    
    /// <summary>
    /// Whether there was a conflict with existing manual entry
    /// </summary>
    public bool ConflictDetected { get; set; } = false;
    
    /// <summary>
    /// Conflict resolution action taken (e.g., "PreservedManual", "AdminOverride", "NoConflict")
    /// </summary>
    public string? ConflictResolution { get; set; }
    
    /// <summary>
    /// User who approved override of manual data (if applicable)
    /// </summary>
    public string? ApprovedOverrideBy { get; set; }
    
    /// <summary>
    /// Timestamp when sync was executed
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User or system that initiated the sync
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the created or updated FinanceEntity
    /// </summary>
    public int? FinanceEntityId { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
    
    /// <summary>
    /// Navigation property to FinanceEntity
    /// </summary>
    public FinanceEntity? FinanceEntity { get; set; }
}

/// <summary>
/// Status of a finance sync operation
/// </summary>
public enum FinanceSyncStatus
{
    /// <summary>
    /// Sync is pending
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Sync completed successfully
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Sync failed due to error
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Record was rejected (validation failed, missing required fields, etc.)
    /// </summary>
    Rejected = 3,
    
    /// <summary>
    /// Sync skipped due to conflict with approved data
    /// </summary>
    ConflictPreserved = 4
}
