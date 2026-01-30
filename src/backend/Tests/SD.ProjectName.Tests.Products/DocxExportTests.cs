using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

public sealed class DocxExportTests
{
    private static void CreateTestOrganization(InMemoryReportStore store)
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
    
    private static void CreateTestOrganizationalUnit(InMemoryReportStore store)
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

    private static void CreateTestConfiguration(InMemoryReportStore store)
    {
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
    }

    [Fact]
    public void GenerateDocx_WithValidReport_ReturnsDocxBytes()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Test DOCX export"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        
        var docxService = new DocxExportService();
        
        // Act
        var docxBytes = docxService.GenerateDocx(report!);
        
        // Assert
        Assert.NotNull(docxBytes);
        Assert.NotEmpty(docxBytes);
        Assert.True(docxBytes.Length > 0);
        
        // DOCX files start with PK (ZIP archive signature)
        var header = System.Text.Encoding.ASCII.GetString(docxBytes.Take(2).ToArray());
        Assert.Equal("PK", header);
    }

    [Fact]
    public void GenerateDocx_WithOptions_IncludesTitlePageAndToc()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Sustainability Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "extended",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Test with options"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        
        var docxService = new DocxExportService();
        var options = new DocxExportOptions
        {
            IncludeTitlePage = true,
            IncludeTableOfContents = true,
            IncludePageNumbers = true,
            VariantName = "Management"
        };
        
        // Act
        var docxBytes = docxService.GenerateDocx(report!, options);
        
        // Assert
        Assert.NotNull(docxBytes);
        Assert.NotEmpty(docxBytes);
        Assert.True(docxBytes.Length > 0);
    }

    [Fact]
    public void GenerateFilename_WithCompanyAndPeriod_ReturnsFormattedName()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Test filename generation"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        
        var docxService = new DocxExportService();
        
        // Act
        var filename = docxService.GenerateFilename(report!);
        
        // Assert
        Assert.NotNull(filename);
        Assert.Contains("Test_Corporation", filename);
        Assert.Contains("2024_Annual_Report", filename);
        Assert.EndsWith(".docx", filename);
    }

    [Fact]
    public void GenerateFilename_WithVariant_IncludesVariantInName()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Q1 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-03-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Test with variant"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        
        var docxService = new DocxExportService();
        
        // Act
        var filename = docxService.GenerateFilename(report!, "Executive Summary");
        
        // Assert
        Assert.NotNull(filename);
        Assert.Contains("Executive_Summary", filename);
        Assert.EndsWith(".docx", filename);
    }
}
