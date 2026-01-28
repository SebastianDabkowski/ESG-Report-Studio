namespace ARP.ESG_ReportStudio.API.Reporting;

/// <summary>
/// Represents a user in the system.
/// </summary>
public sealed class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // admin, report-owner, contributor, auditor
}

public sealed class ReportingPeriod
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ReportingMode { get; set; } = "simplified";
    public string ReportScope { get; set; } = "single-company";
    public string Status { get; set; } = "active";
    public string CreatedAt { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
}

public class ReportSection
{
    public string Id { get; set; } = string.Empty;
    public string PeriodId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "environmental";
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Completeness { get; set; } = "empty";
    public string? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int Order { get; set; }
    
    /// <summary>
    /// Catalog code reference (e.g., "ENV-001", "SOC-001") for stable identification across versions.
    /// </summary>
    public string? CatalogCode { get; set; }
}

public sealed class SectionSummary : ReportSection
{
    public int DataPointCount { get; set; }
    public int EvidenceCount { get; set; }
    public int GapCount { get; set; }
    public int AssumptionCount { get; set; }
    public int CompletenessPercentage { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress status derived from data point statuses.
    /// Values: "not-started", "in-progress", "blocked", "completed"
    /// Rules:
    /// - not-started: No data points exist or all are "missing"
    /// - in-progress: Has data points, some complete/incomplete, none blocked
    /// - blocked: Has any data points with reviewStatus "changes-requested"
    /// - completed: All required data points are "complete" or "not applicable"
    /// </summary>
    public string ProgressStatus { get; set; } = "not-started";
}

public sealed class CreateReportingPeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ReportingMode { get; set; } = "simplified";
    public string ReportScope { get; set; } = "single-company";
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    
    /// <summary>
    /// Optional ID of a previous reporting period to copy ownership mappings from.
    /// When provided, ownership mappings are copied to matching sections/data points based on catalog codes.
    /// </summary>
    public string? CopyOwnershipFromPeriodId { get; set; }
}

public sealed class UpdateReportingPeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ReportingMode { get; set; } = "simplified";
    public string ReportScope { get; set; } = "single-company";
}

/// <summary>
/// Request to update the owner of a report section.
/// </summary>
public sealed class UpdateSectionOwnerRequest
{
    /// <summary>
    /// User ID of the new owner.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person making the change (for audit logging).
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note explaining the reason for the change.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Request to update the owner of multiple report sections in bulk.
/// </summary>
public sealed class BulkUpdateSectionOwnerRequest
{
    /// <summary>
    /// IDs of the sections to update.
    /// </summary>
    public List<string> SectionIds { get; set; } = new();
    
    /// <summary>
    /// User ID of the new owner.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person making the change (for audit logging).
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note explaining the reason for the change.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Result of updating a section owner, including notification information.
/// </summary>
public sealed class UpdateSectionOwnerResult
{
    public ReportSection? Section { get; set; }
    public User? OldOwner { get; set; }
    public User? NewOwner { get; set; }
    public User? ChangedBy { get; set; }
}

/// <summary>
/// Result of a bulk section owner update operation.
/// </summary>
public sealed class BulkUpdateSectionOwnerResult
{
    /// <summary>
    /// Sections that were successfully updated.
    /// </summary>
    public List<ReportSection> UpdatedSections { get; set; } = new();
    
    /// <summary>
    /// Section IDs that were skipped due to permission or validation errors.
    /// </summary>
    public List<BulkUpdateFailure> SkippedSections { get; set; } = new();
    
    /// <summary>
    /// Section owner updates for notification purposes.
    /// </summary>
    public List<SectionOwnerUpdate> OwnerUpdates { get; set; } = new();
}

/// <summary>
/// Details about a section owner update for notification purposes.
/// </summary>
public sealed class SectionOwnerUpdate
{
    public ReportSection Section { get; set; } = null!;
    public User? OldOwner { get; set; }
    public User? NewOwner { get; set; }
}

/// <summary>
/// Details about a section that failed to update in a bulk operation.
/// </summary>
public sealed class BulkUpdateFailure
{
    /// <summary>
    /// ID of the section that failed to update.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason why the section was skipped.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents ownership information for a user with their assigned sections.
/// </summary>
public sealed class OwnerAssignment
{
    /// <summary>
    /// User ID of the owner. Empty string for unassigned sections.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the owner. "Unassigned" for sections without an owner.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email of the owner. Empty string for unassigned sections.
    /// </summary>
    public string OwnerEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Sections assigned to this owner.
    /// </summary>
    public List<SectionSummary> Sections { get; set; } = new();
    
    /// <summary>
    /// Count of data points owned by this owner across all their sections.
    /// </summary>
    public int TotalDataPoints { get; set; }
}

/// <summary>
/// Responsibility matrix showing all sections grouped by owner.
/// </summary>
public sealed class ResponsibilityMatrix
{
    /// <summary>
    /// Assignments grouped by owner.
    /// </summary>
    public List<OwnerAssignment> Assignments { get; set; } = new();
    
    /// <summary>
    /// Total number of sections.
    /// </summary>
    public int TotalSections { get; set; }
    
    /// <summary>
    /// Number of sections without an assigned owner.
    /// </summary>
    public int UnassignedSections { get; set; }
    
    /// <summary>
    /// Reporting period ID for this matrix.
    /// </summary>
    public string? PeriodId { get; set; }
}

public sealed class ReportingDataSnapshot
{
    public Organization? Organization { get; set; }
    public IReadOnlyList<ReportingPeriod> Periods { get; set; } = Array.Empty<ReportingPeriod>();
    public IReadOnlyList<ReportSection> Sections { get; set; } = Array.Empty<ReportSection>();
    public IReadOnlyList<SectionSummary> SectionSummaries { get; set; } = Array.Empty<SectionSummary>();
    public IReadOnlyList<OrganizationalUnit> OrganizationalUnits { get; set; } = Array.Empty<OrganizationalUnit>();
}

/// <summary>
/// Represents a template section in the catalog that can be used to create sections in reporting periods.
/// </summary>
public sealed class SectionCatalogItem
{
    /// <summary>
    /// Unique identifier for the catalog item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display title of the section.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Unique code identifier for the section (e.g., "ENV-001", "SOC-001").
    /// Must be unique across all catalog items.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category of the section. Must be one of: "environmental", "social", "governance".
    /// </summary>
    public string Category { get; set; } = "environmental";

    /// <summary>
    /// Detailed description of what the section covers.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the section is deprecated and should not be used in new reporting periods.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// ISO 8601 timestamp of when the catalog item was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// ISO 8601 timestamp of when the catalog item was deprecated, if applicable.
    /// </summary>
    public string? DeprecatedAt { get; set; }
}

/// <summary>
/// Request to create a new section catalog item.
/// </summary>
public sealed class CreateSectionCatalogItemRequest
{
    /// <summary>
    /// Display title of the section.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Unique code identifier for the section (e.g., "ENV-001", "SOC-001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category of the section. Must be one of: "environmental", "social", "governance".
    /// </summary>
    public string Category { get; set; } = "environmental";

    /// <summary>
    /// Detailed description of what the section covers.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing section catalog item.
/// Note: This does not change the IsDeprecated status. Use the deprecate endpoint for that.
/// </summary>
public sealed class UpdateSectionCatalogItemRequest
{
    /// <summary>
    /// Display title of the section.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Unique code identifier for the section (e.g., "ENV-001", "SOC-001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category of the section. Must be one of: "environmental", "social", "governance".
    /// </summary>
    public string Category { get; set; } = "environmental";

    /// <summary>
    /// Detailed description of what the section covers.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an ESG data point entry with metadata for auditability.
/// </summary>
public sealed class DataPoint
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Type { get; set; } = "narrative";
    public string? Classification { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public List<string> ContributorIds { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string? Assumptions { get; set; }
    public string CompletenessStatus { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "draft";
    public string? ReviewedBy { get; set; }
    public string? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string> EvidenceIds { get; set; } = new();
    public string? Deadline { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockerReason { get; set; }
    public string? BlockerDueDate { get; set; }
}

/// <summary>
/// Request to create a new data point.
/// </summary>
public sealed class CreateDataPointRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string Type { get; set; } = "narrative";
    public string? Classification { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public List<string> ContributorIds { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string? Assumptions { get; set; }
    public string CompletenessStatus { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "draft";
    public string? Deadline { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockerReason { get; set; }
    public string? BlockerDueDate { get; set; }
}

/// <summary>
/// Request to update an existing data point.
/// </summary>
public sealed class UpdateDataPointRequest
{
    public string Type { get; set; } = "narrative";
    public string? Classification { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public List<string> ContributorIds { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string? Assumptions { get; set; }
    public string CompletenessStatus { get; set; } = string.Empty;
    public string? ReviewStatus { get; set; }
    public string? ChangeNote { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Deadline { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockerReason { get; set; }
    public string? BlockerDueDate { get; set; }
}

/// <summary>
/// Request to approve a data point.
/// </summary>
public sealed class ApproveDataPointRequest
{
    /// <summary>
    /// User ID of the reviewer approving the data point.
    /// </summary>
    public string ReviewedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional comments from the reviewer.
    /// </summary>
    public string? ReviewComments { get; set; }
}

/// <summary>
/// Request to request changes on a data point.
/// </summary>
public sealed class RequestChangesRequest
{
    /// <summary>
    /// User ID of the reviewer requesting changes.
    /// </summary>
    public string ReviewedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Feedback explaining what changes are needed.
    /// </summary>
    public string ReviewComments { get; set; } = string.Empty;
}

/// <summary>
/// Request to update the completeness status of a data point.
/// </summary>
public sealed class UpdateDataPointStatusRequest
{
    /// <summary>
    /// The new completeness status. Must be one of: "missing", "incomplete", "complete", "not applicable".
    /// </summary>
    public string CompletenessStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person making the change (for audit logging).
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note explaining the reason for the change.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Represents a note or comment on a data point for internal accountability tracking.
/// </summary>
public sealed class DataPointNote
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new note on a data point.
/// </summary>
public sealed class CreateDataPointNoteRequest
{
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a missing required field for completion.
/// </summary>
public sealed class MissingFieldDetail
{
    /// <summary>
    /// Name of the missing field.
    /// </summary>
    public string Field { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of why this field is required.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result when a status update is blocked due to validation errors.
/// </summary>
public sealed class StatusValidationError
{
    /// <summary>
    /// Overall error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// List of missing fields preventing the status change.
    /// </summary>
    public List<MissingFieldDetail> MissingFields { get; set; } = new();
}

/// <summary>
/// Represents evidence supporting an ESG data point.
/// </summary>
public sealed class Evidence
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? SourceUrl { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
    public List<string> LinkedDataPoints { get; set; } = new();
}

/// <summary>
/// Represents an assumption or estimation for an ESG data point.
/// </summary>
public sealed class Assumption
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string? DataPointId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Represents a data gap that needs to be addressed.
/// </summary>
public sealed class Gap
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = "medium";
    public string? ImprovementPlan { get; set; }
    public string? TargetDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public bool Resolved { get; set; }
}

/// <summary>
/// Request to create new evidence.
/// </summary>
public sealed class CreateEvidenceRequest
{
    /// <summary>
    /// ID of the section this evidence belongs to.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>
    /// Title of the evidence.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the evidence.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional URL to external source. Must use http or https protocol.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// User ID of the person uploading the evidence.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to link evidence to a data point.
/// </summary>
public sealed class LinkEvidenceRequest
{
    /// <summary>
    /// ID of the data point to link to.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a validation rule that can be applied to data points.
/// </summary>
public sealed class ValidationRule
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty; // "non-negative", "required-unit", "allowed-units", "value-within-period"
    public string? TargetField { get; set; } // "value", "unit", etc.
    public string? Parameters { get; set; } // JSON-encoded parameters (e.g., allowed units list, date range)
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new validation rule.
/// </summary>
public sealed class CreateValidationRuleRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string? TargetField { get; set; }
    public string? Parameters { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing validation rule.
/// </summary>
public sealed class UpdateValidationRuleRequest
{
    public string RuleType { get; set; } = string.Empty;
    public string? TargetField { get; set; }
    public string? Parameters { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a single field change in an audit log entry.
/// </summary>
public sealed class FieldChange
{
    public string Field { get; init; } = string.Empty;
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
}

/// <summary>
/// Represents an immutable audit log entry for tracking changes.
/// </summary>
public sealed class AuditLogEntry
{
    public string Id { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string? ChangeNote { get; init; }
    public List<FieldChange> Changes { get; init; } = new();
}

/// <summary>
/// Configuration for reminder schedule.
/// </summary>
public sealed class ReminderConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string PeriodId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Days before deadline to send reminders (e.g., [7, 3, 1] means reminders at 7, 3, and 1 day before deadline)
    /// </summary>
    public List<int> DaysBeforeDeadline { get; set; } = new() { 7, 3, 1 };
    /// <summary>
    /// Frequency in hours to check for items needing reminders
    /// </summary>
    public int CheckFrequencyHours { get; set; } = 24;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Tracks reminder history for data points.
/// </summary>
public sealed class ReminderHistory
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string SentAt { get; set; } = string.Empty;
    public string ReminderType { get; set; } = string.Empty; // "missing", "incomplete"
    public int DaysUntilDeadline { get; set; }
    public string? DeadlineDate { get; set; }
    public bool EmailSent { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents completeness statistics for a specific category, organizational unit, or overall.
/// </summary>
public sealed class CompletenessBreakdown
{
    /// <summary>
    /// Identifier (e.g., category name "environmental", "social", "governance" or organizational unit ID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the breakdown dimension.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of data points with status "missing".
    /// </summary>
    public int MissingCount { get; set; }

    /// <summary>
    /// Number of data points with status "incomplete".
    /// </summary>
    public int IncompleteCount { get; set; }

    /// <summary>
    /// Number of data points with status "complete".
    /// </summary>
    public int CompleteCount { get; set; }

    /// <summary>
    /// Number of data points with status "not applicable".
    /// </summary>
    public int NotApplicableCount { get; set; }

    /// <summary>
    /// Total count of data points.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Percentage of complete data points.
    /// </summary>
    public double CompletePercentage { get; set; }
}

/// <summary>
/// Aggregated completeness statistics for the dashboard.
/// </summary>
public sealed class CompletenessStats
{
    /// <summary>
    /// Overall completeness breakdown.
    /// </summary>
    public CompletenessBreakdown Overall { get; set; } = new();

    /// <summary>
    /// Completeness breakdown by E/S/G category.
    /// </summary>
    public List<CompletenessBreakdown> ByCategory { get; set; } = new();

    /// <summary>
    /// Completeness breakdown by organizational unit.
    /// </summary>
    public List<CompletenessBreakdown> ByOrganizationalUnit { get; set; } = new();
}

/// <summary>
/// Represents a notification sent to a user about ownership changes.
/// </summary>
public sealed class OwnerNotification
{
    /// <summary>
    /// Unique notification identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Recipient user ID.
    /// </summary>
    public string RecipientUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of notification.
    /// </summary>
    public string NotificationType { get; set; } = string.Empty; // "section-assigned", "section-removed", "datapoint-assigned", "datapoint-removed"
    
    /// <summary>
    /// ID of the entity this notification is about (section or data point ID).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity (ReportSection or DataPoint).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the section or data point.
    /// </summary>
    public string EntityTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// User who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the user who made the change.
    /// </summary>
    public string ChangedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// When the notification was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// Whether email was sent successfully.
    /// </summary>
    public bool EmailSent { get; set; } = false;
}

/// <summary>
/// Configuration for escalating overdue items per reporting period.
/// </summary>
public sealed class EscalationConfiguration
{
    /// <summary>
    /// Unique configuration identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Reporting period this configuration applies to.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether escalation is enabled for this period.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Days after deadline to escalate (e.g., [3, 7] means escalate at 3 and 7 days overdue).
    /// </summary>
    public List<int> DaysAfterDeadline { get; set; } = new() { 3, 7 };
    
    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// When the configuration was last updated.
    /// </summary>
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Tracks escalation history for overdue data points.
/// </summary>
public sealed class EscalationHistory
{
    /// <summary>
    /// Unique escalation record identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Data point that was escalated.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner who was notified.
    /// </summary>
    public string OwnerUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner email address.
    /// </summary>
    public string OwnerEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Administrator/approver who was notified.
    /// </summary>
    public string? EscalatedToUserId { get; set; }
    
    /// <summary>
    /// Administrator/approver email address.
    /// </summary>
    public string? EscalatedToEmail { get; set; }
    
    /// <summary>
    /// When the escalation notification was sent.
    /// </summary>
    public string SentAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of days the item was overdue when escalated.
    /// </summary>
    public int DaysOverdue { get; set; }
    
    /// <summary>
    /// The deadline that was missed.
    /// </summary>
    public string DeadlineDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether email to owner was sent successfully.
    /// </summary>
    public bool OwnerEmailSent { get; set; }
    
    /// <summary>
    /// Whether email to administrator was sent successfully.
    /// </summary>
    public bool AdminEmailSent { get; set; }
    
    /// <summary>
    /// Error message if email sending failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Readiness report showing ownership completeness and data completion metrics.
/// </summary>
public sealed class ReadinessReport
{
    /// <summary>
    /// Reporting period ID for this report.
    /// </summary>
    public string? PeriodId { get; set; }

    /// <summary>
    /// Overall readiness metrics.
    /// </summary>
    public ReadinessMetrics Metrics { get; set; } = new();

    /// <summary>
    /// List of items (sections/data points) with their readiness status.
    /// </summary>
    public List<ReadinessItem> Items { get; set; } = new();
}

/// <summary>
/// Metrics for readiness reporting.
/// </summary>
public sealed class ReadinessMetrics
{
    /// <summary>
    /// Percentage of items (sections/data points) that have owners assigned (0-100).
    /// </summary>
    public int OwnershipPercentage { get; set; }

    /// <summary>
    /// Percentage of items that are completed (0-100).
    /// </summary>
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// Count of items that are blocked.
    /// </summary>
    public int BlockedCount { get; set; }

    /// <summary>
    /// Count of items that are overdue.
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// Total number of items in the report.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of items with owners.
    /// </summary>
    public int ItemsWithOwners { get; set; }

    /// <summary>
    /// Number of completed items.
    /// </summary>
    public int CompletedItems { get; set; }
}

/// <summary>
/// Individual item in the readiness report.
/// </summary>
public sealed class ReadinessItem
{
    /// <summary>
    /// Item ID (section or data point ID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Item type: "section" or "datapoint".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Item title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// ESG category: "environmental", "social", or "governance".
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Owner ID (empty if unassigned).
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Owner name (empty if unassigned).
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Progress status: "not-started", "in-progress", "blocked", "completed".
    /// </summary>
    public string ProgressStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the item is blocked.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Whether the item is overdue.
    /// </summary>
    public bool IsOverdue { get; set; }

    /// <summary>
    /// Deadline for the item (ISO 8601).
    /// </summary>
    public string? Deadline { get; set; }

    /// <summary>
    /// Completeness percentage (0-100).
    /// </summary>
    public int CompletenessPercentage { get; set; }
}
