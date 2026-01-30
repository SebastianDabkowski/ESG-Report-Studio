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
    
    /// <summary>
    /// Indicates whether the user is active in the system.
    /// Inactive users cannot be assigned as owners during rollover.
    /// </summary>
    public bool IsActive { get; set; } = true;
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
    
    /// <summary>
    /// SHA-256 hash of critical fields for integrity verification.
    /// Calculated from: Id, Name, StartDate, EndDate, ReportingMode, ReportScope, OwnerId, OrganizationId
    /// </summary>
    public string? IntegrityHash { get; set; }
    
    /// <summary>
    /// Indicates if an integrity warning has been raised for this period.
    /// When true, publication is blocked unless overridden by an admin.
    /// </summary>
    public bool IntegrityWarning { get; set; }
    
    /// <summary>
    /// Details about the integrity warning, if any.
    /// </summary>
    public string? IntegrityWarningDetails { get; set; }
    
    /// <summary>
    /// Variance explanation threshold configuration for this reporting period.
    /// When null, variance explanations are not required.
    /// </summary>
    public VarianceThresholdConfig? VarianceThresholdConfig { get; set; }
    
    /// <summary>
    /// Indicates if the period is locked to prevent accidental edits.
    /// When true, only admins can make changes, and unlocking requires a documented reason.
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// Timestamp when the period was locked (ISO 8601 format).
    /// </summary>
    public string? LockedAt { get; set; }
    
    /// <summary>
    /// User ID of the person who locked the period.
    /// </summary>
    public string? LockedBy { get; set; }
    
    /// <summary>
    /// User name of the person who locked the period.
    /// </summary>
    public string? LockedByName { get; set; }
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
    
    /// <summary>
    /// Indicates whether this section is enabled for inclusion in report generation.
    /// When false, the section is excluded from generated reports.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
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
/// Request to lock a reporting period.
/// </summary>
public sealed class LockPeriodRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to unlock a locked reporting period (admin only).
/// </summary>
public sealed class UnlockPeriodRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
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
    
    // Calculation Lineage fields for Derived KPIs
    /// <summary>
    /// Indicates whether this data point is a calculated/derived value from other data points.
    /// </summary>
    public bool IsCalculated { get; set; }
    
    /// <summary>
    /// Formula or method description used to calculate this value.
    /// Example: "Total Scope 1 Emissions / Total Revenue", "Sum of all facility energy consumption".
    /// </summary>
    public string? CalculationFormula { get; set; }
    
    /// <summary>
    /// List of input data point IDs used in the calculation.
    /// These are the dependencies that, when changed, may require recalculation.
    /// </summary>
    public List<string> CalculationInputIds { get; set; } = new();
    
    /// <summary>
    /// Detailed snapshot of input values at the time of calculation for audit trail.
    /// JSON object containing { dataPointId: { value, unit, timestamp } } mappings.
    /// </summary>
    public string? CalculationInputSnapshot { get; set; }
    
    /// <summary>
    /// Version number for this calculated value. Increments when recalculated.
    /// </summary>
    public int CalculationVersion { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this value was calculated.
    /// </summary>
    public string? CalculatedAt { get; set; }
    
    /// <summary>
    /// User ID who performed or triggered the calculation.
    /// </summary>
    public string? CalculatedBy { get; set; }
    
    /// <summary>
    /// Flag indicating that one or more input values have changed since this calculation.
    /// Set to true when a dependent data point is updated.
    /// </summary>
    public bool CalculationNeedsRecalculation { get; set; }
    
    /// <summary>
    /// Reason why recalculation is needed (e.g., "Input data point X updated", "Input data point Y value changed").
    /// </summary>
    public string? RecalculationReason { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when the need for recalculation was detected.
    /// </summary>
    public string? RecalculationFlaggedAt { get; set; }
    
    // Cross-Period Lineage Tracking
    /// <summary>
    /// ID of the reporting period from which this data point was copied/rolled over.
    /// Null if this is an original data point (not copied from a previous period).
    /// </summary>
    public string? SourcePeriodId { get; set; }
    
    /// <summary>
    /// Name of the reporting period from which this data point was copied.
    /// Stored for quick reference without additional lookups.
    /// </summary>
    public string? SourcePeriodName { get; set; }
    
    /// <summary>
    /// ID of the original data point from the previous period that was copied.
    /// Used to trace lineage back through reporting periods.
    /// </summary>
    public string? SourceDataPointId { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this data point was rolled over from a previous period.
    /// </summary>
    public string? RolloverTimestamp { get; set; }
    
    /// <summary>
    /// User ID who performed the rollover operation.
    /// </summary>
    public string? RolloverPerformedBy { get; set; }
    
    /// <summary>
    /// Name of user who performed the rollover operation.
    /// </summary>
    public string? RolloverPerformedByName { get; set; }
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
    
    // Calculation Lineage fields for Derived KPIs
    /// <summary>
    /// Indicates whether this data point is a calculated/derived value.
    /// </summary>
    public bool IsCalculated { get; set; }
    
    /// <summary>
    /// Formula or method description used to calculate this value.
    /// </summary>
    public string? CalculationFormula { get; set; }
    
    /// <summary>
    /// List of input data point IDs used in the calculation.
    /// </summary>
    public List<string> CalculationInputIds { get; set; } = new();
    
    /// <summary>
    /// User ID who performed the calculation.
    /// </summary>
    public string? CalculatedBy { get; set; }
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
    
    // Calculation Lineage fields for Derived KPIs
    /// <summary>
    /// Indicates whether this data point is a calculated/derived value.
    /// </summary>
    public bool IsCalculated { get; set; }
    
    /// <summary>
    /// Formula or method description used to calculate this value.
    /// </summary>
    public string? CalculationFormula { get; set; }
    
    /// <summary>
    /// List of input data point IDs used in the calculation.
    /// </summary>
    public List<string> CalculationInputIds { get; set; } = new();
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
/// Request to recalculate a derived data point using current input values.
/// </summary>
public sealed class RecalculateDataPointRequest
{
    /// <summary>
    /// User ID of the person triggering the recalculation.
    /// </summary>
    public string CalculatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note explaining the reason for recalculation.
    /// </summary>
    public string? ChangeNote { get; set; }
}

/// <summary>
/// Response containing calculation lineage information for a data point.
/// </summary>
public sealed class CalculationLineageResponse
{
    /// <summary>
    /// The data point ID for which lineage is being provided.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Formula or method used for calculation.
    /// </summary>
    public string? Formula { get; set; }
    
    /// <summary>
    /// Current version of the calculation.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// When this calculation was performed.
    /// </summary>
    public string? CalculatedAt { get; set; }
    
    /// <summary>
    /// Who performed or triggered the calculation.
    /// </summary>
    public string? CalculatedBy { get; set; }
    
    /// <summary>
    /// List of input data points with their current values and metadata.
    /// </summary>
    public List<LineageInput> Inputs { get; set; } = new();
    
    /// <summary>
    /// Snapshot of input values at the time of last calculation.
    /// </summary>
    public string? InputSnapshot { get; set; }
    
    /// <summary>
    /// Flag indicating inputs have changed since last calculation.
    /// </summary>
    public bool NeedsRecalculation { get; set; }
    
    /// <summary>
    /// Reason why recalculation is needed.
    /// </summary>
    public string? RecalculationReason { get; set; }
}

/// <summary>
/// Represents an input data point in the calculation lineage.
/// </summary>
public sealed class LineageInput
{
    /// <summary>
    /// ID of the input data point.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the input data point.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Current value of the input.
    /// </summary>
    public string? CurrentValue { get; set; }
    
    /// <summary>
    /// Unit of the current value.
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Value used in the last calculation (may differ from current if changed).
    /// </summary>
    public string? ValueAtCalculation { get; set; }
    
    /// <summary>
    /// When this input was last updated.
    /// </summary>
    public string? LastUpdated { get; set; }
    
    /// <summary>
    /// Flag indicating this input has changed since the calculation.
    /// </summary>
    public bool HasChanged { get; set; }
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
/// Comparison of completeness breakdown between two periods.
/// </summary>
public sealed class CompletenessBreakdownComparison
{
    /// <summary>
    /// Identifier (e.g., category name, organizational unit ID, or section catalog code).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the breakdown dimension.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current period completeness breakdown.
    /// </summary>
    public CompletenessBreakdown CurrentPeriod { get; set; } = new();

    /// <summary>
    /// Prior period completeness breakdown.
    /// </summary>
    public CompletenessBreakdown? PriorPeriod { get; set; }

    /// <summary>
    /// Change in complete percentage (current - prior).
    /// Positive indicates improvement, negative indicates regression.
    /// </summary>
    public double? PercentagePointChange { get; set; }

    /// <summary>
    /// Change in absolute number of complete items.
    /// </summary>
    public int? CompleteCountChange { get; set; }

    /// <summary>
    /// Indicates if there's a regression in completeness (negative change).
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Owner ID if this is a section-level comparison.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Owner name if this is a section-level comparison.
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Indicates if the item exists in both periods (true) or only in one period (false).
    /// When false, changes should be interpreted as structural rather than regression.
    /// </summary>
    public bool ExistsInBothPeriods { get; set; }

    /// <summary>
    /// Reason why item doesn't exist in both periods.
    /// </summary>
    public string? NotApplicableReason { get; set; }
}

/// <summary>
/// Completeness comparison between two reporting periods.
/// </summary>
public sealed class CompletenessComparison
{
    /// <summary>
    /// Current reporting period information.
    /// </summary>
    public PeriodInfo CurrentPeriod { get; set; } = new();

    /// <summary>
    /// Prior reporting period information.
    /// </summary>
    public PeriodInfo PriorPeriod { get; set; } = new();

    /// <summary>
    /// Overall completeness comparison.
    /// </summary>
    public CompletenessBreakdownComparison Overall { get; set; } = new();

    /// <summary>
    /// Completeness comparison by E/S/G category.
    /// </summary>
    public List<CompletenessBreakdownComparison> ByCategory { get; set; } = new();

    /// <summary>
    /// Completeness comparison by section (using catalog codes for matching).
    /// </summary>
    public List<CompletenessBreakdownComparison> BySection { get; set; } = new();

    /// <summary>
    /// Completeness comparison by organizational unit.
    /// </summary>
    public List<CompletenessBreakdownComparison> ByOrganizationalUnit { get; set; } = new();

    /// <summary>
    /// Sections with regressions, ordered by severity.
    /// </summary>
    public List<CompletenessBreakdownComparison> Regressions { get; set; } = new();

    /// <summary>
    /// Sections with improvements, ordered by magnitude.
    /// </summary>
    public List<CompletenessBreakdownComparison> Improvements { get; set; } = new();

    /// <summary>
    /// Summary statistics for the comparison.
    /// </summary>
    public ComparisonSummary Summary { get; set; } = new();
}

/// <summary>
/// Basic period information for comparison context.
/// </summary>
public sealed class PeriodInfo
{
    /// <summary>
    /// Period ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Period name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Period start date.
    /// </summary>
    public string StartDate { get; set; } = string.Empty;

    /// <summary>
    /// Period end date.
    /// </summary>
    public string EndDate { get; set; } = string.Empty;
}

/// <summary>
/// Summary statistics for completeness comparison.
/// </summary>
public sealed class ComparisonSummary
{
    /// <summary>
    /// Total number of sections with regressions.
    /// </summary>
    public int RegressionCount { get; set; }

    /// <summary>
    /// Total number of sections with improvements.
    /// </summary>
    public int ImprovementCount { get; set; }

    /// <summary>
    /// Total number of sections unchanged.
    /// </summary>
    public int UnchangedCount { get; set; }

    /// <summary>
    /// Number of sections added in current period (not in prior).
    /// </summary>
    public int AddedSectionCount { get; set; }

    /// <summary>
    /// Number of sections removed from current period (were in prior).
    /// </summary>
    public int RemovedSectionCount { get; set; }
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
    
    /// <summary>
    /// SHA-256 hash of critical fields for integrity verification.
    /// Calculated from: Id, Version, Title, Context, DecisionText, Alternatives, Consequences
    /// </summary>
    public string? IntegrityHash { get; set; }
    
    /// <summary>
    /// Status of integrity verification: 'valid', 'failed', 'not-checked'
    /// </summary>
    public string IntegrityStatus { get; set; } = "not-checked";
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
    
    /// <summary>
    /// SHA-256 hash of this version's content for integrity verification.
    /// </summary>
    public string? IntegrityHash { get; set; }
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
    /// If the user doesn't exist in the system, the user ID will be used as the display name in export metadata.
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
    /// Note: Only populated when downloading the package via the download endpoint.
    /// The export metadata endpoint returns an empty checksum.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of the package in bytes.
    /// Note: Only populated when downloading the package via the download endpoint.
    /// The export metadata endpoint returns 0.
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

/// <summary>
/// Report of integrity status for a reporting period and its related entities.
/// </summary>
public sealed class IntegrityStatusReport
{
    public string PeriodId { get; set; } = string.Empty;
    public bool PeriodIntegrityValid { get; set; }
    public bool PeriodIntegrityWarning { get; set; }
    public List<string> FailedDecisions { get; set; } = new();
    public bool CanPublish { get; set; }
    public string? WarningDetails { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to override an integrity warning.
/// </summary>
public sealed class OverrideIntegrityWarningRequest
{
    public string Justification { get; set; } = string.Empty;
}

/// <summary>
/// Retention policy configuration for audit data.
/// Supports both tenant-level and report-type-level retention periods.
/// </summary>
public sealed class RetentionPolicy
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional tenant ID for tenant-specific policies.
    /// If null, applies as a default policy.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Optional report type for report-type-specific policies.
    /// Examples: "simplified", "extended", "csrd-aligned"
    /// If null, applies to all report types.
    /// </summary>
    public string? ReportType { get; set; }
    
    /// <summary>
    /// Data category this policy applies to.
    /// Examples: "audit-log", "evidence", "all"
    /// </summary>
    public string DataCategory { get; set; } = "all";
    
    /// <summary>
    /// Retention period in days.
    /// Data older than this period becomes eligible for cleanup.
    /// </summary>
    public int RetentionDays { get; set; }
    
    /// <summary>
    /// Whether this policy is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Priority level for policy resolution (higher = more specific).
    /// Used when multiple policies could apply.
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Whether deletion is actually performed or just logged.
    /// For regulated customers, may be set to false.
    /// </summary>
    public bool AllowDeletion { get; set; } = true;
    
    /// <summary>
    /// Timestamp when this policy was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who created this policy.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this policy was last updated.
    /// </summary>
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Legal hold status for preserving data beyond retention periods.
/// Future feature - placeholder for now.
/// </summary>
public sealed class LegalHold
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional tenant ID if hold is tenant-specific.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Optional period ID if hold is period-specific.
    /// </summary>
    public string? PeriodId { get; set; }
    
    /// <summary>
    /// Reason for the legal hold.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference number (e.g., case number, matter ID).
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this hold is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Timestamp when hold was placed.
    /// </summary>
    public string PlacedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who placed the hold.
    /// </summary>
    public string PlacedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional timestamp when hold will be automatically released.
    /// </summary>
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// Metadata-only record of data deletion for audit compliance.
/// Does not contain the deleted data itself, only information about what was deleted.
/// </summary>
public sealed class DeletionReport
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when deletion occurred.
    /// </summary>
    public string DeletedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who initiated the deletion.
    /// </summary>
    public string DeletedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Retention policy that triggered this deletion.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data category that was deleted.
    /// </summary>
    public string DataCategory { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of records deleted.
    /// </summary>
    public int RecordCount { get; set; }
    
    /// <summary>
    /// Date range of deleted records (start).
    /// </summary>
    public string DateRangeStart { get; set; } = string.Empty;
    
    /// <summary>
    /// Date range of deleted records (end).
    /// </summary>
    public string DateRangeEnd { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional tenant ID for tenant-specific deletions.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Summary of what was deleted (metadata only, no actual data).
    /// Example: "127 audit log entries from 2023-01-01 to 2023-03-31"
    /// </summary>
    public string DeletionSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// Cryptographic signature of the report for tamper detection.
    /// Future: Should be signed with a private key for non-repudiation.
    /// </summary>
    public string? Signature { get; set; }
    
    /// <summary>
    /// Hash of report content for integrity verification.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a retention policy.
/// </summary>
/// <summary>
/// Request to create a retention policy.
/// </summary>
public sealed class CreateRetentionPolicyRequest
{
    public string? TenantId { get; set; }
    public string? ReportType { get; set; }
    public string DataCategory { get; set; } = "all";
    public int RetentionDays { get; set; }
    public bool AllowDeletion { get; set; } = true;
    
    /// <summary>
    /// User creating the policy. Set automatically by controller from authenticated user.
    /// API consumers should not set this field; it will be overwritten.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to run cleanup based on retention policies.
/// </summary>
public sealed class RunCleanupRequest
{
    /// <summary>
    /// If true, performs a dry run without actually deleting data.
    /// </summary>
    public bool DryRun { get; set; } = true;
    
    /// <summary>
    /// Optional tenant ID to limit cleanup to specific tenant.
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// User initiating the cleanup. Set automatically by controller from authenticated user.
    /// API consumers should not set this field; it will be overwritten.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Result of a cleanup operation.
/// </summary>
public sealed class CleanupResult
{
    public bool Success { get; set; }
    public bool WasDryRun { get; set; }
    public int RecordsIdentified { get; set; }
    public int RecordsDeleted { get; set; }
    public List<string> DeletionReportIds { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string ExecutedAt { get; set; } = string.Empty;
}

/// <summary>
/// Options for rolling over content from one reporting period to another.
/// </summary>
public sealed class RolloverOptions
{
    /// <summary>
    /// Copy section structure (titles, descriptions, ownership).
    /// </summary>
    public bool CopyStructure { get; set; } = true;
    
    /// <summary>
    /// Copy disclosures (gaps, assumptions, remediation plans).
    /// Requires CopyStructure to be true.
    /// </summary>
    public bool CopyDisclosures { get; set; }
    
    /// <summary>
    /// Copy data values (data points, narratives, metrics).
    /// Requires CopyStructure to be true.
    /// </summary>
    public bool CopyDataValues { get; set; }
    
    /// <summary>
    /// Copy attachments (evidence files).
    /// Requires CopyDataValues to be true.
    /// </summary>
    public bool CopyAttachments { get; set; }
    
    /// <summary>
    /// Number of days to add to remediation action due dates when carrying forward tasks.
    /// If null or 0, due dates are not adjusted. If positive, shifts dates forward by that many days.
    /// This enables re-baselining task due dates to align with the new reporting period.
    /// </summary>
    public int? DueDateAdjustmentDays { get; set; }
}

/// <summary>
/// Request to rollover (copy) a reporting period to a new period.
/// </summary>
public sealed class RolloverRequest
{
    /// <summary>
    /// ID of the source reporting period to copy from.
    /// </summary>
    public string SourcePeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name for the new reporting period.
    /// </summary>
    public string TargetPeriodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Start date for the new reporting period (ISO 8601 format).
    /// </summary>
    public string TargetPeriodStartDate { get; set; } = string.Empty;
    
    /// <summary>
    /// End date for the new reporting period (ISO 8601 format).
    /// </summary>
    public string TargetPeriodEndDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Reporting mode for the new period (simplified or extended).
    /// Defaults to the source period's mode if not specified.
    /// </summary>
    public string? TargetReportingMode { get; set; }
    
    /// <summary>
    /// Report scope for the new period (single-company or group).
    /// Defaults to the source period's scope if not specified.
    /// </summary>
    public string? TargetReportScope { get; set; }
    
    /// <summary>
    /// Options controlling what content to copy.
    /// </summary>
    public RolloverOptions Options { get; set; } = new();
    
    /// <summary>
    /// ID of the user performing the rollover (for audit trail).
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional rule overrides for this specific rollover operation.
    /// These temporary rules apply only to this rollover and don't affect the global configuration.
    /// </summary>
    public List<RolloverRuleOverride> RuleOverrides { get; set; } = new();
    
    /// <summary>
    /// Optional manual mappings for sections that cannot be automatically mapped.
    /// Maps source section CatalogCode to target section CatalogCode.
    /// </summary>
    public List<ManualSectionMapping> ManualMappings { get; set; } = new();
}

/// <summary>
/// Audit log entry for a period rollover operation.
/// </summary>
public sealed class RolloverAuditLog
{
    public string Id { get; set; } = string.Empty;
    public string SourcePeriodId { get; set; } = string.Empty;
    public string SourcePeriodName { get; set; } = string.Empty;
    public string TargetPeriodId { get; set; } = string.Empty;
    public string TargetPeriodName { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public string PerformedAt { get; set; } = string.Empty;
    public RolloverOptions Options { get; set; } = new();
    
    // Statistics about what was copied
    public int SectionsCopied { get; set; }
    public int DataPointsCopied { get; set; }
    public int GapsCopied { get; set; }
    public int AssumptionsCopied { get; set; }
    public int RemediationPlansCopied { get; set; }
    public int EvidenceCopied { get; set; }
}

/// <summary>
/// Result of a rollover operation.
/// </summary>
public sealed class RolloverResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ReportingPeriod? TargetPeriod { get; set; }
    public RolloverAuditLog? AuditLog { get; set; }
    
    /// <summary>
    /// Reconciliation report showing mapping results and any unmapped items.
    /// </summary>
    public RolloverReconciliation? Reconciliation { get; set; }
    
    /// <summary>
    /// Warnings about inactive users found in ownership assignments during rollover.
    /// These users need to be reassigned before finalizing the new period.
    /// </summary>
    public List<InactiveOwnerWarning> InactiveOwnerWarnings { get; set; } = new();
}

/// <summary>
/// Warning about an inactive user assigned as owner in carried-forward content.
/// </summary>
public sealed class InactiveOwnerWarning
{
    /// <summary>
    /// ID of the inactive user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the inactive user.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity where the inactive owner was found.
    /// Examples: "Section", "RemediationPlan", "RemediationAction", "DataPoint"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity with the inactive owner.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title or description of the entity for display purposes.
    /// </summary>
    public string EntityTitle { get; set; } = string.Empty;
}

/// <summary>
/// Manual mapping of a source section to a target section.
/// Used when automatic mapping by CatalogCode is not possible.
/// </summary>
public sealed class ManualSectionMapping
{
    /// <summary>
    /// Source section catalog code.
    /// </summary>
    public string SourceCatalogCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Target section catalog code to map to.
    /// </summary>
    public string TargetCatalogCode { get; set; } = string.Empty;
}

/// <summary>
/// Reconciliation report for a rollover operation showing mapping results.
/// </summary>
public sealed class RolloverReconciliation
{
    /// <summary>
    /// Total number of sections in the source period.
    /// </summary>
    public int TotalSourceSections { get; set; }
    
    /// <summary>
    /// Number of sections successfully mapped (automatically or manually).
    /// </summary>
    public int MappedSections { get; set; }
    
    /// <summary>
    /// Number of sections that could not be mapped.
    /// </summary>
    public int UnmappedSections { get; set; }
    
    /// <summary>
    /// List of sections that were successfully mapped.
    /// </summary>
    public List<MappedSection> MappedItems { get; set; } = new();
    
    /// <summary>
    /// List of sections that could not be mapped with reasons.
    /// </summary>
    public List<UnmappedSection> UnmappedItems { get; set; } = new();
}

/// <summary>
/// Represents a successfully mapped section during rollover.
/// </summary>
public sealed class MappedSection
{
    /// <summary>
    /// Source section catalog code.
    /// </summary>
    public string SourceCatalogCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Source section title.
    /// </summary>
    public string SourceTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Target section catalog code.
    /// </summary>
    public string TargetCatalogCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Target section title.
    /// </summary>
    public string TargetTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// How the mapping was performed: "automatic" or "manual".
    /// </summary>
    public string MappingType { get; set; } = "automatic";
    
    /// <summary>
    /// Number of data points copied.
    /// </summary>
    public int DataPointsCopied { get; set; }
}

/// <summary>
/// Represents a section that could not be mapped during rollover.
/// </summary>
public sealed class UnmappedSection
{
    /// <summary>
    /// Source section catalog code (may be null if section has no catalog code).
    /// </summary>
    public string? SourceCatalogCode { get; set; }
    
    /// <summary>
    /// Source section title.
    /// </summary>
    public string SourceTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Source section ID.
    /// </summary>
    public string SourceSectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason why the section could not be mapped.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Suggested actions to resolve the unmapped item.
    /// </summary>
    public List<string> SuggestedActions { get; set; } = new();
    
    /// <summary>
    /// Number of data points that were not copied due to unmapped section.
    /// </summary>
    public int AffectedDataPoints { get; set; }
}

/// <summary>
/// Rule type for how a data type should be handled during rollover.
/// </summary>
public enum DataTypeRolloverRuleType
{
    /// <summary>
    /// Copy data values from source period to target period (default behavior).
    /// </summary>
    Copy,
    
    /// <summary>
    /// Reset data values - don't copy data, create empty placeholders.
    /// </summary>
    Reset,
    
    /// <summary>
    /// Copy data values but mark them as draft requiring review.
    /// </summary>
    CopyAsDraft
}

/// <summary>
/// Rollover rule configuration for a specific data type.
/// Defines how data points of this type should be handled during period rollover.
/// </summary>
public sealed class DataTypeRolloverRule
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Data type this rule applies to (e.g., "narrative", "metric", "kpi", "policy", "target").
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Rule type defining how this data type is handled during rollover.
    /// </summary>
    public DataTypeRolloverRuleType RuleType { get; set; } = DataTypeRolloverRuleType.Copy;
    
    /// <summary>
    /// Optional description explaining why this rule is configured this way.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this rule was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who created this rule.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when this rule was last updated.
    /// </summary>
    public string? UpdatedAt { get; set; }
    
    /// <summary>
    /// User ID who last updated this rule.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Version number of this rule. Increments on each update for history tracking.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Historical record of a rollover rule change for audit purposes.
/// </summary>
public sealed class RolloverRuleHistory
{
    /// <summary>
    /// Unique identifier for this history entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the rule this history entry relates to.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data type the rule applies to.
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Rule type at this point in history.
    /// </summary>
    public DataTypeRolloverRuleType RuleType { get; set; }
    
    /// <summary>
    /// Description at this point in history.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Version number of the rule at this point.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this change was made.
    /// </summary>
    public string ChangedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who made this change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// User name who made this change.
    /// </summary>
    public string ChangedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of change: "created", "updated", "deleted".
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;
}

/// <summary>
/// Request to create or update a rollover rule for a data type.
/// </summary>
public sealed class SaveDataTypeRolloverRuleRequest
{
    /// <summary>
    /// Data type this rule applies to (e.g., "narrative", "metric", "kpi", "policy", "target").
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Rule type defining how this data type is handled during rollover.
    /// Valid values: "copy", "reset", "copy-as-draft".
    /// </summary>
    public string RuleType { get; set; } = "copy";
    
    /// <summary>
    /// Optional description explaining why this rule is configured this way.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// User ID performing the save operation (for audit trail).
    /// </summary>
    public string SavedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response containing a rollover rule with additional metadata.
/// </summary>
public sealed class DataTypeRolloverRuleResponse
{
    /// <summary>
    /// The rollover rule.
    /// </summary>
    public DataTypeRolloverRule Rule { get; set; } = new();
    
    /// <summary>
    /// Number of history entries for this rule.
    /// </summary>
    public int HistoryCount { get; set; }
}

/// <summary>
/// Override rules for a specific rollover operation.
/// Allows temporary rule changes for a single rollover without affecting the global configuration.
/// </summary>
public sealed class RolloverRuleOverride
{
    /// <summary>
    /// Data type to override the rule for.
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Rule type to use for this specific rollover.
    /// </summary>
    public DataTypeRolloverRuleType RuleType { get; set; }
}

/// <summary>
/// Represents a snapshot of a data point's value at a specific point in time.
/// Used to track value changes across periods.
/// </summary>
public sealed class DataPointVersionSnapshot
{
    /// <summary>
    /// ID of the data point.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the reporting period.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the reporting period.
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Start date of the reporting period.
    /// </summary>
    public string PeriodStartDate { get; set; } = string.Empty;
    
    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public string PeriodEndDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Value of the data point.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Content/description of the data point.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Unit of measurement.
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Source of the data.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Information type (fact, estimate, declaration, plan).
    /// </summary>
    public string InformationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this version was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this version was last updated.
    /// </summary>
    public string UpdatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner ID at this point in time.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner name at this point in time.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of evidence attachments.
    /// </summary>
    public int EvidenceCount { get; set; }
    
    /// <summary>
    /// Indicates if this value was copied from a previous period.
    /// </summary>
    public bool IsRolledOver { get; set; }
    
    /// <summary>
    /// Timestamp when rolled over, if applicable.
    /// </summary>
    public string? RolloverTimestamp { get; set; }
}

/// <summary>
/// Response containing cross-period lineage information for a data point.
/// Shows the history of a data point across multiple reporting periods.
/// </summary>
public sealed class CrossPeriodLineageResponse
{
    /// <summary>
    /// ID of the current data point.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the data point.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Current reporting period information.
    /// </summary>
    public DataPointVersionSnapshot CurrentVersion { get; set; } = new();
    
    /// <summary>
    /// Historical versions from previous periods, ordered from most recent to oldest.
    /// </summary>
    public List<DataPointVersionSnapshot> PreviousVersions { get; set; } = new();
    
    /// <summary>
    /// Audit log entries showing all changes within the current period.
    /// </summary>
    public List<AuditLogEntry> CurrentPeriodChanges { get; set; } = new();
    
    /// <summary>
    /// Total number of periods this data point has existed in.
    /// </summary>
    public int TotalPeriods { get; set; }
    
    /// <summary>
    /// Indicates if there are more historical versions beyond those returned.
    /// </summary>
    public bool HasMoreHistory { get; set; }
}

/// <summary>
/// Response containing year-over-year comparison for a numeric metric.
/// Compares a metric's value across two reporting periods.
/// </summary>
public sealed class MetricComparisonResponse
{
    /// <summary>
    /// ID of the current data point being compared.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the metric.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Current period value information.
    /// </summary>
    public MetricPeriodValue CurrentPeriod { get; set; } = new();
    
    /// <summary>
    /// Prior period value information.
    /// </summary>
    public MetricPeriodValue? PriorPeriod { get; set; }
    
    /// <summary>
    /// Percentage change from prior period to current period.
    /// Null if prior period data is unavailable or values are not numeric.
    /// </summary>
    public decimal? PercentageChange { get; set; }
    
    /// <summary>
    /// Absolute change from prior period to current period.
    /// Null if prior period data is unavailable or values are not numeric.
    /// </summary>
    public decimal? AbsoluteChange { get; set; }
    
    /// <summary>
    /// Indicates whether comparison is available.
    /// </summary>
    public bool IsComparisonAvailable { get; set; }
    
    /// <summary>
    /// Reason why comparison is unavailable (if applicable).
    /// Examples: "No prior period data", "Unit mismatch", "Non-numeric values"
    /// </summary>
    public string? UnavailableReason { get; set; }
    
    /// <summary>
    /// Indicates if units are compatible between periods.
    /// </summary>
    public bool UnitsCompatible { get; set; }
    
    /// <summary>
    /// Warning message if units differ but could potentially be converted.
    /// </summary>
    public string? UnitWarning { get; set; }
    
    /// <summary>
    /// List of available baseline periods for comparison.
    /// </summary>
    public List<AvailableBaselinePeriod> AvailableBaselines { get; set; } = new();
    
    /// <summary>
    /// Variance flag information indicating if this change requires explanation.
    /// </summary>
    public VarianceFlagInfo? VarianceFlag { get; set; }
}

/// <summary>
/// Represents a metric value for a specific reporting period.
/// </summary>
public sealed class MetricPeriodValue
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
    /// Start date of the reporting period.
    /// </summary>
    public string StartDate { get; set; } = string.Empty;
    
    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public string EndDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Metric value (as string for display).
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Numeric value (parsed from Value if numeric).
    /// </summary>
    public decimal? NumericValue { get; set; }
    
    /// <summary>
    /// Unit of measurement.
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Source of the data.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Information type (fact, estimate, etc.).
    /// </summary>
    public string InformationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner of the data point.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of evidence items attached.
    /// </summary>
    public int EvidenceCount { get; set; }
    
    /// <summary>
    /// Indicates if this value is missing or unavailable.
    /// </summary>
    public bool IsMissing { get; set; }
    
    /// <summary>
    /// Reason for missing data (if applicable).
    /// </summary>
    public string? MissingReason { get; set; }
}

/// <summary>
/// Represents an available baseline period for comparison.
/// </summary>
public sealed class AvailableBaselinePeriod
{
    /// <summary>
    /// ID of the baseline period.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the baseline period.
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;
    
    /// <summary>
    /// Label for this baseline (e.g., "Previous Year", "2 Years Back").
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if data exists for this period.
    /// </summary>
    public bool HasData { get; set; }
    
    /// <summary>
    /// Start date of the baseline period.
    /// </summary>
    public string StartDate { get; set; } = string.Empty;
    
    /// <summary>
    /// End date of the baseline period.
    /// </summary>
    public string EndDate { get; set; } = string.Empty;
}

/// <summary>
/// Request to compare narrative text disclosures between two periods.
/// </summary>
public sealed class CompareTextDisclosuresRequest
{
    /// <summary>
    /// ID of the data point in the current period.
    /// </summary>
    public string CurrentDataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: ID of the previous period to compare against.
    /// If not provided, compares against the data point's source period (if it was rolled over).
    /// </summary>
    public string? PreviousPeriodId { get; set; }
    
    /// <summary>
    /// Diff granularity: "word" or "sentence". Defaults to "word".
    /// </summary>
    public string Granularity { get; set; } = "word";
}

/// <summary>
/// Response containing text disclosure comparison results.
/// </summary>
public sealed class TextDisclosureComparisonResponse
{
    /// <summary>
    /// Current data point information.
    /// </summary>
    public DataPointInfo CurrentDataPoint { get; set; } = new();
    
    /// <summary>
    /// Previous data point information (if found).
    /// </summary>
    public DataPointInfo? PreviousDataPoint { get; set; }
    
    /// <summary>
    /// List of text segments with change indicators.
    /// </summary>
    public List<TextSegmentDto> Segments { get; set; } = new();
    
    /// <summary>
    /// Summary of changes.
    /// </summary>
    public DiffSummaryDto Summary { get; set; } = new();
    
    /// <summary>
    /// Indicates if the current data point is a draft copy from a previous period.
    /// When true and no edits have been made, the diff shows no changes.
    /// </summary>
    public bool IsDraftCopy { get; set; }
    
    /// <summary>
    /// Indicates if the disclosure has been edited since being copied/rolled over.
    /// </summary>
    public bool HasBeenEdited { get; set; }
}

/// <summary>
/// Summary information about a data point for comparison.
/// </summary>
public sealed class DataPointInfo
{
    public string Id { get; set; } = string.Empty;
    public string PeriodId { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? SourcePeriodId { get; set; }
    public string? SourceDataPointId { get; set; }
    public string? RolloverTimestamp { get; set; }
}

/// <summary>
/// DTO for text segment with change type.
/// </summary>
public sealed class TextSegmentDto
{
    /// <summary>
    /// The text content of this segment.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Change type: "unchanged", "added", "removed", "modified".
    /// </summary>
    public string ChangeType { get; set; } = "unchanged";
}

/// <summary>
/// DTO for diff summary statistics.
/// </summary>
public sealed class DiffSummaryDto
{
    public int TotalSegments { get; set; }
    public int AddedSegments { get; set; }
    public int RemovedSegments { get; set; }
    public int UnchangedSegments { get; set; }
    public int OldTextLength { get; set; }
    public int NewTextLength { get; set; }
    public bool HasChanges { get; set; }
}

/// <summary>
/// Configuration for variance thresholds that trigger explanation requirements.
/// </summary>
public sealed class VarianceThresholdConfig
{
    /// <summary>
    /// Unique identifier for this configuration.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Percentage change threshold that triggers explanation requirement.
    /// Example: 10 means changes >= 10% require explanation.
    /// Null if percentage threshold is not used.
    /// </summary>
    public decimal? PercentageThreshold { get; set; }
    
    /// <summary>
    /// Absolute change threshold that triggers explanation requirement.
    /// Example: 1000 means absolute changes >= 1000 require explanation.
    /// Null if absolute threshold is not used.
    /// </summary>
    public decimal? AbsoluteThreshold { get; set; }
    
    /// <summary>
    /// When true, both percentage AND absolute thresholds must be exceeded.
    /// When false, either threshold being exceeded triggers requirement.
    /// </summary>
    public bool RequireBothThresholds { get; set; } = false;
    
    /// <summary>
    /// When true, variance explanations require reviewer approval before being cleared.
    /// When false, submitting an explanation immediately clears the flag.
    /// </summary>
    public bool RequireReviewerApproval { get; set; } = false;
    
    /// <summary>
    /// ISO 8601 timestamp when this configuration was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who created this configuration.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a variance explanation for a significant year-over-year change in a metric.
/// </summary>
public sealed class VarianceExplanation
{
    /// <summary>
    /// Unique identifier for this variance explanation.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the data point in the current period that has the variance.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the prior reporting period being compared against.
    /// </summary>
    public string PriorPeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the data point in the prior period (for reference).
    /// </summary>
    public string? PriorDataPointId { get; set; }
    
    /// <summary>
    /// Current period value.
    /// </summary>
    public string CurrentValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Prior period value.
    /// </summary>
    public string PriorValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Percentage change from prior to current period.
    /// </summary>
    public decimal? PercentageChange { get; set; }
    
    /// <summary>
    /// Absolute change from prior to current period.
    /// </summary>
    public decimal? AbsoluteChange { get; set; }
    
    /// <summary>
    /// Primary narrative explanation for the variance.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Root cause or underlying reason for the change.
    /// Examples: "Business expansion", "Process improvement", "Market conditions"
    /// </summary>
    public string? RootCause { get; set; }
    
    /// <summary>
    /// Category of the variance explanation.
    /// Examples: "operational-change", "methodology-change", "business-expansion", "market-conditions", "other"
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Status of the variance explanation.
    /// Values: "draft", "submitted", "approved", "rejected", "revision-requested"
    /// </summary>
    public string Status { get; set; } = "draft";
    
    /// <summary>
    /// IDs of evidence items that support this explanation.
    /// </summary>
    public List<string> EvidenceIds { get; set; } = new();
    
    /// <summary>
    /// References to related documents, decisions, or assumptions.
    /// </summary>
    public List<string> References { get; set; } = new();
    
    /// <summary>
    /// User ID who created this explanation.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// ISO 8601 timestamp when this explanation was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who last updated this explanation.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this explanation was last updated.
    /// </summary>
    public string? UpdatedAt { get; set; }
    
    /// <summary>
    /// User ID of the reviewer (if review is required and has been performed).
    /// </summary>
    public string? ReviewedBy { get; set; }
    
    /// <summary>
    /// ISO 8601 timestamp when this explanation was reviewed.
    /// </summary>
    public string? ReviewedAt { get; set; }
    
    /// <summary>
    /// Comments from the reviewer.
    /// </summary>
    public string? ReviewComments { get; set; }
    
    /// <summary>
    /// Indicates if this variance was flagged as requiring explanation.
    /// </summary>
    public bool IsFlagged { get; set; } = true;
}

/// <summary>
/// Request to create a variance threshold configuration.
/// </summary>
public sealed class CreateVarianceThresholdConfigRequest
{
    /// <summary>
    /// Percentage change threshold (e.g., 10 for 10%).
    /// </summary>
    public decimal? PercentageThreshold { get; set; }
    
    /// <summary>
    /// Absolute change threshold.
    /// </summary>
    public decimal? AbsoluteThreshold { get; set; }
    
    /// <summary>
    /// When true, both thresholds must be exceeded.
    /// </summary>
    public bool RequireBothThresholds { get; set; } = false;
    
    /// <summary>
    /// When true, explanations require reviewer approval.
    /// </summary>
    public bool RequireReviewerApproval { get; set; } = false;
    
    /// <summary>
    /// User ID creating this configuration.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a variance explanation.
/// </summary>
public sealed class CreateVarianceExplanationRequest
{
    /// <summary>
    /// ID of the data point with the variance.
    /// </summary>
    public string DataPointId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the prior reporting period.
    /// </summary>
    public string PriorPeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Explanation text.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Root cause (optional).
    /// </summary>
    public string? RootCause { get; set; }
    
    /// <summary>
    /// Category (optional).
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Evidence IDs (optional).
    /// </summary>
    public List<string> EvidenceIds { get; set; } = new();
    
    /// <summary>
    /// References (optional).
    /// </summary>
    public List<string> References { get; set; } = new();
    
    /// <summary>
    /// User creating the explanation.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a variance explanation.
/// </summary>
public sealed class UpdateVarianceExplanationRequest
{
    /// <summary>
    /// Updated explanation text.
    /// </summary>
    public string? Explanation { get; set; }
    
    /// <summary>
    /// Updated root cause.
    /// </summary>
    public string? RootCause { get; set; }
    
    /// <summary>
    /// Updated category.
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Updated evidence IDs.
    /// </summary>
    public List<string>? EvidenceIds { get; set; }
    
    /// <summary>
    /// Updated references.
    /// </summary>
    public List<string>? References { get; set; }
    
    /// <summary>
    /// User updating the explanation.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to submit a variance explanation for review.
/// </summary>
public sealed class SubmitVarianceExplanationRequest
{
    /// <summary>
    /// User submitting the explanation.
    /// </summary>
    public string SubmittedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to review (approve/reject) a variance explanation.
/// </summary>
public sealed class ReviewVarianceExplanationRequest
{
    /// <summary>
    /// Review decision: "approve" or "reject" or "request-revision".
    /// </summary>
    public string Decision { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional comments from reviewer.
    /// </summary>
    public string? Comments { get; set; }
    
    /// <summary>
    /// User performing the review.
    /// </summary>
    public string ReviewedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response containing variance flags for a data point comparison.
/// Extended MetricComparisonResponse with variance flag information.
/// </summary>
public sealed class VarianceFlagInfo
{
    /// <summary>
    /// Indicates if this variance requires explanation based on configured thresholds.
    /// </summary>
    public bool RequiresExplanation { get; set; }
    
    /// <summary>
    /// Reason why explanation is required (if applicable).
    /// Examples: "Exceeds 10% threshold", "Exceeds absolute threshold of 1000"
    /// </summary>
    public string? RequiresExplanationReason { get; set; }
    
    /// <summary>
    /// Existing variance explanation (if one exists).
    /// </summary>
    public VarianceExplanation? Explanation { get; set; }
    
    /// <summary>
    /// Indicates if the variance flag has been cleared (explanation approved or no review required).
    /// </summary>
    public bool IsFlagCleared { get; set; }
}

/// <summary>
/// Represents a maturity model for measuring reporting progress over time.
/// Maturity models are versioned to preserve historical assessments.
/// </summary>
public sealed class MaturityModel
{
    /// <summary>
    /// Unique identifier for this maturity model.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the maturity model (e.g., "ESG Reporting Maturity Framework").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the maturity model and its purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Current version of this maturity model.
    /// Incremented when levels or criteria are modified.
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Indicates if this is the active/current version of the model.
    /// Only one version can be active at a time.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Maturity levels in this model.
    /// </summary>
    public List<MaturityLevel> Levels { get; set; } = new();
    
    /// <summary>
    /// User who created this model.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the user who created this model.
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this model was created.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who last updated this model.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// User name who last updated this model.
    /// </summary>
    public string? UpdatedByName { get; set; }
    
    /// <summary>
    /// Timestamp when this model was last updated.
    /// </summary>
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a maturity level within a maturity model.
/// </summary>
public sealed class MaturityLevel
{
    /// <summary>
    /// Unique identifier for this level.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the maturity level (e.g., "Initial", "Repeatable", "Managed", "Auditable", "Optimized").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this maturity level represents.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Order/rank of this level (1 = lowest maturity, higher = more mature).
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Criteria that must be met to achieve this maturity level.
    /// </summary>
    public List<MaturityCriterion> Criteria { get; set; } = new();
}

/// <summary>
/// Represents a criterion within a maturity level.
/// Criteria can be linked to data completeness, evidence, and process controls.
/// </summary>
public sealed class MaturityCriterion
{
    /// <summary>
    /// Unique identifier for this criterion.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name/title of the criterion.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this criterion measures.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of criterion: "data-completeness", "evidence-quality", "process-control", "custom".
    /// </summary>
    public string CriterionType { get; set; } = "custom";
    
    /// <summary>
    /// Target value for this criterion (e.g., "80" for 80% completeness).
    /// </summary>
    public string TargetValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Unit of measurement (e.g., "%", "count", "yes/no").
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// For data-completeness criteria: minimum percentage of KPIs that must have values.
    /// </summary>
    public decimal? MinCompletionPercentage { get; set; }
    
    /// <summary>
    /// For evidence-quality criteria: minimum percentage of KPIs that must have evidence.
    /// </summary>
    public decimal? MinEvidencePercentage { get; set; }
    
    /// <summary>
    /// For process-control criteria: required control implementations.
    /// Examples: "approval-workflow", "dual-validation", "audit-trail"
    /// </summary>
    public List<string> RequiredControls { get; set; } = new();
    
    /// <summary>
    /// Indicates if this criterion is mandatory for the level.
    /// </summary>
    public bool IsMandatory { get; set; } = true;
}

/// <summary>
/// Request to create a new maturity model.
/// </summary>
public sealed class CreateMaturityModelRequest
{
    /// <summary>
    /// Name of the maturity model.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the maturity model.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Initial maturity levels.
    /// </summary>
    public List<MaturityLevelRequest> Levels { get; set; } = new();
    
    /// <summary>
    /// User creating the model.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// User name creating the model.
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing maturity model.
/// Creates a new version of the model.
/// </summary>
public sealed class UpdateMaturityModelRequest
{
    /// <summary>
    /// Updated name of the maturity model.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Updated description of the maturity model.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Updated maturity levels.
    /// </summary>
    public List<MaturityLevelRequest> Levels { get; set; } = new();
    
    /// <summary>
    /// User updating the model.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// User name updating the model.
    /// </summary>
    public string UpdatedByName { get; set; } = string.Empty;
}

/// <summary>
/// Request model for a maturity level.
/// </summary>
public sealed class MaturityLevelRequest
{
    /// <summary>
    /// Name of the maturity level.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the maturity level.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Order/rank of this level.
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Criteria for this level.
    /// </summary>
    public List<MaturityCriterionRequest> Criteria { get; set; } = new();
}

/// <summary>
/// Request model for a maturity criterion.
/// </summary>
public sealed class MaturityCriterionRequest
{
    /// <summary>
    /// Name of the criterion.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the criterion.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of criterion.
    /// </summary>
    public string CriterionType { get; set; } = "custom";
    
    /// <summary>
    /// Target value for this criterion.
    /// </summary>
    public string TargetValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Unit of measurement.
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum completion percentage (optional).
    /// </summary>
    public decimal? MinCompletionPercentage { get; set; }
    
    /// <summary>
    /// Minimum evidence percentage (optional).
    /// </summary>
    public decimal? MinEvidencePercentage { get; set; }
    
    /// <summary>
    /// Required controls (optional).
    /// </summary>
    public List<string> RequiredControls { get; set; } = new();
    
    /// <summary>
    /// Indicates if this criterion is mandatory.
    /// </summary>
    public bool IsMandatory { get; set; } = true;
}

/// <summary>
/// Represents a maturity assessment snapshot for a reporting period.
/// Tracks the calculated maturity score and per-criterion status at a point in time.
/// </summary>
public sealed class MaturityAssessment
{
    /// <summary>
    /// Unique identifier for this assessment.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the reporting period being assessed.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the maturity model version used for this assessment.
    /// </summary>
    public string MaturityModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Version number of the maturity model used.
    /// </summary>
    public int ModelVersion { get; set; }
    
    /// <summary>
    /// Timestamp when this assessment was calculated.
    /// </summary>
    public string CalculatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who triggered the assessment calculation.
    /// </summary>
    public string CalculatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the user who triggered the assessment.
    /// </summary>
    public string CalculatedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this is the current/latest assessment for the period.
    /// Only one assessment per period should be marked as current.
    /// </summary>
    public bool IsCurrent { get; set; } = true;
    
    /// <summary>
    /// Highest maturity level achieved based on criteria evaluation.
    /// Null if no level criteria are met.
    /// </summary>
    public string? AchievedLevelId { get; set; }
    
    /// <summary>
    /// Name of the achieved maturity level.
    /// </summary>
    public string? AchievedLevelName { get; set; }
    
    /// <summary>
    /// Order/rank of the achieved level (1 = lowest).
    /// </summary>
    public int? AchievedLevelOrder { get; set; }
    
    /// <summary>
    /// Overall maturity score (0-100).
    /// Calculated based on the percentage of criteria met across all levels.
    /// </summary>
    public decimal OverallScore { get; set; }
    
    /// <summary>
    /// Results for each criterion evaluated.
    /// </summary>
    public List<MaturityCriterionResult> CriterionResults { get; set; } = new();
    
    /// <summary>
    /// Summary statistics about the assessment.
    /// </summary>
    public MaturityAssessmentStats Stats { get; set; } = new();
}

/// <summary>
/// Result of evaluating a single maturity criterion against actual data.
/// </summary>
public sealed class MaturityCriterionResult
{
    /// <summary>
    /// ID of the maturity level this criterion belongs to.
    /// </summary>
    public string LevelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the maturity level.
    /// </summary>
    public string LevelName { get; set; } = string.Empty;
    
    /// <summary>
    /// Order of the maturity level.
    /// </summary>
    public int LevelOrder { get; set; }
    
    /// <summary>
    /// ID of the criterion.
    /// </summary>
    public string CriterionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the criterion.
    /// </summary>
    public string CriterionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of criterion.
    /// </summary>
    public string CriterionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target value defined in the criterion.
    /// </summary>
    public string TargetValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Actual measured value.
    /// </summary>
    public string ActualValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Unit of measurement.
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the criterion passed (actual >= target).
    /// </summary>
    public bool Passed { get; set; }
    
    /// <summary>
    /// Whether this criterion is mandatory for the level.
    /// </summary>
    public bool IsMandatory { get; set; }
    
    /// <summary>
    /// Status: "passed", "failed", "incomplete-data"
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Explanation of why the criterion failed or has incomplete data.
    /// Includes details about missing inputs.
    /// </summary>
    public string? FailureReason { get; set; }
    
    /// <summary>
    /// Supporting evidence IDs referenced in the calculation.
    /// </summary>
    public List<string> EvidenceIds { get; set; } = new();
}

/// <summary>
/// Summary statistics for a maturity assessment.
/// </summary>
public sealed class MaturityAssessmentStats
{
    /// <summary>
    /// Total number of criteria evaluated.
    /// </summary>
    public int TotalCriteria { get; set; }
    
    /// <summary>
    /// Number of criteria that passed.
    /// </summary>
    public int PassedCriteria { get; set; }
    
    /// <summary>
    /// Number of criteria that failed.
    /// </summary>
    public int FailedCriteria { get; set; }
    
    /// <summary>
    /// Number of criteria with incomplete data.
    /// </summary>
    public int IncompleteCriteria { get; set; }
    
    /// <summary>
    /// Data completeness percentage across all data points.
    /// </summary>
    public decimal DataCompletenessPercentage { get; set; }
    
    /// <summary>
    /// Evidence quality percentage (data points with evidence).
    /// </summary>
    public decimal EvidenceQualityPercentage { get; set; }
    
    /// <summary>
    /// Number of total data points in the period.
    /// </summary>
    public int TotalDataPoints { get; set; }
    
    /// <summary>
    /// Number of complete data points.
    /// </summary>
    public int CompleteDataPoints { get; set; }
    
    /// <summary>
    /// Number of data points with evidence.
    /// </summary>
    public int DataPointsWithEvidence { get; set; }
}

/// <summary>
/// Request to calculate a maturity assessment for a period.
/// </summary>
public sealed class CalculateMaturityAssessmentRequest
{
    /// <summary>
    /// ID of the reporting period to assess.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: ID of specific maturity model to use.
    /// If not provided, uses the active maturity model.
    /// </summary>
    public string? MaturityModelId { get; set; }
    
    /// <summary>
    /// User triggering the calculation.
    /// </summary>
    public string CalculatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the user triggering the calculation.
    /// </summary>
    public string CalculatedByName { get; set; } = string.Empty;
}

// ==================== Progress Dashboard Models ====================

/// <summary>
/// Response containing trends for completeness and maturity across multiple periods.
/// </summary>
public sealed class ProgressTrendsResponse
{
    /// <summary>
    /// Trend data for each period.
    /// </summary>
    public List<PeriodTrendData> Periods { get; set; } = new();
    
    /// <summary>
    /// Overall summary of the trends.
    /// </summary>
    public TrendsSummary Summary { get; set; } = new();
}

/// <summary>
/// Trend data for a single period.
/// </summary>
public sealed class PeriodTrendData
{
    /// <summary>
    /// Period information.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// Completeness metrics.
    /// </summary>
    public decimal CompletenessPercentage { get; set; }
    public int CompleteDataPoints { get; set; }
    public int TotalDataPoints { get; set; }
    
    /// <summary>
    /// Maturity metrics.
    /// </summary>
    public decimal? MaturityScore { get; set; }
    public string? MaturityLevel { get; set; }
    public int? MaturityLevelOrder { get; set; }
    
    /// <summary>
    /// Outstanding actions count.
    /// </summary>
    public int OpenGaps { get; set; }
    public int HighRiskGaps { get; set; }
    public int BlockedDataPoints { get; set; }
}

/// <summary>
/// Summary statistics for trends across all periods.
/// </summary>
public sealed class TrendsSummary
{
    /// <summary>
    /// Number of periods included in the trends.
    /// </summary>
    public int TotalPeriods { get; set; }
    
    /// <summary>
    /// Number of locked periods.
    /// </summary>
    public int LockedPeriods { get; set; }
    
    /// <summary>
    /// Latest period completeness.
    /// </summary>
    public decimal? LatestCompletenessPercentage { get; set; }
    
    /// <summary>
    /// Latest period maturity score.
    /// </summary>
    public decimal? LatestMaturityScore { get; set; }
    
    /// <summary>
    /// Change in completeness from previous to latest period.
    /// </summary>
    public decimal? CompletenessChange { get; set; }
    
    /// <summary>
    /// Change in maturity from previous to latest period.
    /// </summary>
    public decimal? MaturityChange { get; set; }
}

/// <summary>
/// Response containing outstanding actions across periods.
/// </summary>
public sealed class OutstandingActionsResponse
{
    /// <summary>
    /// All outstanding actions.
    /// </summary>
    public List<OutstandingAction> Actions { get; set; } = new();
    
    /// <summary>
    /// Summary statistics.
    /// </summary>
    public OutstandingActionsSummary Summary { get; set; } = new();
}

/// <summary>
/// An outstanding action requiring attention.
/// </summary>
public sealed class OutstandingAction
{
    /// <summary>
    /// Unique identifier for the action.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of action: "gap", "blocked-datapoint", "pending-approval"
    /// </summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Title/description of the action.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Period information.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public bool PeriodIsLocked { get; set; }
    
    /// <summary>
    /// Section information.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner information.
    /// </summary>
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    
    /// <summary>
    /// Priority: "high", "medium", "low"
    /// </summary>
    public string Priority { get; set; } = "medium";
    
    /// <summary>
    /// Due date or period.
    /// </summary>
    public string? DueDate { get; set; }
    
    /// <summary>
    /// Organizational unit.
    /// </summary>
    public string? OrganizationalUnitId { get; set; }
    public string? OrganizationalUnitName { get; set; }
}

/// <summary>
/// Summary of outstanding actions.
/// </summary>
public sealed class OutstandingActionsSummary
{
    public int TotalActions { get; set; }
    public int HighPriority { get; set; }
    public int MediumPriority { get; set; }
    public int LowPriority { get; set; }
    public int OpenGaps { get; set; }
    public int BlockedDataPoints { get; set; }
    public int PendingApprovals { get; set; }
}

// ============================================================================
// Year-over-Year Annex Export Models
// ============================================================================

/// <summary>
/// Request to export a year-over-year annex for auditors.
/// </summary>
public sealed class ExportYoYAnnexRequest
{
    /// <summary>
    /// ID of the current (more recent) reporting period.
    /// </summary>
    public string CurrentPeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the prior reporting period to compare against.
    /// </summary>
    public string PriorPeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional list of section IDs to include. If empty, includes all sections.
    /// </summary>
    public List<string> SectionIds { get; set; } = new();
    
    /// <summary>
    /// Include variance explanations in the export.
    /// </summary>
    public bool IncludeVarianceExplanations { get; set; } = true;
    
    /// <summary>
    /// Include evidence references according to user's access rights.
    /// </summary>
    public bool IncludeEvidenceReferences { get; set; } = true;
    
    /// <summary>
    /// Include narrative text diffs summary.
    /// </summary>
    public bool IncludeNarrativeDiffs { get; set; } = true;
    
    /// <summary>
    /// User ID who is exporting the annex.
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note about the export purpose.
    /// </summary>
    public string? ExportNote { get; set; }
}

/// <summary>
/// Result of a YoY annex export operation (metadata only).
/// </summary>
public sealed class ExportYoYAnnexResult
{
    public string ExportId { get; set; } = string.Empty;
    public string ExportedAt { get; set; } = string.Empty;
    public string ExportedBy { get; set; } = string.Empty;
    public string ExportedByName { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public long PackageSize { get; set; }
    public YoYAnnexSummary Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for a YoY annex export.
/// </summary>
public sealed class YoYAnnexSummary
{
    public string CurrentPeriodId { get; set; } = string.Empty;
    public string CurrentPeriodName { get; set; } = string.Empty;
    public string PriorPeriodId { get; set; } = string.Empty;
    public string PriorPeriodName { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public int MetricRowCount { get; set; }
    public int NarrativeComparisonCount { get; set; }
    public int VarianceExplanationCount { get; set; }
    public int EvidenceReferenceCount { get; set; }
    public int ConfidentialItemsExcluded { get; set; }
}

/// <summary>
/// Complete contents of a YoY annex package.
/// </summary>
public sealed class YoYAnnexContents
{
    public ExportMetadata Metadata { get; set; } = new();
    public ReportingPeriod CurrentPeriod { get; set; } = new();
    public ReportingPeriod PriorPeriod { get; set; } = new();
    public List<YoYAnnexSectionData> Sections { get; set; } = new();
    public List<VarianceExplanation> VarianceExplanations { get; set; } = new();
    public List<EvidenceReference> EvidenceReferences { get; set; } = new();
    public List<NarrativeDiffSummary> NarrativeDiffs { get; set; } = new();
    public YoYAnnexSummary Summary { get; set; } = new();
    public List<string> ExclusionNotes { get; set; } = new();
}

/// <summary>
/// Section-level data for YoY annex with metric comparisons.
/// </summary>
public sealed class YoYAnnexSectionData
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public List<YoYMetricRow> Metrics { get; set; } = new();
}

/// <summary>
/// Single metric row with current/prior values and variance info.
/// </summary>
public sealed class YoYMetricRow
{
    public string DataPointId { get; set; } = string.Empty;
    public string MetricTitle { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string PriorValue { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? PercentageChange { get; set; }
    public decimal? AbsoluteChange { get; set; }
    public string? VarianceExplanationId { get; set; }
    public string? VarianceExplanationSummary { get; set; }
    public bool HasVarianceFlag { get; set; }
    public int CurrentEvidenceCount { get; set; }
    public int PriorEvidenceCount { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
}

/// <summary>
/// Summary of narrative text differences between periods.
/// </summary>
public sealed class NarrativeDiffSummary
{
    public string DataPointId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int AddedSegments { get; set; }
    public int RemovedSegments { get; set; }
    public int UnchangedSegments { get; set; }
    public int TotalSegments { get; set; }
    public bool HasChanges { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
}

/// <summary>
/// Record of a YoY annex export for tracking/audit purposes.
/// </summary>
public sealed class YoYAnnexExportRecord
{
    public string Id { get; set; } = string.Empty;
    public string CurrentPeriodId { get; set; } = string.Empty;
    public string CurrentPeriodName { get; set; } = string.Empty;
    public string PriorPeriodId { get; set; } = string.Empty;
    public string PriorPeriodName { get; set; } = string.Empty;
    public List<string> SectionIds { get; set; } = new();
    public string ExportedAt { get; set; } = string.Empty;
    public string ExportedBy { get; set; } = string.Empty;
    public string ExportedByName { get; set; } = string.Empty;
    public string? ExportNote { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public long PackageSize { get; set; }
    public int MetricRowCount { get; set; }
    public int VarianceExplanationCount { get; set; }
    public int EvidenceReferenceCount { get; set; }
}

/// <summary>
/// Request to generate a report from the selected structure.
/// </summary>
public sealed class GenerateReportRequest
{
    /// <summary>
    /// The reporting period ID for which to generate the report.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional list of specific section IDs to include. If null or empty, includes all enabled sections.
    /// </summary>
    public List<string>? SectionIds { get; set; }
    
    /// <summary>
    /// User ID of the person generating the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note about this generation.
    /// </summary>
    public string? GenerationNote { get; set; }
}

/// <summary>
/// Result of report generation containing the structured output.
/// </summary>
public sealed class GeneratedReport
{
    /// <summary>
    /// Unique identifier for this generated report.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The reporting period information.
    /// </summary>
    public ReportingPeriod Period { get; set; } = new();
    
    /// <summary>
    /// Organization information.
    /// </summary>
    public Organization? Organization { get; set; }
    
    /// <summary>
    /// Ordered list of sections included in the report, sorted by Order field.
    /// Only enabled sections are included.
    /// </summary>
    public List<GeneratedReportSection> Sections { get; set; } = new();
    
    /// <summary>
    /// Timestamp when the report was generated (ISO 8601 format).
    /// </summary>
    public string GeneratedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// User name of the person who generated the report.
    /// </summary>
    public string GeneratedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note about this generation.
    /// </summary>
    public string? GenerationNote { get; set; }
    
    /// <summary>
    /// SHA-256 checksum of the report content for integrity verification.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// A section included in a generated report with its associated data.
/// </summary>
public sealed class GeneratedReportSection
{
    /// <summary>
    /// Section metadata.
    /// </summary>
    public ReportSection Section { get; set; } = new();
    
    /// <summary>
    /// Owner information for the section.
    /// </summary>
    public User? Owner { get; set; }
    
    /// <summary>
    /// Data points included in this section with their latest values.
    /// </summary>
    public List<DataPointSnapshot> DataPoints { get; set; } = new();
    
    /// <summary>
    /// Evidence attached to this section.
    /// </summary>
    public List<EvidenceMetadata> Evidence { get; set; } = new();
    
    /// <summary>
    /// Active assumptions for this section.
    /// </summary>
    public List<AssumptionRecord> Assumptions { get; set; } = new();
    
    /// <summary>
    /// Active gaps for this section.
    /// </summary>
    public List<GapRecord> Gaps { get; set; } = new();
}

/// <summary>
/// Snapshot of a data point's current state for report generation.
/// </summary>
public sealed class DataPointSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string InformationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? LastUpdatedAt { get; set; }
    public int EvidenceCount { get; set; }
    public bool HasAssumptions { get; set; }
}

/// <summary>
/// Evidence metadata for report generation.
/// </summary>
public sealed class EvidenceMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UploadedAt { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Checksum of the evidence file for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }
    
    /// <summary>
    /// Integrity status of the evidence file.
    /// </summary>
    public string IntegrityStatus { get; set; } = "not-checked";
    
    /// <summary>
    /// URL or path to the evidence file.
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// Description of the evidence.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether the current user has permission to view this attachment.
    /// Set based on user permissions during export generation.
    /// </summary>
    public bool IsAccessible { get; set; } = true;
    
    /// <summary>
    /// Reason for restriction if not accessible.
    /// </summary>
    public string? RestrictionReason { get; set; }
}

/// <summary>
/// Assumption record for report generation.
/// </summary>
public sealed class AssumptionRecord
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Gap record for report generation.
/// </summary>
public sealed class GapRecord
{
    public string Id { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MissingReason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

// ============================================================================
// Report Variants - Audience-specific report generation
// ============================================================================

/// <summary>
/// Defines the type of audience for a report variant.
/// </summary>
public enum AudienceType
{
    Management,
    Bank,
    Client,
    Auditor,
    Regulator,
    InternalTeam,
    Custom
}

/// <summary>
/// Report variant configuration for generating audience-specific reports.
/// </summary>
public sealed class ReportVariant
{
    /// <summary>
    /// Unique identifier for this variant.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the variant (e.g., "Management Summary", "Bank Disclosure").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of this variant and its purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Target audience for this variant.
    /// </summary>
    public string AudienceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Rules for filtering sections and data points.
    /// </summary>
    public List<VariantRule> Rules { get; set; } = new();
    
    /// <summary>
    /// Redaction rules for masking sensitive information.
    /// </summary>
    public List<RedactionRule> RedactionRules { get; set; } = new();
    
    /// <summary>
    /// Whether this variant is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User ID who created this variant.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// User name who created this variant.
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when this variant was created (ISO 8601 format).
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who last modified this variant.
    /// </summary>
    public string? LastModifiedBy { get; set; }
    
    /// <summary>
    /// User name who last modified this variant.
    /// </summary>
    public string? LastModifiedByName { get; set; }
    
    /// <summary>
    /// Timestamp when this variant was last modified (ISO 8601 format).
    /// </summary>
    public string? LastModifiedAt { get; set; }
}

/// <summary>
/// Rule for including or excluding content in a report variant.
/// </summary>
public sealed class VariantRule
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of rule: "include-section", "exclude-section", "include-field-group", "exclude-field-group", "exclude-attachments".
    /// </summary>
    public string RuleType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target of the rule (section ID, field group name, etc.).
    /// </summary>
    public string Target { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional condition for when this rule applies.
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// Order in which rules are applied (lower values first).
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Rule for redacting sensitive information in a report variant.
/// </summary>
public sealed class RedactionRule
{
    /// <summary>
    /// Unique identifier for this redaction rule.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Field or data point identifier to redact.
    /// </summary>
    public string FieldIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of redaction: "mask", "remove", "replace".
    /// </summary>
    public string RedactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Replacement value (for "replace" type).
    /// </summary>
    public string? ReplacementValue { get; set; }
    
    /// <summary>
    /// Reason for redaction (for audit trail).
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new report variant.
/// </summary>
public sealed class CreateVariantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public List<VariantRule> Rules { get; set; } = new();
    public List<RedactionRule> RedactionRules { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing report variant.
/// </summary>
public sealed class UpdateVariantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public List<VariantRule> Rules { get; set; } = new();
    public List<RedactionRule> RedactionRules { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to generate a report using a specific variant.
/// </summary>
public sealed class GenerateVariantRequest
{
    /// <summary>
    /// The reporting period ID for which to generate the variant report.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// The variant ID to use for generation.
    /// </summary>
    public string VariantId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID of the person generating the variant report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional note about this generation.
    /// </summary>
    public string? GenerationNote { get; set; }
}

/// <summary>
/// Result of variant report generation.
/// </summary>
public sealed class GeneratedReportVariant
{
    /// <summary>
    /// The base generated report.
    /// </summary>
    public GeneratedReport Report { get; set; } = new();
    
    /// <summary>
    /// The variant configuration used for generation.
    /// </summary>
    public ReportVariant Variant { get; set; } = new();
    
    /// <summary>
    /// List of section IDs that were excluded by variant rules.
    /// </summary>
    public List<string> ExcludedSections { get; set; } = new();
    
    /// <summary>
    /// List of field identifiers that were redacted.
    /// </summary>
    public List<string> RedactedFields { get; set; } = new();
    
    /// <summary>
    /// Count of attachments excluded by variant rules.
    /// </summary>
    public int ExcludedAttachmentCount { get; set; }
}

/// <summary>
/// Request to compare multiple report variants.
/// </summary>
public sealed class CompareVariantsRequest
{
    /// <summary>
    /// The reporting period ID for comparison.
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// List of variant IDs to compare (2 or more).
    /// </summary>
    public List<string> VariantIds { get; set; } = new();
    
    /// <summary>
    /// User ID requesting the comparison.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
}

/// <summary>
/// Result of comparing multiple report variants.
/// </summary>
public sealed class VariantComparison
{
    /// <summary>
    /// The reporting period being compared.
    /// </summary>
    public ReportingPeriod Period { get; set; } = new();
    
    /// <summary>
    /// List of variants being compared.
    /// </summary>
    public List<ReportVariant> Variants { get; set; } = new();
    
    /// <summary>
    /// Section-level differences between variants.
    /// </summary>
    public List<SectionDifference> SectionDifferences { get; set; } = new();
    
    /// <summary>
    /// Field-level differences between variants.
    /// </summary>
    public List<FieldDifference> FieldDifferences { get; set; } = new();
    
    /// <summary>
    /// Timestamp when comparison was performed.
    /// </summary>
    public string ComparedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who requested the comparison.
    /// </summary>
    public string ComparedBy { get; set; } = string.Empty;
}

/// <summary>
/// Difference in section inclusion between variants.
/// </summary>
public sealed class SectionDifference
{
    /// <summary>
    /// Section ID.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Section name.
    /// </summary>
    public string SectionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Variant IDs that include this section.
    /// </summary>
    public List<string> IncludedInVariants { get; set; } = new();
    
    /// <summary>
    /// Variant IDs that exclude this section.
    /// </summary>
    public List<string> ExcludedFromVariants { get; set; } = new();
    
    /// <summary>
    /// Reason for exclusion (from variant rules).
    /// </summary>
    public string? ExclusionReason { get; set; }
}

/// <summary>
/// Difference in field/data point visibility between variants.
/// </summary>
public sealed class FieldDifference
{
    /// <summary>
    /// Field/data point identifier.
    /// </summary>
    public string FieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// Field/data point name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Section containing this field.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Variant IDs where this field is visible (not redacted).
    /// </summary>
    public List<string> VisibleInVariants { get; set; } = new();
    
    /// <summary>
    /// Variant IDs where this field is redacted or removed.
    /// </summary>
    public List<string> RedactedInVariants { get; set; } = new();
    
    /// <summary>
    /// Redaction type applied.
    /// </summary>
    public string? RedactionType { get; set; }
    
    /// <summary>
    /// Reason for redaction.
    /// </summary>
    public string? RedactionReason { get; set; }
}
