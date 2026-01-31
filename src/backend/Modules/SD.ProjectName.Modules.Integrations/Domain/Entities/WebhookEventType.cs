namespace SD.ProjectName.Modules.Integrations.Domain.Entities;

/// <summary>
/// Catalogue of supported webhook event types
/// </summary>
public static class WebhookEventType
{
    /// <summary>
    /// Triggered when data points are created or updated
    /// </summary>
    public const string DataChange = "data.changed";
    
    /// <summary>
    /// Triggered when an approval request is created
    /// </summary>
    public const string ApprovalRequested = "approval.requested";
    
    /// <summary>
    /// Triggered when an approval is granted
    /// </summary>
    public const string ApprovalGranted = "approval.granted";
    
    /// <summary>
    /// Triggered when an approval is rejected
    /// </summary>
    public const string ApprovalRejected = "approval.rejected";
    
    /// <summary>
    /// Triggered when a report export is initiated
    /// </summary>
    public const string ExportStarted = "export.started";
    
    /// <summary>
    /// Triggered when a report export is completed
    /// </summary>
    public const string ExportCompleted = "export.completed";
    
    /// <summary>
    /// Triggered when a report export fails
    /// </summary>
    public const string ExportFailed = "export.failed";
    
    /// <summary>
    /// Get all supported event types
    /// </summary>
    public static readonly string[] AllEventTypes =
    {
        DataChange,
        ApprovalRequested,
        ApprovalGranted,
        ApprovalRejected,
        ExportStarted,
        ExportCompleted,
        ExportFailed
    };
}
