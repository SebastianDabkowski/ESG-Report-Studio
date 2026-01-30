using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public class MetricComparisonDebugTests
{
    [Fact]
    public void Debug_CreateDataPoint()
    {
        var store = new InMemoryReportStore();
        
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "LLC",
            Country = "US",
            Identifier = "12345",
            CreatedBy = "test-user",
            CoverageType = "full",
            CoverageJustification = "Test coverage"
        });
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Organization Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });
        
        var request = new CreateReportingPeriodRequest
        {
            Name = "FY2023",
            StartDate = "2023-01-01",
            EndDate = "2023-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User",
            OrganizationId = "org1"
        };

        var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);
        Assert.True(isValid, $"Period creation failed: {errorMessage}");
        
        var period = snapshot!.Periods.First(p => p.Name == "FY2023");
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = section.Id,
            Title = "Total Energy Consumption",
            Content = "Annual energy consumption",
            Value = "1000",
            Unit = "MWh",
            OwnerId = "user1",
            Source = "Energy meters",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        
        var (isValidDP, errorMessageDP, dataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(isValidDP, $"DataPoint creation failed: {errorMessageDP}");
        Assert.NotNull(dataPoint);
    }
}
