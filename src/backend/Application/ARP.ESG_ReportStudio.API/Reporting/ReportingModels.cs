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

public sealed class ReportingDataSnapshot
{
    public Organization? Organization { get; set; }
    public IReadOnlyList<ReportingPeriod> Periods { get; set; } = Array.Empty<ReportingPeriod>();
    public IReadOnlyList<ReportSection> Sections { get; set; } = Array.Empty<ReportSection>();
    public IReadOnlyList<SectionSummary> SectionSummaries { get; set; } = Array.Empty<SectionSummary>();
    public IReadOnlyList<OrganizationalUnit> OrganizationalUnits { get; set; } = Array.Empty<OrganizationalUnit>();
}
