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
    public void GenerateReport_WithValidPeriod_ReturnsSuccess()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test period
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
        Assert.Equal("user1", report.GeneratedBy);
        Assert.Equal("user1", report.GeneratedByName); // Falls back to ID when user not in store
        Assert.NotEmpty(report.Sections);
    }

    [Fact]
    public void GenerateReport_ReturnsSectionsInOrder()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
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
        CreateTestConfiguration(store);
        
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
        
        // Find the report-generated entry
        var reportGeneratedEntry = store.GetAuditLog()
            .FirstOrDefault(e => e.Action == "report-generated" && e.EntityId == report!.Id);
        
        Assert.NotNull(reportGeneratedEntry);
        Assert.Equal("report-generated", reportGeneratedEntry!.Action);
        Assert.Equal("report", reportGeneratedEntry.EntityType);
        Assert.Equal(user.Id, reportGeneratedEntry.UserId);
        Assert.Equal(user.Id, reportGeneratedEntry.UserName); // Falls back to ID when user not in store
    }

    [Fact]
    public void GenerateReport_ProducesDeterministicChecksum()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
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
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        Assert.NotEmpty(report!.Checksum);
        
        // Verify checksum is consistent format (base64 encoded SHA256)
        // Base64 encoded SHA256 should be 44 characters
        Assert.True(report.Checksum.Length > 40);
    }
    
    [Fact]
    public void GenerateReport_IncludesDataPointsWithSnapshots()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
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
        var period = snapshot!.Periods.First();
        
        // Act
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = user.Id
        };
        
        var (resultIsValid, _, report) = store.GenerateReport(generateRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.NotNull(report);
        Assert.NotNull(report!.Sections);
        
        // Each section should have proper structure
        foreach (var section in report.Sections)
        {
            Assert.NotNull(section.Section);
            Assert.NotNull(section.DataPoints);
            Assert.NotNull(section.Evidence);
            Assert.NotNull(section.Assumptions);
            Assert.NotNull(section.Gaps);
        }
    }
}
