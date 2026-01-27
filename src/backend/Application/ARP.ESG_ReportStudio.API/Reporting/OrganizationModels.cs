namespace ARP.ESG_ReportStudio.API.Reporting;

public sealed class Organization
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalForm { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string LegalForm { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string LegalForm { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
}
