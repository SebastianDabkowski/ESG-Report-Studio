namespace ARP.ESG_ReportStudio.API.Reporting;

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
}

public sealed class SectionSummary : ReportSection
{
    public int DataPointCount { get; set; }
    public int EvidenceCount { get; set; }
    public int GapCount { get; set; }
    public int AssumptionCount { get; set; }
    public int CompletenessPercentage { get; set; }
    public string OwnerName { get; set; } = string.Empty;
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
}

public sealed class UpdateReportingPeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ReportingMode { get; set; } = "simplified";
    public string ReportScope { get; set; } = "single-company";
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
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string CompletenessStatus { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string> EvidenceIds { get; set; } = new();
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
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string CompletenessStatus { get; set; } = string.Empty;
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
    public string Source { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public string CompletenessStatus { get; set; } = string.Empty;
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
    /// Optional URL to external source.
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
