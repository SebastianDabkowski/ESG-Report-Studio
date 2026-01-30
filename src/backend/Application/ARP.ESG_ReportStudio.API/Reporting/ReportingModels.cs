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
    
    /// <summary>
    /// When true and CopyOwnershipFromPeriodId is provided, carries forward open gaps, active assumptions,
    /// and active remediation plans from the previous period to the new period.
    /// </summary>
    public bool CarryForwardGapsAndAssumptions { get; set; }
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
/// Represents a source of input data used in an estimate calculation.
/// Supports both internal documents and external evidence references.
/// </summary>
public sealed class EstimateInputSource
{
    /// <summary>
    /// Type of source: 'internal-document', 'uploaded-evidence', 'external-url', 'assumption', or 'other'.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference identifier (e.g., document ID, evidence ID, assumption ID, or URL).
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the source.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a source data reference for narrative content provenance tracking.
/// Links narrative statements to their underlying source data for auditability.
/// </summary>
public sealed class NarrativeSourceReference
{
    /// <summary>
    /// Type of source: 'data-point', 'evidence', 'assumption', 'external-system', 'uploaded-file', or 'other'.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference identifier to the source record (e.g., data point ID, evidence ID, file path, system identifier).
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the source.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the origin system or file where the source data originated.
    /// Examples: "HR System", "Finance ERP", "Sustainability Report 2023.xlsx"
    /// </summary>
    public string? OriginSystem { get; set; }
    
    /// <summary>
    /// User ID of the owner of this source data.
    /// </summary>
    public string? OwnerId { get; set; }
    
    /// <summary>
    /// Name of the owner of this source data.
    /// </summary>
    public string? OwnerName { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when the source data was last updated.
    /// Used to detect when source data has changed and provenance needs review.
    /// </summary>
    public string? LastUpdated { get; set; }
    
    /// <summary>
    /// Optional snapshot or hash of the source value at the time of linking.
    /// Stored at publication time to detect changes in source data.
    /// </summary>
    public string? ValueSnapshot { get; set; }
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
    
    // Missing data tracking
    public bool IsMissing { get; set; }
    public string? MissingReason { get; set; }
    public string? MissingReasonCategory { get; set; }
    public string? MissingFlaggedBy { get; set; }
    public string? MissingFlaggedAt { get; set; }
    
    /// <summary>
    /// Type of estimate when InformationType is 'estimate'. Options: point, range, proxy-based, extrapolated.
    /// </summary>
    public string? EstimateType { get; set; }
    
    /// <summary>
    /// Description of the methodology used to derive the estimate.
    /// </summary>
    public string? EstimateMethod { get; set; }
    
    /// <summary>
    /// Confidence level in the accuracy of the estimate. Options: low, medium, high.
    /// </summary>
    public string? ConfidenceLevel { get; set; }
    
    /// <summary>
    /// Gap closure workflow status. Options: "missing", "estimated", "provided".
    /// Tracks the progression from missing data → estimate → verified source data.
    /// </summary>
    public string? GapStatus { get; set; }
    
    /// <summary>
    /// Populated when transitioning from 'estimated' to 'provided' status.
    /// Preserves the estimate details (EstimateType, EstimateMethod, ConfidenceLevel, etc.) for historical reference.
    /// </summary>
    public string? PreviousEstimateSnapshot { get; set; }
    
    // Data Provenance fields for Estimates
    /// <summary>
    /// List of input sources used to derive the estimate. Each source contains reference information.
    /// Required when InformationType is 'estimate' and multiple sources are used.
    /// </summary>
    public List<EstimateInputSource> EstimateInputSources { get; set; } = new();
    
    /// <summary>
    /// Detailed inputs used in the estimate calculation (e.g., "Energy consumption: 1000 kWh, Emission factor: 0.5 kg CO2/kWh").
    /// </summary>
    public string? EstimateInputs { get; set; }
    
    /// <summary>
    /// User who created/authored the estimate. For audit trail.
    /// </summary>
    public string? EstimateAuthor { get; set; }
    
    /// <summary>
    /// Timestamp when the estimate was created. For audit trail.
    /// </summary>
    public string? EstimateCreatedAt { get; set; }
    
    // Narrative Provenance fields (for all content types, especially narrative)
    /// <summary>
    /// List of source data references that support this statement/narrative.
    /// Enables traceability from report statements to underlying evidence and data.
    /// </summary>
    public List<NarrativeSourceReference> SourceReferences { get; set; } = new();
    
    /// <summary>
    /// Hash or snapshot of source values at the time of publication.
    /// Used to detect when underlying source data has changed and provenance needs review.
    /// </summary>
    public string? PublicationSourceHash { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when source references were last verified/published.
    /// </summary>
    public string? ProvenanceLastVerified { get; set; }
    
    /// <summary>
    /// Flag indicating that source data has changed and this statement needs review.
    /// Set to true when source data is updated after publication.
    /// </summary>
    public bool ProvenanceNeedsReview { get; set; }
    
    /// <summary>
    /// Reason why provenance needs review (e.g., "Source data point updated", "Evidence file replaced").
    /// </summary>
    public string? ProvenanceReviewReason { get; set; }
    
    /// <summary>
    /// User ID who flagged the provenance for review.
    /// </summary>
    public string? ProvenanceFlaggedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when provenance was flagged for review.
    /// </summary>
    public string? ProvenanceFlaggedAt { get; set; }
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
    public bool IsMissing { get; set; }
    public string? MissingReason { get; set; }
    public string? MissingReasonCategory { get; set; }
    /// <summary>
    /// Type of estimate when InformationType is 'estimate'. Required for estimates. Options: point, range, proxy-based, extrapolated.
    /// </summary>
    public string? EstimateType { get; set; }
    /// <summary>
    /// Description of the methodology used to derive the estimate. Required for estimates.
    /// </summary>
    public string? EstimateMethod { get; set; }
    /// <summary>
    /// Confidence level in the accuracy of the estimate. Required for estimates. Options: low, medium, high.
    /// </summary>
    public string? ConfidenceLevel { get; set; }
    
    // Data Provenance fields for Estimates
    /// <summary>
    /// List of input sources used to derive the estimate.
    /// </summary>
    public List<EstimateInputSource> EstimateInputSources { get; set; } = new();
    
    /// <summary>
    /// Detailed inputs used in the estimate calculation.
    /// </summary>
    public string? EstimateInputs { get; set; }
    
    // Narrative Provenance fields
    /// <summary>
    /// List of source data references that support this statement/narrative.
    /// Optional: can be added to link narrative to source data for traceability.
    /// </summary>
    public List<NarrativeSourceReference> SourceReferences { get; set; } = new();
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
    public bool IsMissing { get; set; }
    public string? MissingReason { get; set; }
    public string? MissingReasonCategory { get; set; }
    /// <summary>
    /// Type of estimate when InformationType is 'estimate'. Required for estimates. Options: point, range, proxy-based, extrapolated.
    /// </summary>
    public string? EstimateType { get; set; }
    /// <summary>
    /// Description of the methodology used to derive the estimate. Required for estimates.
    /// </summary>
    public string? EstimateMethod { get; set; }
    /// <summary>
    /// Confidence level in the accuracy of the estimate. Required for estimates. Options: low, medium, high.
    /// </summary>
    public string? ConfidenceLevel { get; set; }
    
    // Data Provenance fields for Estimates
    /// <summary>
    /// List of input sources used to derive the estimate.
    /// </summary>
    public List<EstimateInputSource> EstimateInputSources { get; set; } = new();
    
    /// <summary>
    /// Detailed inputs used in the estimate calculation.
    /// </summary>
    public string? EstimateInputs { get; set; }
    
    // Narrative Provenance fields
    /// <summary>
    /// List of source data references that support this statement/narrative.
    /// Optional: can be added to link narrative to source data for traceability.
    /// </summary>
    public List<NarrativeSourceReference> SourceReferences { get; set; } = new();
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
/// Request to flag a data point as missing with a reason.
/// </summary>
public sealed class FlagMissingDataRequest
{
    /// <summary>
    /// User ID of the person flagging the data as missing.
    /// </summary>
    public string FlaggedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Category for why data is missing. Must be one of: "not-measured", "not-applicable", 
    /// "unavailable-from-supplier", "data-quality-issue", "system-limitation", "other".
    /// </summary>
    public string MissingReasonCategory { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed reason or additional context (free text).
    /// </summary>
    public string MissingReason { get; set; } = string.Empty;
}

/// <summary>
/// Request to unflag a data point (mark it as no longer missing).
/// </summary>
public sealed class UnflagMissingDataRequest
{
    /// <summary>
    /// User ID of the person unflagging the data.
    /// </summary>
    public string UnflaggedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note explaining why data is now available.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Request to transition a data point's gap status (Missing → Estimated → Provided).
/// </summary>
public sealed class TransitionGapStatusRequest
{
    /// <summary>
    /// User ID of the person transitioning the status.
    /// </summary>
    public string TransitionedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Target gap status. Must be: "missing", "estimated", or "provided".
    /// </summary>
    public string TargetStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// Note explaining the status transition.
    /// </summary>
    public string? ChangeNote { get; set; }
    
    /// <summary>
    /// When transitioning to "estimated", provide estimate type.
    /// Required when TargetStatus is "estimated".
    /// </summary>
    public string? EstimateType { get; set; }
    
    /// <summary>
    /// When transitioning to "estimated", provide methodology.
    /// Required when TargetStatus is "estimated".
    /// </summary>
    public string? EstimateMethod { get; set; }
    
    /// <summary>
    /// When transitioning to "estimated", provide confidence level.
    /// Required when TargetStatus is "estimated".
    /// </summary>
    public string? ConfidenceLevel { get; set; }
}

/// <summary>
/// Represents a historical record of gap status transitions.
/// Preserved in audit log for traceability.
/// </summary>
public sealed class GapStatusHistoryEntry
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string TransitionedBy { get; set; } = string.Empty;
    public string TransitionedByName { get; set; } = string.Empty;
    public string TransitionedAt { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
    
    /// <summary>
    /// Snapshot of estimate details if transitioning from "estimated" to "provided".
    /// Preserves the previous estimate for historical reference.
    /// </summary>
    public string? EstimateSnapshot { get; set; }
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
    
    // Chain-of-custody metadata
    public long? FileSize { get; set; }
    public string? Checksum { get; set; } // SHA-256 hash
    public string? ContentType { get; set; }
    public string IntegrityStatus { get; set; } = "not-checked"; // 'valid', 'failed', 'not-checked'
}

/// <summary>
/// Represents an access log entry for evidence file downloads.
/// </summary>
public sealed class EvidenceAccessLog
{
    public string Id { get; init; } = string.Empty;
    public string EvidenceId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string AccessedAt { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty; // 'download', 'view', 'validate'
    public string? Purpose { get; init; }
}

/// <summary>
/// Represents a source supporting an assumption.
/// </summary>
public sealed class AssumptionSource
{
    /// <summary>
    /// Type of source: 'internal-document', 'uploaded-evidence', 'external-url', or 'other'.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference identifier (e.g., document ID, evidence ID, or URL).
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the source.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an assumption or estimation for an ESG data point.
/// Supports reusability across multiple disclosures, versioning, and lifecycle management.
/// </summary>
public sealed class Assumption
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string? DataPointId { get; set; }
    
    // Core fields from acceptance criteria
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty; // e.g., "Company-wide", "Specific facility", "Product line"
    public string ValidityStartDate { get; set; } = string.Empty;
    public string ValidityEndDate { get; set; } = string.Empty;
    
    // Existing fields
    public string Methodology { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
    
    // Data Provenance fields
    /// <summary>
    /// Detailed rationale explaining why this assumption was made and how it was derived.
    /// </summary>
    public string? Rationale { get; set; }
    
    /// <summary>
    /// List of sources supporting this assumption (documents, evidence, external references).
    /// </summary>
    public List<AssumptionSource> Sources { get; set; } = new();
    
    // Lifecycle management
    public string Status { get; set; } = "active"; // 'active', 'deprecated', 'invalid'
    public string? ReplacementAssumptionId { get; set; } // Reference to replacement when deprecated
    public string? DeprecationJustification { get; set; } // Required when marked as invalid without replacement
    
    // Versioning for tracking updates
    public int Version { get; set; } = 1;
    public string? UpdatedBy { get; set; }
    public string? UpdatedAt { get; set; }
    
    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    
    // Linkage tracking - stores IDs of all data points using this assumption
    // This represents linked disclosures
    public List<string> LinkedDataPointIds { get; set; } = new();
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
/// Represents a simplification (e.g., boundary limitation) applied to the reporting scope.
/// Ensures transparency about scope constraints and their impact.
/// </summary>
public sealed class Simplification
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// List of affected entities (companies, subsidiaries, etc.) by this simplification.
    /// </summary>
    public List<string> AffectedEntities { get; set; } = new();
    
    /// <summary>
    /// List of affected sites (locations, facilities, etc.) by this simplification.
    /// </summary>
    public List<string> AffectedSites { get; set; } = new();
    
    /// <summary>
    /// List of affected processes (business processes, operations, etc.) by this simplification.
    /// </summary>
    public List<string> AffectedProcesses { get; set; } = new();
    
    /// <summary>
    /// Qualitative impact level: "low", "medium", or "high".
    /// </summary>
    public string ImpactLevel { get; set; } = "medium";
    
    /// <summary>
    /// Optional notes providing additional context about the impact assessment.
    /// </summary>
    public string? ImpactNotes { get; set; }
    
    /// <summary>
    /// Status of the simplification: "active" or "removed".
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// User who created this simplification.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when this simplification was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who last updated this simplification.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this simplification was last updated.
    /// </summary>
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a new assumption.
/// </summary>
public sealed class CreateAssumptionRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ValidityStartDate { get; set; } = string.Empty;
    public string ValidityEndDate { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
    public List<string> LinkedDataPointIds { get; set; } = new();
    
    // Data Provenance fields
    public string? Rationale { get; set; }
    public List<AssumptionSource> Sources { get; set; } = new();
}

/// <summary>
/// Request to update an existing assumption.
/// </summary>
public sealed class UpdateAssumptionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ValidityStartDate { get; set; } = string.Empty;
    public string ValidityEndDate { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
    public List<string> LinkedDataPointIds { get; set; } = new();
    
    // Data Provenance fields
    public string? Rationale { get; set; }
    public List<AssumptionSource> Sources { get; set; } = new();
}

/// <summary>
/// Request to deprecate an assumption.
/// </summary>
public sealed class DeprecateAssumptionRequest
{
    public string? ReplacementAssumptionId { get; set; }
    public string? Justification { get; set; }
}

/// <summary>
/// Request to create a new simplification.
/// </summary>
public sealed class CreateSimplificationRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedEntities { get; set; } = new();
    public List<string> AffectedSites { get; set; } = new();
    public List<string> AffectedProcesses { get; set; } = new();
    public string ImpactLevel { get; set; } = "medium";
    public string? ImpactNotes { get; set; }
}

/// <summary>
/// Request to update an existing simplification.
/// </summary>
public sealed class UpdateSimplificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedEntities { get; set; } = new();
    public List<string> AffectedSites { get; set; } = new();
    public List<string> AffectedProcesses { get; set; } = new();
    public string ImpactLevel { get; set; } = "medium";
    public string? ImpactNotes { get; set; }
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

    /// <summary>
    /// ID of the user performing the link/unlink operation.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
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
    public string UpdatedBy { get; set; } = string.Empty;
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

/// <summary>
/// Represents a remediation plan for addressing missing or estimated data items.
/// Links to gaps or assumptions that need resolution in future periods.
/// </summary>
public sealed class RemediationPlan
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the remediation plan.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of what needs to be remediated and why.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Target period for completion (e.g., "Q1 2026", "FY 2026").
    /// </summary>
    public string TargetPeriod { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person responsible for overseeing this remediation.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the owner (denormalized for display).
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Priority level: "low", "medium", or "high".
    /// </summary>
    public string Priority { get; set; } = "medium";
    
    /// <summary>
    /// Status of the remediation plan: "planned", "in-progress", "completed", "cancelled".
    /// </summary>
    public string Status { get; set; } = "planned";
    
    /// <summary>
    /// Optional reference to a Gap this plan addresses.
    /// </summary>
    public string? GapId { get; set; }
    
    /// <summary>
    /// Optional reference to an Assumption this plan addresses (e.g., to replace with actual data).
    /// </summary>
    public string? AssumptionId { get; set; }
    
    /// <summary>
    /// Optional reference to a DataPoint this plan addresses.
    /// </summary>
    public string? DataPointId { get; set; }
    
    /// <summary>
    /// When the plan was completed (if status is "completed").
    /// </summary>
    public string? CompletedAt { get; set; }
    
    /// <summary>
    /// User who completed the plan.
    /// </summary>
    public string? CompletedBy { get; set; }
    
    /// <summary>
    /// Audit fields.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a specific action within a remediation plan.
/// Actions can include tasks like gathering data, obtaining evidence, etc.
/// </summary>
public sealed class RemediationAction
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the remediation plan this action belongs to.
    /// </summary>
    public string RemediationPlanId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title/description of the action.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of what needs to be done.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person responsible for this action.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the owner (denormalized for display).
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Due date for this action (ISO 8601).
    /// </summary>
    public string DueDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the action: "pending", "in-progress", "completed", "cancelled".
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// When the action was completed.
    /// </summary>
    public string? CompletedAt { get; set; }
    
    /// <summary>
    /// User who completed the action.
    /// </summary>
    public string? CompletedBy { get; set; }
    
    /// <summary>
    /// Evidence IDs attached to this action (e.g., invoices, meter readings, HR exports).
    /// </summary>
    public List<string> EvidenceIds { get; set; } = new();
    
    /// <summary>
    /// Optional notes about completion or progress.
    /// </summary>
    public string? CompletionNotes { get; set; }
    
    /// <summary>
    /// Audit fields.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a new remediation plan.
/// </summary>
public sealed class CreateRemediationPlanRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetPeriod { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string? GapId { get; set; }
    public string? AssumptionId { get; set; }
    public string? DataPointId { get; set; }
}

/// <summary>
/// Request to update an existing remediation plan.
/// </summary>
public sealed class UpdateRemediationPlanRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetPeriod { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "planned";
}

/// <summary>
/// Request to mark a remediation plan as completed.
/// </summary>
public sealed class CompleteRemediationPlanRequest
{
    public string CompletedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new remediation action.
/// </summary>
public sealed class CreateRemediationActionRequest
{
    public string RemediationPlanId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing remediation action.
/// </summary>
public sealed class UpdateRemediationActionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
}

/// <summary>
/// Request to mark a remediation action as completed.
/// </summary>
public sealed class CompleteRemediationActionRequest
{
    public string CompletedBy { get; set; } = string.Empty;
    public string? CompletionNotes { get; set; }
    public List<string> EvidenceIds { get; set; } = new();
}

/// <summary>
/// Represents an approved exception for report completeness validation.
/// Allows controlled gaps with explicit justification and approval.
/// </summary>
public sealed class CompletionException
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to data point that has the exception (if applicable).
    /// </summary>
    public string? DataPointId { get; set; }
    
    /// <summary>
    /// Title of the exception.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of exception: "missing-data", "estimated-data", "simplified-scope", "other".
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed justification for why this exception is necessary.
    /// </summary>
    public string Justification { get; set; } = string.Empty;
    
    /// <summary>
    /// Status: "pending", "accepted", "rejected".
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// User who requested the exception.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when the exception was requested.
    /// </summary>
    public string RequestedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who approved the exception (required when status is "accepted").
    /// </summary>
    public string? ApprovedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when the exception was approved.
    /// </summary>
    public string? ApprovedAt { get; set; }
    
    /// <summary>
    /// User who rejected the exception (if applicable).
    /// </summary>
    public string? RejectedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when the exception was rejected.
    /// </summary>
    public string? RejectedAt { get; set; }
    
    /// <summary>
    /// Optional comments from approver/rejector.
    /// </summary>
    public string? ReviewComments { get; set; }
    
    /// <summary>
    /// ISO 8601 date when this exception expires (optional).
    /// After this date, the exception should be re-evaluated.
    /// </summary>
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// Request to create a new completion exception.
/// </summary>
public sealed class CreateCompletionExceptionRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string? DataPointId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// Request to approve a completion exception.
/// </summary>
public sealed class ApproveCompletionExceptionRequest
{
    public string ApprovedBy { get; set; } = string.Empty;
    public string? ReviewComments { get; set; }
}

/// <summary>
/// Request to reject a completion exception.
/// </summary>
public sealed class RejectCompletionExceptionRequest
{
    public string RejectedBy { get; set; } = string.Empty;
    public string ReviewComments { get; set; } = string.Empty;
}

/// <summary>
/// Completeness validation report with exceptions breakdown.
/// </summary>
public sealed class CompletenessValidationReport
{
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Sections broken down by completeness issues.
    /// </summary>
    public List<SectionCompletenessDetail> Sections { get; set; } = new();
    
    /// <summary>
    /// Summary statistics for the report.
    /// </summary>
    public CompletenessValidationSummary Summary { get; set; } = new();
}

/// <summary>
/// Completeness details for a specific section.
/// </summary>
public sealed class SectionCompletenessDetail
{
    public string SectionId { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// List of missing data points in this section.
    /// </summary>
    public List<DataPointSummary> MissingItems { get; set; } = new();
    
    /// <summary>
    /// List of estimated data points in this section.
    /// </summary>
    public List<DataPointSummary> EstimatedItems { get; set; } = new();
    
    /// <summary>
    /// List of simplified/boundary limited data points in this section.
    /// </summary>
    public List<DataPointSummary> SimplifiedItems { get; set; } = new();
    
    /// <summary>
    /// Accepted exceptions for this section.
    /// </summary>
    public List<CompletionException> AcceptedExceptions { get; set; } = new();
}

/// <summary>
/// Summary of a data point for validation reporting.
/// </summary>
public sealed class DataPointSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompletenessStatus { get; set; } = string.Empty;
    public string? MissingReason { get; set; }
    public string? EstimateType { get; set; }
    public string? ConfidenceLevel { get; set; }
}

/// <summary>
/// Summary statistics for completeness validation.
/// </summary>
public sealed class CompletenessValidationSummary
{
    public int TotalSections { get; set; }
    public int TotalDataPoints { get; set; }
    public int MissingCount { get; set; }
    public int EstimatedCount { get; set; }
    public int SimplifiedCount { get; set; }
    public int AcceptedExceptionsCount { get; set; }
    public int PendingExceptionsCount { get; set; }
    public double CompletenessPercentage { get; set; }
    public double CompletenessWithExceptionsPercentage { get; set; }
}

/// <summary>
/// Compiled report of gaps, estimates, assumptions, simplifications, and remediation plans.
/// Supports both auto-generated and manually edited narrative content.
/// </summary>
public sealed class GapsAndImprovementsReport
{
    /// <summary>
    /// Reporting period ID for this report.
    /// </summary>
    public string? PeriodId { get; set; }
    
    /// <summary>
    /// Overall summary metrics.
    /// </summary>
    public GapsAndImprovementsSummary Summary { get; set; } = new();
    
    /// <summary>
    /// Gaps and improvements grouped by section.
    /// </summary>
    public List<SectionGapsAndImprovements> Sections { get; set; } = new();
    
    /// <summary>
    /// Auto-generated narrative text for the report.
    /// </summary>
    public string AutoGeneratedNarrative { get; set; } = string.Empty;
    
    /// <summary>
    /// Manually edited narrative text (overrides auto-generated if present).
    /// </summary>
    public string? ManualNarrative { get; set; }
    
    /// <summary>
    /// Timestamp when report was generated.
    /// </summary>
    public string GeneratedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Summary metrics for gaps and improvements.
/// </summary>
public sealed class GapsAndImprovementsSummary
{
    public int TotalGaps { get; set; }
    public int ResolvedGaps { get; set; }
    public int UnresolvedGaps { get; set; }
    public int TotalAssumptions { get; set; }
    public int ActiveAssumptions { get; set; }
    public int DeprecatedAssumptions { get; set; }
    public int TotalSimplifications { get; set; }
    public int ActiveSimplifications { get; set; }
    public int TotalRemediationPlans { get; set; }
    public int CompletedRemediationPlans { get; set; }
    public int InProgressRemediationPlans { get; set; }
    public int TotalRemediationActions { get; set; }
    public int CompletedActions { get; set; }
    public int OverdueActions { get; set; }
}

/// <summary>
/// Gaps and improvements for a specific section.
/// </summary>
public sealed class SectionGapsAndImprovements
{
    public string SectionId { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Gaps identified in this section.
    /// </summary>
    public List<GapWithRemediation> Gaps { get; set; } = new();
    
    /// <summary>
    /// Assumptions used in this section.
    /// </summary>
    public List<AssumptionReference> Assumptions { get; set; } = new();
    
    /// <summary>
    /// Simplifications applied in this section.
    /// </summary>
    public List<SimplificationReference> Simplifications { get; set; } = new();
    
    /// <summary>
    /// Standalone remediation plans for this section.
    /// </summary>
    public List<RemediationPlanWithActions> RemediationPlans { get; set; } = new();
}

/// <summary>
/// Gap with associated remediation plan (if any).
/// </summary>
public sealed class GapWithRemediation
{
    public Gap Gap { get; set; } = new();
    public RemediationPlanWithActions? RemediationPlan { get; set; }
}

/// <summary>
/// Assumption with linked data point references.
/// </summary>
public sealed class AssumptionReference
{
    public Assumption Assumption { get; set; } = new();
    public List<string> LinkedDataPointTitles { get; set; } = new();
}

/// <summary>
/// Simplification with impact details.
/// </summary>
public sealed class SimplificationReference
{
    public Simplification Simplification { get; set; } = new();
}

/// <summary>
/// Remediation plan with its actions.
/// </summary>
public sealed class RemediationPlanWithActions
{
    public RemediationPlan Plan { get; set; } = new();
    public List<RemediationAction> Actions { get; set; } = new();
}

/// <summary>
/// Request to update the manual narrative for a gaps and improvements report.
/// </summary>
public sealed class UpdateGapsNarrativeRequest
{
    /// <summary>
    /// Manually edited narrative text. Set to null to revert to auto-generated content.
    /// </summary>
    public string? ManualNarrative { get; set; }
}

/// <summary>
/// Extended gap information including section and owner details for dashboard display.
/// </summary>
public sealed class GapDashboardItem
{
    public Gap Gap { get; set; } = new();
    public string SectionTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerId { get; set; }
    public string? DuePeriod { get; set; }
    public string Status { get; set; } = "open"; // open, resolved
    public string? RemediationPlanId { get; set; }
    public string? RemediationPlanStatus { get; set; }
}

/// <summary>
/// Response for listing gaps with dashboard metadata.
/// </summary>
public sealed class GapDashboardResponse
{
    public List<GapDashboardItem> Gaps { get; set; } = new();
    public GapDashboardSummary Summary { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Summary metrics for the gap dashboard.
/// </summary>
public sealed class GapDashboardSummary
{
    public int TotalGaps { get; set; }
    public int OpenGaps { get; set; }
    public int ResolvedGaps { get; set; }
    public int HighRiskGaps { get; set; }
    public int MediumRiskGaps { get; set; }
    public int LowRiskGaps { get; set; }
    public int WithRemediationPlan { get; set; }
    public int WithoutRemediationPlan { get; set; }
}

/// <summary>
/// Request to create a new gap.
/// </summary>
public sealed class CreateGapRequest
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = "medium"; // low, medium, high
    public string? ImprovementPlan { get; set; }
    public string? TargetDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing gap.
/// </summary>
public sealed class UpdateGapRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = "medium"; // low, medium, high
    public string? ImprovementPlan { get; set; }
    public string? TargetDate { get; set; }
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Request to resolve a gap.
/// </summary>
public sealed class ResolveGapRequest
{
    public string? ResolutionNote { get; set; }
}

/// <summary>
/// Represents a decision made about report assumptions, simplifications, or boundaries.
/// Follows ADR (Architecture Decision Record) structure to document context, decision, alternatives, and consequences.
/// Supports versioning to maintain an audit trail of decision evolution.
/// </summary>
public sealed class Decision
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional section ID if the decision is specific to a section.
    /// </summary>
    public string? SectionId { get; set; }
    
    /// <summary>
    /// Brief title summarizing the decision.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Context: Background and circumstances leading to the decision.
    /// </summary>
    public string Context { get; set; } = string.Empty;
    
    /// <summary>
    /// Decision: The actual decision made and rationale.
    /// </summary>
    public string DecisionText { get; set; } = string.Empty;
    
    /// <summary>
    /// Alternatives: Other options that were considered but not chosen.
    /// </summary>
    public string Alternatives { get; set; } = string.Empty;
    
    /// <summary>
    /// Consequences: Expected impacts and implications of this decision.
    /// </summary>
    public string Consequences { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status: 'active' (current decision), 'superseded' (replaced by newer version), 'deprecated' (no longer applicable).
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// Version number - increments with each update. Version 1 is the initial decision.
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// References to report fragments (data points, sections) that use this decision.
    /// </summary>
    public List<string> ReferencedByFragmentIds { get; set; } = new();
    
    /// <summary>
    /// User who created this decision.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this decision was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who last updated this decision (null if never updated).
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Timestamp when this decision was last updated (null if never updated).
    /// </summary>
    public string? UpdatedAt { get; set; }
    
    /// <summary>
    /// Change note explaining what was updated in this version.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Represents a historical version of a decision (read-only).
/// Prior versions are preserved for audit trail purposes.
/// </summary>
public sealed class DecisionVersion
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The decision ID this version belongs to.
    /// </summary>
    public string DecisionId { get; set; } = string.Empty;
    
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string DecisionText { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string Consequences { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Request to create a new decision.
/// </summary>
public sealed class CreateDecisionRequest
{
    public string? SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string DecisionText { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string Consequences { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing decision. Creates a new version.
/// </summary>
public sealed class UpdateDecisionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string DecisionText { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string Consequences { get; set; } = string.Empty;
    public string ChangeNote { get; set; } = string.Empty;
}

/// <summary>
/// Request to link a decision to a report fragment.
/// </summary>
public sealed class LinkDecisionRequest
{
    public string FragmentId { get; set; } = string.Empty;
}

/// <summary>
/// Request to unlink a decision from a report fragment.
/// </summary>
public sealed class UnlinkDecisionRequest
{
    public string FragmentId { get; set; } = string.Empty;
}

/// <summary>
/// Request to deprecate a decision.
/// </summary>
public sealed class DeprecateDecisionRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents the audit view for a report fragment (section, data point, table row, paragraph).
/// Provides traceability from report output back to sources, evidence, and decisions.
/// </summary>
public sealed class FragmentAuditView
{
    /// <summary>
    /// Type of fragment: 'section', 'data-point', 'table-row', 'paragraph'.
    /// </summary>
    public string FragmentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique identifier of the fragment.
    /// </summary>
    public string FragmentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Stable identifier for export formats (PDF/DOCX). Used for mapping.
    /// </summary>
    public string StableFragmentIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Title or description of the fragment.
    /// </summary>
    public string FragmentTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Content of the fragment.
    /// </summary>
    public string FragmentContent { get; set; } = string.Empty;
    
    /// <summary>
    /// Parent section information.
    /// </summary>
    public FragmentSectionInfo? SectionInfo { get; set; }
    
    /// <summary>
    /// Linked source data references.
    /// </summary>
    public List<LinkedSource> LinkedSources { get; set; } = new();
    
    /// <summary>
    /// Linked evidence files.
    /// </summary>
    public List<LinkedEvidence> LinkedEvidenceFiles { get; set; } = new();
    
    /// <summary>
    /// Linked decisions.
    /// </summary>
    public List<LinkedDecision> LinkedDecisions { get; set; } = new();
    
    /// <summary>
    /// Linked assumptions.
    /// </summary>
    public List<LinkedAssumption> LinkedAssumptions { get; set; } = new();
    
    /// <summary>
    /// Linked gaps (if applicable).
    /// </summary>
    public List<LinkedGap> LinkedGaps { get; set; } = new();
    
    /// <summary>
    /// Missing provenance warnings.
    /// </summary>
    public List<ProvenanceWarning> ProvenanceWarnings { get; set; } = new();
    
    /// <summary>
    /// Indicates whether all required provenance links are present.
    /// </summary>
    public bool HasCompleteProvenance { get; set; }
    
    /// <summary>
    /// Audit trail entries for this fragment.
    /// </summary>
    public List<AuditLogEntry> AuditTrail { get; set; } = new();
}

/// <summary>
/// Section information for a fragment.
/// </summary>
public sealed class FragmentSectionInfo
{
    public string SectionId { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string SectionCategory { get; set; } = string.Empty;
    public string? CatalogCode { get; set; }
}

/// <summary>
/// Linked source reference with navigation details.
/// </summary>
public sealed class LinkedSource
{
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NavigationUrl { get; set; }
    public string? OriginSystem { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? LastUpdated { get; set; }
}

/// <summary>
/// Linked evidence file with navigation details.
/// </summary>
public sealed class LinkedEvidence
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? Checksum { get; set; }
    public string IntegrityStatus { get; set; } = string.Empty;
}

/// <summary>
/// Linked decision with navigation details.
/// </summary>
public sealed class LinkedDecision
{
    public string DecisionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DecisionText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string DecisionBy { get; set; } = string.Empty;
    public string DecisionDate { get; set; } = string.Empty;
}

/// <summary>
/// Linked assumption with navigation details.
/// </summary>
public sealed class LinkedAssumption
{
    public string AssumptionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Methodology { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Linked gap with navigation details.
/// </summary>
public sealed class LinkedGap
{
    public string GapId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public string? ImprovementPlan { get; set; }
}

/// <summary>
/// Warning about missing provenance information.
/// </summary>
public sealed class ProvenanceWarning
{
    /// <summary>
    /// Type of missing link: 'source', 'evidence', 'decision', 'assumption'.
    /// </summary>
    public string MissingLinkType { get; set; } = string.Empty;
    
    /// <summary>
    /// Warning message describing what is missing.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity: 'info', 'warning', 'error'.
    /// </summary>
    public string Severity { get; set; } = "warning";
    
    /// <summary>
    /// Recommendation for addressing the warning.
    /// </summary>
    public string? Recommendation { get; set; }
}

/// <summary>
/// Export mapping metadata for PDF/DOCX exports.
/// Maps stable fragment identifiers to their location in the export.
/// </summary>
public sealed class ExportFragmentMapping
{
    /// <summary>
    /// Unique identifier for this export.
    /// </summary>
    public string ExportId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reporting period ID.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Export format: 'pdf', 'docx'.
    /// </summary>
    public string ExportFormat { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when export was created.
    /// </summary>
    public string ExportedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who generated the export.
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Fragment mappings in this export.
    /// </summary>
    public List<FragmentMapping> Mappings { get; set; } = new();
}

/// <summary>
/// Individual fragment mapping in an export.
/// </summary>
public sealed class FragmentMapping
{
    /// <summary>
    /// Stable fragment identifier.
    /// </summary>
    public string StableFragmentIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Fragment type.
    /// </summary>
    public string FragmentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Fragment ID.
    /// </summary>
    public string FragmentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Page number in the export (for PDF).
    /// </summary>
    public int? PageNumber { get; set; }
    
    /// <summary>
    /// Paragraph/section number (for DOCX).
    /// </summary>
    public string? ParagraphNumber { get; set; }
    
    /// <summary>
    /// Section heading in the export.
    /// </summary>
    public string? SectionHeading { get; set; }
}

/// <summary>
/// Represents a consistency validation issue detected during validation.
/// Links to affected fragments and data records.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>
    /// Unique identifier for this validation issue.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of validation rule that failed.
    /// Examples: "missing-required-field", "invalid-unit", "contradictory-statement", "period-coverage"
    /// </summary>
    public string RuleType { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity level of the issue.
    /// Values: "error" (blocks publication), "warning" (requires attention), "info" (informational)
    /// </summary>
    public string Severity { get; set; } = "error";
    
    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the affected section, if applicable.
    /// </summary>
    public string? SectionId { get; set; }
    
    /// <summary>
    /// Title of the affected section, if applicable.
    /// </summary>
    public string? SectionTitle { get; set; }
    
    /// <summary>
    /// IDs of affected data points.
    /// </summary>
    public List<string> AffectedDataPointIds { get; set; } = new();
    
    /// <summary>
    /// IDs of affected evidence records.
    /// </summary>
    public List<string> AffectedEvidenceIds { get; set; } = new();
    
    /// <summary>
    /// Field name that has the issue (e.g., "Value", "Unit", "Content").
    /// </summary>
    public string? FieldName { get; set; }
    
    /// <summary>
    /// Expected value or format.
    /// </summary>
    public string? ExpectedValue { get; set; }
    
    /// <summary>
    /// Actual value that caused the issue.
    /// </summary>
    public string? ActualValue { get; set; }
    
    /// <summary>
    /// Timestamp when the issue was detected.
    /// </summary>
    public string DetectedAt { get; set; } = string.Empty;
}

/// <summary>
/// Result of a consistency validation run.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Overall validation status.
    /// Values: "passed", "failed", "warning"
    /// </summary>
    public string Status { get; set; } = "passed";
    
    /// <summary>
    /// Reporting period ID that was validated.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the reporting period.
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when validation was run.
    /// </summary>
    public string ValidatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who initiated the validation.
    /// </summary>
    public string ValidatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// List of all validation issues found.
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// Count of error-level issues (blocks publication).
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Count of warning-level issues.
    /// </summary>
    public int WarningCount { get; set; }
    
    /// <summary>
    /// Count of info-level issues.
    /// </summary>
    public int InfoCount { get; set; }
    
    /// <summary>
    /// Whether publication can proceed (true if ErrorCount == 0).
    /// </summary>
    public bool CanPublish { get; set; }
    
    /// <summary>
    /// Summary message describing the validation result.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Request to run consistency validation on a reporting period.
/// </summary>
public sealed class RunValidationRequest
{
    /// <summary>
    /// ID of the reporting period to validate.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person requesting validation.
    /// </summary>
    public string ValidatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Specific rule types to run. If empty, all rules are run.
    /// </summary>
    public List<string> RuleTypes { get; set; } = new();
}

/// <summary>
/// Request to publish a reporting period with optional override.
/// </summary>
public sealed class PublishReportRequest
{
    /// <summary>
    /// ID of the reporting period to publish.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person publishing.
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to override validation failures and publish anyway.
    /// </summary>
    public bool OverrideValidation { get; set; }
    
    /// <summary>
    /// Justification required when OverrideValidation is true.
    /// </summary>
    public string? OverrideJustification { get; set; }
}

/// <summary>
/// Result of a report publication attempt.
/// </summary>
public sealed class PublishReportResult
{
    /// <summary>
    /// ID of the reporting period.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the reporting period.
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when report was published.
    /// </summary>
    public string PublishedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who published the report.
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether validation was overridden.
    /// </summary>
    public bool ValidationOverridden { get; set; }
    
    /// <summary>
    /// Justification for overriding validation, if applicable.
    /// </summary>
    public string? OverrideJustification { get; set; }
    
    /// <summary>
    /// Validation result at time of publication.
    /// </summary>
    public ValidationResult? ValidationResult { get; set; }
    
    /// <summary>
    /// Publication status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Message describing the publication result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Approval request associated with this publication, if any.
    /// </summary>
    public ApprovalRequest? ApprovalRequest { get; set; }
}

/// <summary>
/// Request to initiate an approval workflow for a report version.
/// </summary>
public sealed class CreateApprovalRequestRequest
{
    /// <summary>
    /// ID of the reporting period for which approval is requested.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person requesting approval (typically report owner).
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// List of user IDs who should approve this report.
    /// </summary>
    public List<string> ApproverIds { get; set; } = new();
    
    /// <summary>
    /// Optional message to approvers explaining context or urgency.
    /// </summary>
    public string? RequestMessage { get; set; }
    
    /// <summary>
    /// Deadline for approval (ISO 8601 timestamp).
    /// </summary>
    public string? ApprovalDeadline { get; set; }
}

/// <summary>
/// Represents an approval request workflow for a reporting period.
/// </summary>
public sealed class ApprovalRequest
{
    /// <summary>
    /// Unique identifier for this approval request.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the reporting period this approval is for.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who requested the approval.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when approval was requested (ISO 8601).
    /// </summary>
    public string RequestedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional message to approvers.
    /// </summary>
    public string? RequestMessage { get; set; }
    
    /// <summary>
    /// Deadline for approval (ISO 8601 timestamp).
    /// </summary>
    public string? ApprovalDeadline { get; set; }
    
    /// <summary>
    /// Overall status of the approval request.
    /// Values: pending, approved, rejected, cancelled
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// Individual approval records from each approver.
    /// </summary>
    public List<ApprovalRecord> Approvals { get; set; } = new();
}

/// <summary>
/// Individual approval record from one approver.
/// </summary>
public sealed class ApprovalRecord
{
    /// <summary>
    /// Unique identifier for this approval record.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the approval request this record belongs to.
    /// </summary>
    public string ApprovalRequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the approver.
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the approver (for display).
    /// </summary>
    public string ApproverName { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of this individual approval.
    /// Values: pending, approved, rejected
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// Decision: approve or reject.
    /// </summary>
    public string? Decision { get; set; }
    
    /// <summary>
    /// Timestamp when the decision was made (ISO 8601).
    /// </summary>
    public string? DecidedAt { get; set; }
    
    /// <summary>
    /// Optional comment from the approver explaining their decision.
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Request to submit an approval decision.
/// </summary>
public sealed class SubmitApprovalDecisionRequest
{
    /// <summary>
    /// ID of the approval record being decided on.
    /// </summary>
    public string ApprovalRecordId { get; set; } = string.Empty;
    
    /// <summary>
    /// Decision: "approve" or "reject".
    /// </summary>
    public string Decision { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional comment explaining the decision.
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// User ID of the person making the decision.
    /// </summary>
    public string DecidedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to export an audit package for external auditors.
/// </summary>
public sealed class ExportAuditPackageRequest
{
    /// <summary>
    /// ID of the reporting period to export.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional list of section IDs to include. If empty/null, includes all sections.
    /// </summary>
    public List<string>? SectionIds { get; set; }
    
    /// <summary>
    /// User ID of the person requesting the export.
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note describing the purpose of this export.
    /// </summary>
    public string? ExportNote { get; set; }
}

/// <summary>
/// Result of an audit package export operation.
/// </summary>
public sealed class ExportAuditPackageResult
{
    /// <summary>
    /// Unique identifier for this export.
    /// </summary>
    public string ExportId { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the export was created.
    /// </summary>
    public string ExportedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who requested the export.
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// SHA-256 checksum of the entire package for integrity verification.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of the package in bytes.
    /// </summary>
    public long PackageSize { get; set; }
    
    /// <summary>
    /// Summary of what was included in the package.
    /// </summary>
    public AuditPackageSummary Summary { get; set; } = new();
}

/// <summary>
/// Summary of contents included in an audit package.
/// </summary>
public sealed class AuditPackageSummary
{
    public string PeriodId { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public int DataPointCount { get; set; }
    public int AuditLogEntryCount { get; set; }
    public int DecisionCount { get; set; }
    public int AssumptionCount { get; set; }
    public int GapCount { get; set; }
    public int EvidenceFileCount { get; set; }
}

/// <summary>
/// Complete audit package contents for export.
/// </summary>
public sealed class AuditPackageContents
{
    /// <summary>
    /// Export metadata.
    /// </summary>
    public ExportMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Report period information.
    /// </summary>
    public ReportingPeriod Period { get; set; } = new();
    
    /// <summary>
    /// Included sections with complete data.
    /// </summary>
    public List<SectionAuditData> Sections { get; set; } = new();
    
    /// <summary>
    /// Audit log entries for the period/sections.
    /// </summary>
    public List<AuditLogEntry> AuditTrail { get; set; } = new();
    
    /// <summary>
    /// Decision log entries related to the report.
    /// </summary>
    public List<Decision> Decisions { get; set; } = new();
    
    /// <summary>
    /// Evidence file references with checksums.
    /// </summary>
    public List<EvidenceReference> EvidenceFiles { get; set; } = new();
}

/// <summary>
/// Metadata about the export operation itself.
/// </summary>
public sealed class ExportMetadata
{
    public string ExportId { get; set; } = string.Empty;
    public string ExportedAt { get; set; } = string.Empty;
    public string ExportedBy { get; set; } = string.Empty;
    public string ExportedByName { get; set; } = string.Empty;
    public string? ExportNote { get; set; }
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Complete audit data for a section.
/// </summary>
public sealed class SectionAuditData
{
    public ReportSection Section { get; set; } = new();
    public List<DataPoint> DataPoints { get; set; } = new();
    public List<FragmentAuditView> ProvenanceMappings { get; set; } = new();
    public List<Gap> Gaps { get; set; } = new();
    public List<Assumption> Assumptions { get; set; } = new();
}

/// <summary>
/// Evidence file reference with integrity information.
/// </summary>
public sealed class EvidenceReference
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public string? Checksum { get; set; }
    public string? ContentType { get; set; }
    public string IntegrityStatus { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
    public List<string> LinkedDataPointIds { get; set; } = new();
}

/// <summary>
/// Record of an audit package export for tracking purposes.
/// </summary>
public sealed class AuditPackageExportRecord
{
    public string Id { get; set; } = string.Empty;
    public string PeriodId { get; set; } = string.Empty;
    public List<string> SectionIds { get; set; } = new();
    public string ExportedAt { get; set; } = string.Empty;
    public string ExportedBy { get; set; } = string.Empty;
    public string ExportedByName { get; set; } = string.Empty;
    public string? ExportNote { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public long PackageSize { get; set; }
}
