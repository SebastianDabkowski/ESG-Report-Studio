namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Metadata for an integration job execution.
/// Aggregates statistics from individual integration log entries.
/// </summary>
public class IntegrationJobMetadata
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier for this job execution
    /// </summary>
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Reference to the connector that executed this job
    /// </summary>
    public int ConnectorId { get; set; }
    
    /// <summary>
    /// Correlation ID for tracing this job across systems
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of job (e.g., "HRSync", "FinanceSync", "ManualImport")
    /// </summary>
    public string JobType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the job
    /// </summary>
    public IntegrationJobStatus Status { get; set; }
    
    /// <summary>
    /// When the job started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job completed (null if still in progress)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Total duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }
    
    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int TotalRecords { get; set; } = 0;
    
    /// <summary>
    /// Number of records successfully imported/updated
    /// </summary>
    public int SuccessCount { get; set; } = 0;
    
    /// <summary>
    /// Number of records that failed
    /// </summary>
    public int FailureCount { get; set; } = 0;
    
    /// <summary>
    /// Number of records skipped (e.g., duplicates, validation errors)
    /// </summary>
    public int SkippedCount { get; set; } = 0;
    
    /// <summary>
    /// Aggregated error messages from the job
    /// </summary>
    public string? ErrorSummary { get; set; }
    
    /// <summary>
    /// User or service that initiated this job
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional notes or context about this job execution
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Navigation property to Connector
    /// </summary>
    public Connector? Connector { get; set; }
}

/// <summary>
/// Status of an integration job
/// </summary>
public enum IntegrationJobStatus
{
    /// <summary>
    /// Job is queued but not yet started
    /// </summary>
    Queued = 0,
    
    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 1,
    
    /// <summary>
    /// Job completed successfully (all records processed)
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Job completed with some failures
    /// </summary>
    CompletedWithErrors = 3,
    
    /// <summary>
    /// Job failed completely
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled = 5
}
