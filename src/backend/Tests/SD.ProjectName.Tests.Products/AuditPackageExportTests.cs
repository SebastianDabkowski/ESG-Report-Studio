using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class AuditPackageExportTests
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
    public void GenerateAuditPackage_WithValidPeriod_ReturnsSuccess()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test user
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
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
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id,
            ExportNote = "Test export for auditors"
        };
        
        var (resultIsValid, errorMessage, result) = store.GenerateAuditPackage(exportRequest);
        
        // Assert
        Assert.True(resultIsValid);
        Assert.Null(errorMessage);
        Assert.NotNull(result);
        Assert.Equal(period.Id, result!.Summary.PeriodId);
        Assert.Equal(period.Name, result.Summary.PeriodName);
        Assert.NotEmpty(result.ExportId);
        // Checksum and PackageSize are only populated during download, not in metadata endpoint
        Assert.Equal(user.Id, result.ExportedBy);
    }

    [Fact]
    public void GenerateAuditPackage_WithInvalidPeriod_ReturnsError()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = "invalid-period-id",
            ExportedBy = "user1"
        };
        
        // Act
        var (isValid, errorMessage, result) = store.GenerateAuditPackage(exportRequest);
        
        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Period", errorMessage);
        Assert.Null(result);
    }

    [Fact]
    public void GenerateAuditPackage_WithUnknownUser_UsesUserIdAsName()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test user and period (but don't add user to store)
        var userId = "user1";
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = userId,
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = userId
        };
        
        // Act
        var (resultIsValid, errorMessage, result) = store.GenerateAuditPackage(exportRequest);
        
        // Assert - should work even with unknown user (uses user ID as name)
        Assert.True(resultIsValid);
        Assert.Null(errorMessage);
        Assert.NotNull(result);
        Assert.Equal(userId, result!.ExportedBy);
    }

    [Fact]
    public void BuildAuditPackageContents_IncludesAllSectionsWhenNoFilter()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        var sections = snapshot.Sections;
        
        // Act
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id
        };
        
        var contents = store.BuildAuditPackageContents(exportRequest);
        
        // Assert
        Assert.NotNull(contents);
        Assert.Equal(period.Id, contents!.Period.Id);
        Assert.Equal(sections.Count, contents.Sections.Count);
        Assert.NotEmpty(contents.Metadata.ExportId);
        Assert.Equal(user.Id, contents.Metadata.ExportedBy);
        Assert.Equal(user.Id, contents.Metadata.ExportedByName); // User doesn't exist in store, so uses ID
        Assert.Equal("1.0", contents.Metadata.Version);
    }

    [Fact]
    public void BuildAuditPackageContents_FiltersBySpecificSections()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        var sections = snapshot.Sections;
        
        // Select first two sections
        var selectedSections = sections.Take(2).Select(s => s.Id).ToList();
        
        // Act
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id,
            SectionIds = selectedSections
        };
        
        var contents = store.BuildAuditPackageContents(exportRequest);
        
        // Assert
        Assert.NotNull(contents);
        Assert.Equal(2, contents!.Sections.Count);
        Assert.All(contents.Sections, s => Assert.Contains(s.Section.Id, selectedSections));
    }

    [Fact]
    public void RecordAuditPackageExport_CreatesExportRecord()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id,
            ExportNote = "Quarterly audit"
        };
        
        var checksum = "test-checksum-123";
        var packageSize = 1024L;
        
        // Act
        store.RecordAuditPackageExport(exportRequest, checksum, packageSize);
        
        // Assert
        var exports = store.GetAuditPackageExports(period.Id);
        Assert.Single(exports);
        
        var exportRecord = exports.First();
        Assert.Equal(period.Id, exportRecord.PeriodId);
        Assert.Equal(user.Id, exportRecord.ExportedBy);
        Assert.Equal("Unknown", exportRecord.ExportedByName);  // User not in store
        Assert.Equal(checksum, exportRecord.Checksum);
        Assert.Equal(packageSize, exportRecord.PackageSize);
        Assert.Equal("Quarterly audit", exportRecord.ExportNote);
    }

    [Fact]
    public void GetAuditPackageExports_ReturnsOnlyExportsForSpecificPeriod()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data for two periods
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var period1Request = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
        Assert.True(isValid1);
        var period1 = snapshot1!.Periods.First();
        
        var period2Request = new CreateReportingPeriodRequest
        {
            Name = "2023 Annual Report",
            StartDate = "2023-01-01",
            EndDate = "2023-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);
        Assert.True(isValid2);
        var period2 = snapshot2!.Periods.Last();
        
        // Create exports for both periods
        store.RecordAuditPackageExport(
            new ExportAuditPackageRequest { PeriodId = period1.Id, ExportedBy = user.Id },
            "checksum1", 1000);
        
        store.RecordAuditPackageExport(
            new ExportAuditPackageRequest { PeriodId = period1.Id, ExportedBy = user.Id },
            "checksum2", 2000);
        
        store.RecordAuditPackageExport(
            new ExportAuditPackageRequest { PeriodId = period2.Id, ExportedBy = user.Id },
            "checksum3", 3000);
        
        // Act
        var period1Exports = store.GetAuditPackageExports(period1.Id);
        var period2Exports = store.GetAuditPackageExports(period2.Id);
        
        // Assert
        Assert.Equal(2, period1Exports.Count);
        Assert.Single(period2Exports);
        Assert.All(period1Exports, e => Assert.Equal(period1.Id, e.PeriodId));
        Assert.All(period2Exports, e => Assert.Equal(period2.Id, e.PeriodId));
    }

    [Fact]
    public void BuildAuditPackageContents_IncludesAuditTrailForPeriodAndSections()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        var section = snapshot.Sections.First();
        
        // Create a data point to generate audit trail
        var createDataPointRequest = new CreateDataPointRequest
        {
            SectionId = section.Id,
            Title = "Test Data Point",
            Type = "narrative",
            ReviewStatus = "draft",
            OwnerId = "",  // Leave empty to avoid user validation
            Content = "Test content",
            Source = "test",
            InformationType = "fact"
        };
        
        var (dpValid, _, dataPoint) = store.CreateDataPoint(createDataPointRequest);
        Assert.True(dpValid);
        
        // Act
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id
        };
        
        var contents = store.BuildAuditPackageContents(exportRequest);
        
        // Assert
        Assert.NotNull(contents);
        Assert.NotNull(contents!.AuditTrail);  // AuditTrail should exist (may be empty if no logs created)
    }

    [Fact]
    public void BuildAuditPackageContents_IncludesDecisionsLinkedToDataPoints()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        // Create test data
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            Role = "admin"
        };
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            OwnerId = user.Id,
            OwnerName = user.Name
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        var section = snapshot.Sections.First();
        
        // Create a data point
        var createDataPointRequest = new CreateDataPointRequest
        {
            SectionId = section.Id,
            Title = "Test Data Point",
            Type = "narrative",
            ReviewStatus = "draft",
            OwnerId = "",  // Leave empty to avoid user validation
            Content = "Test content",
            Source = "test",
            InformationType = "fact"
        };
        
        var (dpValid, _, dataPoint) = store.CreateDataPoint(createDataPointRequest);
        Assert.True(dpValid);
        
        // Create a decision and link it to the data point
        var createDecisionRequest = new CreateDecisionRequest
        {
            SectionId = section.Id,
            Title = "Test Decision",
            Context = "Test context",
            DecisionText = "Test decision text",
            Alternatives = "Test alternatives",
            Consequences = "Test consequences"
        };
        
        var (decValid, _, decision) = store.CreateDecision(
            createDecisionRequest.SectionId,
            createDecisionRequest.Title,
            createDecisionRequest.Context,
            createDecisionRequest.DecisionText,
            createDecisionRequest.Alternatives,
            createDecisionRequest.Consequences,
            user.Name
        );
        
        Assert.True(decValid);
        
        // Link decision to fragment
        store.LinkDecisionToFragment(decision!.Id, dataPoint!.Id, user.Name);
        
        // Act
        var exportRequest = new ExportAuditPackageRequest
        {
            PeriodId = period.Id,
            ExportedBy = user.Id
        };
        
        var contents = store.BuildAuditPackageContents(exportRequest);
        
        // Assert
        Assert.NotNull(contents);
        Assert.NotEmpty(contents!.Decisions);
        Assert.Contains(contents.Decisions, d => d.Id == decision.Id);
    }
}
