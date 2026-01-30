using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class ReportPreviewTests
{
    private static void CreateTestConfiguration(InMemoryReportStore store)
    {
        var orgRequest = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "TEST123",
            CreatedBy = "admin"
        };
        store.CreateOrganization(orgRequest);
        
        var unitRequest = new CreateOrganizationalUnitRequest
        {
            Name = "Headquarters",
            ParentId = null,
            Description = "Main office",
            CreatedBy = "admin"
        };
        store.CreateOrganizationalUnit(unitRequest);
    }
    
    private InMemoryReportStore CreateTestStore()
    {
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create a reporting period
        var periodReq = new CreateReportingPeriodRequest
        {
            Name = "2024 ESG Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Sarah Chen"
        };
        
        var (valid, _, snapshot) = store.ValidateAndCreatePeriod(periodReq);
        Assert.True(valid);
        var period = snapshot!.Periods.First();
        var sections = snapshot.Sections;
        
        // Create data points in first section
        var section1 = sections.First();
        store.UpdateSectionOwner(section1.Id, new UpdateSectionOwnerRequest
        {
            OwnerId = "user-1",
            UpdatedBy = "admin"
        });
        
        var dp1Req = new CreateDataPointRequest
        {
            SectionId = section1.Id,
            Type = "metric",
            Title = "Total GHG Emissions",
            Content = "Scope 1 and 2 emissions",
            Value = "5000",
            Unit = "tCO2e",
            OwnerId = "user-1",
            ContributorIds = new List<string>(),
            Source = "Emissions tracking system",
            InformationType = "measured"
        };
        store.CreateDataPoint(dp1Req);
        
        // Create data points in second section
        if (sections.Count > 1)
        {
            var section2 = sections.Skip(1).First();
            store.UpdateSectionOwner(section2.Id, new UpdateSectionOwnerRequest
            {
                OwnerId = "user-2",
                UpdatedBy = "admin"
            });
            
            var dp2Req = new CreateDataPointRequest
            {
                SectionId = section2.Id,
                Type = "metric",
                Title = "Employee Training Hours",
                Content = "Total training hours provided",
                Value = "1000",
                Unit = "hours",
                OwnerId = "user-2",
                ContributorIds = new List<string>(),
                Source = "HR system",
                InformationType = "measured"
            };
            store.CreateDataPoint(dp2Req);
        }
        
        return store;
    }
    
    [Fact]
    public void GenerateReport_ReturnsAllEnabledSections()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "admin",
            GenerationNote = "Test report"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(report);
        Assert.NotEmpty(report.Sections);
        Assert.Equal(period.Id, report.Period.Id);
        Assert.Equal("admin", report.GeneratedBy);
    }
    
    [Fact]
    public void GenerateReport_IncludesDataPointsInSections()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(report);
        
        // Check that sections have data points
        var sectionsWithData = report.Sections.Where(s => s.DataPoints.Count > 0).ToList();
        Assert.NotEmpty(sectionsWithData);
        
        // Verify data point structure
        var firstDataPoint = sectionsWithData.First().DataPoints.First();
        Assert.NotEmpty(firstDataPoint.Title);
        Assert.NotEmpty(firstDataPoint.Value);
    }
    
    [Fact]
    public void GenerateReport_WithSpecificSections_OnlyIncludesRequestedSections()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        var sections = store.GetSections(period.Id);
        var firstSectionId = sections.First().Id;
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "admin",
            SectionIds = new List<string> { firstSectionId }
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(report);
        Assert.Single(report.Sections);
        Assert.Equal(firstSectionId, report.Sections.First().Section.Id);
    }
    
    [Fact]
    public void GenerateReport_OnlyIncludesEnabledSections()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(report);
        
        // Verify all sections in report are enabled
        Assert.All(report.Sections, s => Assert.True(s.Section.IsEnabled));
    }
    
    [Fact]
    public void GenerateReport_IncludesOwnerInformation()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(report);
        
        // Check that sections with owners have owner information populated
        var sectionsWithOwners = report.Sections.Where(s => s.Owner != null).ToList();
        Assert.NotEmpty(sectionsWithOwners);
        
        foreach (var section in sectionsWithOwners)
        {
            Assert.NotEmpty(section.Owner!.Name);
            Assert.NotEmpty(section.Owner.Id);
        }
    }
    
    [Fact]
    public void GenerateReport_InvalidPeriodId_ReturnsError()
    {
        // Arrange
        var store = CreateTestStore();
        
        var request = new GenerateReportRequest
        {
            PeriodId = "invalid-period-id",
            GeneratedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Null(report);
        Assert.Contains("not found", errorMessage);
    }
    
    [Fact]
    public void GenerateReport_IncludesChecksumForIntegrity()
    {
        // Arrange
        var store = CreateTestStore();
        var period = store.GetPeriods().First();
        
        var request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, report) = store.GenerateReport(request);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(report);
        Assert.NotEmpty(report.Checksum);
        Assert.NotEmpty(report.Id);
        Assert.NotEmpty(report.GeneratedAt);
    }
}
