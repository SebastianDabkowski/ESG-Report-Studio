using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class YearOverYearAnnexTests
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
        
        // Create prior period (2023)
        var priorPeriodReq = new CreateReportingPeriodRequest
        {
            Name = "2023 Annual Report",
            StartDate = "2023-01-01",
            EndDate = "2023-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "admin",
            OwnerName = "Admin User"
        };
        
        var (priorValid, _, priorSnapshot) = store.ValidateAndCreatePeriod(priorPeriodReq);
        Assert.True(priorValid);
        var priorPeriod = priorSnapshot!.Periods.First();
        var priorSections = priorSnapshot.Sections;
        var priorSection = priorSections.First();
        
        // Create current period (2024)
        var currentPeriodReq = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "admin",
            OwnerName = "Admin User"
        };
        
        var (currentValid, _, currentSnapshot) = store.ValidateAndCreatePeriod(currentPeriodReq);
        Assert.True(currentValid);
        var currentPeriod = currentSnapshot!.Periods.First();
        var currentSections = currentSnapshot.Sections;
        var currentSection = currentSections.First();
        
        // Create data points in prior period
        var priorDp1Req = new CreateDataPointRequest
        {
            SectionId = priorSection.Id,
            Type = "metric",
            Title = "Total Energy Consumption",
            Content = "Energy consumed across all facilities",
            Value = "1000",
            Unit = "MWh",
            OwnerId = "admin",
            Source = "Energy management system",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        var (priorDp1Valid, _, priorDp1) = store.CreateDataPoint(priorDp1Req);
        Assert.True(priorDp1Valid);
        
        var priorDp2Req = new CreateDataPointRequest
        {
            SectionId = priorSection.Id,
            Type = "metric",
            Title = "GHG Emissions",
            Content = "Total greenhouse gas emissions",
            Value = "500",
            Unit = "tCO2e",
            OwnerId = "admin",
            Source = "Emissions calculator",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        var (priorDp2Valid, _, priorDp2) = store.CreateDataPoint(priorDp2Req);
        Assert.True(priorDp2Valid);
        
        // Create data points in current period
        var currentDp1Req = new CreateDataPointRequest
        {
            SectionId = currentSection.Id,
            Type = "metric",
            Title = "Total Energy Consumption",
            Content = "Energy consumed across all facilities",
            Value = "1200",
            Unit = "MWh",
            OwnerId = "admin",
            Source = "Energy management system",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        var (currentDp1Valid, _, currentDp1) = store.CreateDataPoint(currentDp1Req);
        Assert.True(currentDp1Valid);
        
        // Link current dp1 to prior dp1
        currentDp1!.SourceDataPointId = priorDp1!.Id;
        currentDp1.SourcePeriodId = priorPeriod.Id;
        
        var currentDp2Req = new CreateDataPointRequest
        {
            SectionId = currentSection.Id,
            Type = "metric",
            Title = "GHG Emissions",
            Content = "Total greenhouse gas emissions",
            Value = "450",
            Unit = "tCO2e",
            OwnerId = "admin",
            Source = "Emissions calculator",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        var (currentDp2Valid, _, currentDp2) = store.CreateDataPoint(currentDp2Req);
        Assert.True(currentDp2Valid);
        
        // Link current dp2 to prior dp2
        currentDp2!.SourceDataPointId = priorDp2!.Id;
        currentDp2.SourcePeriodId = priorPeriod.Id;
        
        return store;
    }
    
    [Fact]
    public void GenerateYoYAnnex_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin",
            IncludeVarianceExplanations = true,
            IncludeEvidenceReferences = true,
            IncludeNarrativeDiffs = true
        };
        
        // Act
        var (isValid, errorMessage, result) = store.GenerateYoYAnnex(request);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(result);
        Assert.NotEmpty(result.ExportId);
        Assert.Equal("admin", result.ExportedBy);
        Assert.NotNull(result.Summary);
    }
    
    [Fact]
    public void GenerateYoYAnnex_MissingCurrentPeriodId_ReturnsError()
    {
        // Arrange
        var store = CreateTestStore();
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = "",
            PriorPeriodId = "prior-id",
            ExportedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, result) = store.GenerateYoYAnnex(request);
        
        // Assert
        Assert.False(isValid);
        Assert.Equal("CurrentPeriodId is required.", errorMessage);
        Assert.Null(result);
    }
    
    [Fact]
    public void GenerateYoYAnnex_MissingPriorPeriodId_ReturnsError()
    {
        // Arrange
        var store = CreateTestStore();
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = "current-id",
            PriorPeriodId = "",
            ExportedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, result) = store.GenerateYoYAnnex(request);
        
        // Assert
        Assert.False(isValid);
        Assert.Equal("PriorPeriodId is required.", errorMessage);
        Assert.Null(result);
    }
    
    [Fact]
    public void GenerateYoYAnnex_InvalidCurrentPeriod_ReturnsError()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = "invalid-id",
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin"
        };
        
        // Act
        var (isValid, errorMessage, result) = store.GenerateYoYAnnex(request);
        
        // Assert
        Assert.False(isValid);
        Assert.Contains("not found", errorMessage);
        Assert.Null(result);
    }
    
    [Fact]
    public void BuildYoYAnnexContents_IncludesMetricComparisons()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin"
        };
        
        // Act
        var contents = store.BuildYoYAnnexContents(request);
        
        // Assert
        Assert.NotNull(contents);
        Assert.NotEmpty(contents.Sections);
        
        var section = contents.Sections[0];
        Assert.NotEmpty(section.Metrics);
        
        // Check energy consumption metric
        var energyMetric = section.Metrics.FirstOrDefault(m => m.MetricTitle.Contains("Energy"));
        Assert.NotNull(energyMetric);
        Assert.Equal("1200", energyMetric.CurrentValue);
        Assert.Equal("1000", energyMetric.PriorValue);
        Assert.Equal(20m, energyMetric.PercentageChange); // (1200-1000)/1000 * 100 = 20%
        Assert.Equal(200m, energyMetric.AbsoluteChange); // 1200-1000 = 200
    }
    
    [Fact]
    public void BuildYoYAnnexContents_CalculatesNegativeChanges()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin"
        };
        
        // Act
        var contents = store.BuildYoYAnnexContents(request);
        
        // Assert
        var section = contents.Sections[0];
        var ghgMetric = section.Metrics.FirstOrDefault(m => m.MetricTitle.Contains("GHG"));
        
        Assert.NotNull(ghgMetric);
        Assert.Equal("450", ghgMetric.CurrentValue);
        Assert.Equal("500", ghgMetric.PriorValue);
        Assert.Equal(-10m, ghgMetric.PercentageChange); // (450-500)/500 * 100 = -10%
        Assert.Equal(-50m, ghgMetric.AbsoluteChange); // 450-500 = -50
    }
    
    [Fact]
    public void BuildYoYAnnexContents_IncludesSummaryStatistics()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin"
        };
        
        // Act
        var contents = store.BuildYoYAnnexContents(request);
        
        // Assert
        Assert.NotNull(contents.Summary);
        Assert.Equal(currentPeriod.Id, contents.Summary.CurrentPeriodId);
        Assert.Equal(currentPeriod.Name, contents.Summary.CurrentPeriodName);
        Assert.Equal(priorPeriod.Id, contents.Summary.PriorPeriodId);
        Assert.Equal(priorPeriod.Name, contents.Summary.PriorPeriodName);
        Assert.Equal(1, contents.Summary.SectionCount);
        Assert.Equal(2, contents.Summary.MetricRowCount);
    }
    
    [Fact]
    public void RecordYoYAnnexExport_CreatesAuditLogEntry()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin",
            ExportNote = "Test export"
        };
        
        var checksum = "test-checksum";
        var packageSize = 12345L;
        
        // Act
        store.RecordYoYAnnexExport(request, checksum, packageSize);
        
        // Get audit log
        var auditLogs = store.GetAuditLog();
        
        // Assert
        var logEntry = auditLogs.FirstOrDefault(e => e.Action == "yoy-annex-exported");
        Assert.NotNull(logEntry);
        Assert.Equal("yoy-annex-exported", logEntry.Action);
        Assert.Equal("admin", logEntry.UserId);
        Assert.Equal("yoy-annex", logEntry.EntityType);
    }
    
    [Fact]
    public void GetYoYAnnexExports_ReturnsExportHistory()
    {
        // Arrange
        var store = CreateTestStore();
        var periods = store.GetPeriods().ToList();
        var currentPeriod = periods.First(p => p.Name.Contains("2024"));
        var priorPeriod = periods.First(p => p.Name.Contains("2023"));
        
        var request = new ExportYoYAnnexRequest
        {
            CurrentPeriodId = currentPeriod.Id,
            PriorPeriodId = priorPeriod.Id,
            ExportedBy = "admin"
        };
        
        // Record two exports
        store.RecordYoYAnnexExport(request, "checksum1", 10000);
        store.RecordYoYAnnexExport(request, "checksum2", 12000);
        
        // Act
        var exports = store.GetYoYAnnexExports(currentPeriod.Id);
        
        // Assert
        Assert.Equal(2, exports.Count);
        Assert.All(exports, export =>
        {
            Assert.Equal(currentPeriod.Id, export.CurrentPeriodId);
            Assert.Equal(currentPeriod.Name, export.CurrentPeriodName);
            Assert.Equal(priorPeriod.Id, export.PriorPeriodId);
            Assert.Equal(priorPeriod.Name, export.PriorPeriodName);
        });
    }
}
