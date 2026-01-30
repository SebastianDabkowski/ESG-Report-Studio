using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

public sealed class PdfExportTests
{
    [Fact]
    public void GeneratePdf_WithValidReport_ReturnsPdfBytes()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
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
            GenerationNote = "Test PDF export"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        
        var pdfService = new PdfExportService(ExportTestHelpers.CreateTestLocalizationService());
        
        // Act
        var pdfBytes = pdfService.GeneratePdf(report!);
        
        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        // PDF files start with %PDF
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public void GeneratePdf_WithOptions_IncludesTitlePageAndToc()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
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
        
        var pdfService = new PdfExportService(ExportTestHelpers.CreateTestLocalizationService());
        var options = new PdfExportOptions
        {
            IncludeTitlePage = true,
            IncludeTableOfContents = true,
            IncludePageNumbers = true,
            VariantName = "Management"
        };
        
        // Act
        var pdfBytes = pdfService.GeneratePdf(report!, options);
        
        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void GenerateFilename_WithCompanyAndPeriod_ReturnsFormattedName()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
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
        
        var pdfService = new PdfExportService(ExportTestHelpers.CreateTestLocalizationService());
        
        // Act
        var filename = pdfService.GenerateFilename(report!);
        
        // Assert
        Assert.NotNull(filename);
        Assert.Contains("Test_Corporation", filename);
        Assert.Contains("2024_Annual_Report", filename);
        Assert.EndsWith(".pdf", filename);
    }

    [Fact]
    public void GenerateFilename_WithVariant_IncludesVariantInName()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
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
        
        var pdfService = new PdfExportService(ExportTestHelpers.CreateTestLocalizationService());
        
        // Act
        var filename = pdfService.GenerateFilename(report!, "Executive Summary");
        
        // Assert
        Assert.NotNull(filename);
        Assert.Contains("Executive_Summary", filename);
        Assert.EndsWith(".pdf", filename);
    }
}
