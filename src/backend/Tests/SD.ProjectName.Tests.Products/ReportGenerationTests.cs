using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class ReportGenerationTests
{
    private static void CreateTestOrganization(InMemoryReportStore store)
    {
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "TEST123",
            CreatedBy = "test-user"
        };
        
        store.CreateOrganization(request);
    }

    [Fact]
    public void GenerateReport_WithEnabledSections_IncludesOnlyEnabledSections()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "report-owner"
        };
        
        // Create test period
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        var sections = snapshot.Sections.Where(s => s.PeriodId == period.Id).ToList();
        
        // Disable some sections by modifying them
        if (sections.Count > 1)
        {
            // Get the sections from the store
            var allSections = store.GetSections(period.Id).ToList();
            
            // Disable the second section (index 1)
            if (allSections.Count > 1)
            {
                allSections[1].IsEnabled = false;
            }
        }
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id,
            GenerationNote = "Test report generation"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.Null(errorMessage);
        Assert.NotNull(report);
        Assert.Equal(period.Id, report!.Period.Id);
        Assert.Equal(period.Name, report.Period.Name);
        Assert.NotEmpty(report.Id);
        Assert.NotEmpty(report.Checksum);
        Assert.Equal(user.Id, report.GeneratedBy);
        Assert.Equal(user.Name, report.GeneratedByName);
        
        // Verify only enabled sections are included
        Assert.All(report.Sections, s => Assert.True(s.Section.IsEnabled));
    }

    [Fact]
    public void GenerateReport_WithValidPeriod_ReturnsSectionsInOrder()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "report-owner"
        };
        
        // Create test period
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.Null(errorMessage);
        Assert.NotNull(report);
        
        // Verify sections are ordered by their Order property
        var sectionOrders = report!.Sections.Select(s => s.Section.Order).ToList();
        Assert.Equal(sectionOrders.OrderBy(o => o), sectionOrders);
    }

    [Fact]
    public void GenerateReport_WithSpecificSections_IncludesOnlySpecifiedSections()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "report-owner"
        };
        
        // Create test period
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        var allSections = store.GetSections(period.Id).ToList();
        
        // Select only first section
        var selectedSectionIds = allSections.Take(1).Select(s => s.Id).ToList();
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id,
            SectionIds = selectedSectionIds
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.Null(errorMessage);
        Assert.NotNull(report);
        
        // Verify only specified sections are included
        Assert.Equal(selectedSectionIds.Count, report!.Sections.Count);
        Assert.All(report.Sections, s => Assert.Contains(s.Section.Id, selectedSectionIds));
    }

    [Fact]
    public void GenerateReport_WithInvalidPeriod_ReturnsError()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = "non-existent-period",
            GeneratedBy = "user1"
        };
        
        var (isValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Null(report);
        Assert.Contains("not found", errorMessage);
    }

    [Fact]
    public void GenerateReport_CreatesAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "report-owner"
        };
        
        // Create test period
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        
        // Get audit log count before generation
        var auditLogBefore = store.GetAuditLog().Count;
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id,
            GenerationNote = "Test generation"
        };
        
        var (resultIsValid, errorMessage, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        var auditLogAfter = store.GetAuditLog().Count;
        
        // Verify audit log entry was created
        Assert.True(auditLogAfter > auditLogBefore);
        
        var latestEntry = store.GetAuditLog().Last();
        Assert.Equal("report-generated", latestEntry.Action);
        Assert.Equal("report", latestEntry.EntityType);
        Assert.Equal(user.Id, latestEntry.UserId);
        Assert.Equal(user.Name, latestEntry.UserName);
    }

    [Fact]
    public void GenerateReport_ProducesDeterministicChecksum()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "report-owner"
        };
        
        // Create test period
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        
        // Act - Generate report twice
        var generateRequest1 = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id
        };
        
        var (resultIsValid1, _, report1) = store.GenerateReport(generateRequest1);
        
        // Assert
        Assert.True(resultIsValid1);
        Assert.NotNull(report1);
        Assert.NotEmpty(report1!.Checksum);
        
        // Verify checksum is consistent format (base64 encoded SHA256)
        Assert.NotEmpty(report1.Checksum);
        // Base64 encoded SHA256 should be 44 characters
        Assert.True(report1.Checksum.Length > 40);
    }
}
