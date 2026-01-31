using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using System.Text.Json;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for JSON export functionality with versioned schema.
/// </summary>
public sealed class JsonExportTests
{
    [Fact]
    public void GenerateJson_WithValidReport_ReturnsJsonBytes()
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
            GenerationNote = "Test JSON export"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        
        var jsonService = new JsonExportService();
        
        // Act
        var jsonBytes = jsonService.GenerateJson(report!);
        
        // Assert
        Assert.NotNull(jsonBytes);
        Assert.NotEmpty(jsonBytes);
        
        // Verify it's valid JSON
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        Assert.NotNull(jsonString);
        
        // Parse to verify structure
        var container = JsonSerializer.Deserialize<JsonExportContainer>(jsonString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(container);
        Assert.NotNull(container.ExportMetadata);
        Assert.NotNull(container.Report);
    }
    
    [Fact]
    public void GenerateJson_IncludesVersionMetadata()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 ESG Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "extended",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Version test"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        var jsonService = new JsonExportService();
        var options = new JsonExportOptions
        {
            UserId = "user1",
            UserName = "Test User",
            VariantName = "Standard"
        };
        
        // Act
        var jsonBytes = jsonService.GenerateJson(report!, options);
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var container = JsonSerializer.Deserialize<JsonExportContainer>(jsonString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Assert
        Assert.NotNull(container);
        Assert.Equal("json", container.ExportMetadata.Format);
        Assert.Equal("1.0.0", container.ExportMetadata.SchemaVersion);
        Assert.Equal("esg-report-studio/json/v1", container.ExportMetadata.SchemaIdentifier);
        Assert.Equal("user1", container.ExportMetadata.ExportedBy);
        Assert.Equal("Test User", container.ExportMetadata.ExportedByName);
        Assert.Equal(period.Id, container.ExportMetadata.PeriodId);
        Assert.Equal(period.Name, container.ExportMetadata.PeriodName);
        Assert.Equal("Standard", container.ExportMetadata.VariantName);
        Assert.NotEmpty(container.ExportMetadata.ExportId);
    }
    
    [Fact]
    public void GenerateJson_FiltersDataBasedOnOptions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        var jsonService = new JsonExportService();
        
        // Test with all filters disabled
        var options = new JsonExportOptions
        {
            IncludeEvidence = false,
            IncludeAssumptions = false,
            IncludeGaps = false
        };
        
        // Act
        var jsonBytes = jsonService.GenerateJson(report!, options);
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var container = JsonSerializer.Deserialize<JsonExportContainer>(jsonString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Report);
        
        // Verify filters were applied
        foreach (var section in container.Report.Sections)
        {
            Assert.Empty(section.Evidence);
            Assert.Empty(section.Assumptions);
            Assert.Empty(section.Gaps);
        }
    }
    
    [Fact]
    public void GenerateJson_SupportsCompactOutput()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        var jsonService = new JsonExportService();
        
        // Act - formatted output
        var formattedOptions = new JsonExportOptions { FormatOutput = true };
        var formattedBytes = jsonService.GenerateJson(report!, formattedOptions);
        var formattedString = System.Text.Encoding.UTF8.GetString(formattedBytes);
        
        // Act - compact output
        var compactOptions = new JsonExportOptions { FormatOutput = false };
        var compactBytes = jsonService.GenerateJson(report!, compactOptions);
        var compactString = System.Text.Encoding.UTF8.GetString(compactBytes);
        
        // Assert
        // Compact should be significantly smaller
        Assert.True(compactBytes.Length < formattedBytes.Length);
        
        // Both should deserialize to same data
        var formattedContainer = JsonSerializer.Deserialize<JsonExportContainer>(formattedString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        var compactContainer = JsonSerializer.Deserialize<JsonExportContainer>(compactString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(formattedContainer);
        Assert.NotNull(compactContainer);
        Assert.Equal(formattedContainer.Report.Id, compactContainer.Report.Id);
    }
    
    [Fact]
    public void GenerateJson_UsesCustomSchemaVersion()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        var jsonService = new JsonExportService();
        
        // Use a custom schema version (simulating future backward compatibility)
        var customVersion = new ExportSchemaVersion(1, 0, 0, "json");
        var options = new JsonExportOptions
        {
            SchemaVersion = customVersion
        };
        
        // Act
        var jsonBytes = jsonService.GenerateJson(report!, options);
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var container = JsonSerializer.Deserialize<JsonExportContainer>(jsonString, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Assert
        Assert.NotNull(container);
        Assert.Equal("1.0.0", container.ExportMetadata.SchemaVersion);
    }
    
    [Fact]
    public void GenerateFilename_IncludesCompanyPeriodAndDate()
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
        var period = snapshot!.Periods.First();
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        var jsonService = new JsonExportService();
        
        // Act
        var filename = jsonService.GenerateFilename(report!, "Executive");
        
        // Assert
        Assert.Contains(".json", filename);
        Assert.Contains("2024_Annual_Report", filename);
        Assert.Contains("Executive", filename);
    }
}
