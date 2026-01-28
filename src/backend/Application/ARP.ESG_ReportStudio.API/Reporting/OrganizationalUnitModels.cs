namespace ARP.ESG_ReportStudio.API.Reporting;

public sealed class OrganizationalUnit
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class CreateOrganizationalUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationalUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
