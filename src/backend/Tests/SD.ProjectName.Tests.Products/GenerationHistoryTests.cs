using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class GenerationHistoryTests
{
    [Fact]
    public void GenerateReport_TracksHistoryEntry()
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
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "First generation"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        
        var history = store.GetGenerationHistory(period.Id);
        Assert.Single(history);
        
        var entry = history.First();
        Assert.Equal(report!.Id, entry.Id);
        Assert.Equal(period.Id, entry.PeriodId);
        Assert.Equal("user1", entry.GeneratedBy);
        Assert.Equal("user1", entry.GeneratedByName); // Will be user ID if user not found in store
        Assert.Equal("First generation", entry.GenerationNote);
        Assert.Equal("draft", entry.Status);
        Assert.NotEmpty(entry.Checksum);
        Assert.True(entry.SectionCount > 0);
    }
    
    [Fact]
    public void GetGeneration_ReturnsCorrectEntry()
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
            GeneratedBy = "user1",
            GenerationNote = "Test generation"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Act
        var entry = store.GetGeneration(report!.Id);
        
        // Assert
        Assert.NotNull(entry);
        Assert.Equal(report.Id, entry!.Id);
        Assert.NotNull(entry.Report);
    }
    
    [Fact]
    public void MarkGenerationAsFinal_UpdatesStatus()
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
            GeneratedBy = "user1",
            GenerationNote = "Draft version"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Act
        var markFinalRequest = new MarkGenerationFinalRequest
        {
            GenerationId = report!.Id,
            UserId = "user1",
            UserName = "Test User",
            Note = "Approved for publication"
        };
        
        var (isSuccess, error, entry) = store.MarkGenerationAsFinal(markFinalRequest);
        
        // Assert
        Assert.True(isSuccess);
        Assert.Null(error);
        Assert.NotNull(entry);
        Assert.Equal("final", entry!.Status);
        Assert.NotNull(entry.MarkedFinalAt);
        Assert.Equal("user1", entry.MarkedFinalBy);
        Assert.Equal("Test User", entry.MarkedFinalByName);
    }
    
    [Fact]
    public void MarkGenerationAsFinal_AlreadyFinal_ReturnsError()
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
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        var markFinalRequest = new MarkGenerationFinalRequest
        {
            GenerationId = report!.Id,
            UserId = "user1",
            UserName = "Test User"
        };
        
        store.MarkGenerationAsFinal(markFinalRequest);
        
        // Act - try to mark final again
        var (isSuccess, error, entry) = store.MarkGenerationAsFinal(markFinalRequest);
        
        // Assert
        Assert.False(isSuccess);
        Assert.NotNull(error);
        Assert.Contains("already marked as final", error);
    }
    
    [Fact]
    public void CompareGenerations_DetectsSectionDifferences()
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
        
        // Generate first version
        var gen1Request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Version 1"
        };
        
        var (valid1, error1, report1) = store.GenerateReport(gen1Request);
        Assert.True(valid1);
        
        // Generate second version (should be identical)
        var gen2Request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1",
            GenerationNote = "Version 2"
        };
        
        var (valid2, error2, report2) = store.GenerateReport(gen2Request);
        Assert.True(valid2);
        
        // Act
        var compareRequest = new CompareGenerationsRequest
        {
            Generation1Id = report1!.Id,
            Generation2Id = report2!.Id,
            UserId = "user1"
        };
        
        var (isSuccess, error, comparison) = store.CompareGenerations(compareRequest);
        
        // Assert
        Assert.True(isSuccess);
        Assert.NotNull(comparison);
        Assert.Equal(report1.Id, comparison!.Generation1.Id);
        Assert.Equal(report2.Id, comparison.Generation2.Id);
        Assert.NotEmpty(comparison.SectionDifferences);
        Assert.NotNull(comparison.Summary);
    }
    
    [Fact]
    public void CompareGenerations_DifferentPeriods_ReturnsError()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriod1Request = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(createPeriod1Request);
        var period1 = snapshot1!.Periods.First();
        
        var createPeriod2Request = new CreateReportingPeriodRequest
        {
            Name = "2025 Annual Report",
            StartDate = "2025-01-01",
            EndDate = "2025-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User"
        };
        
        var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(createPeriod2Request);
        var period2 = snapshot2!.Periods.Last();
        
        var gen1Request = new GenerateReportRequest
        {
            PeriodId = period1.Id,
            GeneratedBy = "user1"
        };
        
        var (valid1, error1, report1) = store.GenerateReport(gen1Request);
        
        var gen2Request = new GenerateReportRequest
        {
            PeriodId = period2.Id,
            GeneratedBy = "user1"
        };
        
        var (valid2, error2, report2) = store.GenerateReport(gen2Request);
        
        // Act
        var compareRequest = new CompareGenerationsRequest
        {
            Generation1Id = report1!.Id,
            Generation2Id = report2!.Id,
            UserId = "user1"
        };
        
        var (isSuccess, error, comparison) = store.CompareGenerations(compareRequest);
        
        // Assert
        Assert.False(isSuccess);
        Assert.NotNull(error);
        Assert.Contains("different periods", error);
    }
    
    [Fact]
    public void RecordExport_AddsToHistory()
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
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Act
        var exportEntry = new ExportHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            GenerationId = report!.Id,
            PeriodId = period.Id,
            Format = "pdf",
            FileName = "report.pdf",
            FileSize = 1024,
            FileChecksum = "checksum123",
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = "user1",
            ExportedByName = "Test User",
            IncludedTitlePage = true,
            IncludedTableOfContents = true,
            IncludedAttachments = false
        };
        
        store.RecordExport(exportEntry);
        
        // Assert
        var exportHistory = store.GetExportHistory(period.Id);
        Assert.Single(exportHistory);
        
        var entry = exportHistory.First();
        Assert.Equal("pdf", entry.Format);
        Assert.Equal("report.pdf", entry.FileName);
        Assert.Equal(1024, entry.FileSize);
    }
    
    [Fact]
    public void GetExportHistoryForGeneration_FiltersCorrectly()
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
        
        // Generate two reports
        var gen1Request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (valid1, error1, report1) = store.GenerateReport(gen1Request);
        
        var gen2Request = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user1"
        };
        
        var (valid2, error2, report2) = store.GenerateReport(gen2Request);
        
        // Record exports for each generation
        var export1 = new ExportHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            GenerationId = report1!.Id,
            PeriodId = period.Id,
            Format = "pdf",
            FileName = "report1.pdf",
            FileSize = 1024,
            FileChecksum = "checksum1",
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = "user1",
            ExportedByName = "Test User"
        };
        
        var export2 = new ExportHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            GenerationId = report2!.Id,
            PeriodId = period.Id,
            Format = "docx",
            FileName = "report2.docx",
            FileSize = 2048,
            FileChecksum = "checksum2",
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = "user1",
            ExportedByName = "Test User"
        };
        
        store.RecordExport(export1);
        store.RecordExport(export2);
        
        // Act
        var gen1Exports = store.GetExportHistoryForGeneration(report1.Id);
        
        // Assert
        Assert.Single(gen1Exports);
        Assert.Equal("pdf", gen1Exports.First().Format);
    }
}
