using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Shared test utilities for export tests.
/// </summary>
public static class ExportTestHelpers
{
    public static void CreateTestOrganization(InMemoryReportStore store)
    {
        var request = new CreateOrganizationRequest
        {
            Name = "Test Corporation",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "TEST123",
            CreatedBy = "test-user"
        };
        
        store.CreateOrganization(request);
    }
    
    public static void CreateTestOrganizationalUnit(InMemoryReportStore store)
    {
        var request = new CreateOrganizationalUnitRequest
        {
            Name = "Headquarters",
            ParentId = null,
            Description = "Main office",
            CreatedBy = "test-user"
        };
        
        store.CreateOrganizationalUnit(request);
    }

    public static void CreateTestConfiguration(InMemoryReportStore store)
    {
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
    }
}
