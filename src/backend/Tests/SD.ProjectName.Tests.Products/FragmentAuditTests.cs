using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for fragment audit traceability functionality.
/// </summary>
public class FragmentAuditTests
{
    private static void CreateTestConfiguration(InMemoryReportStore store)
    {
        // Create organization
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

        // Create organizational unit
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Organization Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });

        // Create reporting period
        store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });
    }

    private static string CreateTestSection(InMemoryReportStore store)
    {
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var section = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = periodId,
            Title = "Test Section",
            Category = "environmental",
            Description = "Test section for audit",
            OwnerId = "user-1",
            Status = "draft",
            Completeness = "empty",
            Order = 1,
            CatalogCode = "ENV-001"
        };
        
        var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sections = sectionsField!.GetValue(store) as List<ReportSection>;
        sections!.Add(section);
        
        return section.Id;
    }

    [Fact]
    public void GetFragmentAuditView_WithSection_ShouldReturnAuditView()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        // Act
        var auditView = store.GetFragmentAuditView("section", sectionId);

        // Assert
        Assert.NotNull(auditView);
        Assert.Equal("section", auditView.FragmentType);
        Assert.Equal(sectionId, auditView.FragmentId);
        Assert.Equal("Test Section", auditView.FragmentTitle);
        Assert.NotNull(auditView.StableFragmentIdentifier);
        Assert.Equal("ENV-001", auditView.StableFragmentIdentifier);
    }

    [Fact]
    public void GetFragmentAuditView_WithDataPoint_ShouldReturnAuditView()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);
        
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Manual entry",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        
        var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(dpValid, dpError);
        Assert.NotNull(dataPoint);

        // Act
        var auditView = store.GetFragmentAuditView("data-point", dataPoint.Id);

        // Assert
        Assert.NotNull(auditView);
        Assert.Equal("data-point", auditView.FragmentType);
        Assert.Equal(dataPoint.Id, auditView.FragmentId);
        Assert.Equal("Test Data Point", auditView.FragmentTitle);
        Assert.NotNull(auditView.StableFragmentIdentifier);
        Assert.NotNull(auditView.SectionInfo);
        Assert.Equal("Test Section", auditView.SectionInfo.SectionTitle);
    }

    [Fact]
    public void GetFragmentAuditView_WithoutEvidence_ShouldShowProvenanceWarning()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);
        
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Title = "Test Data Point",
            Content = "Test content without evidence",
            OwnerId = "user-1",
            Source = "Manual entry",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        
        var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(dpValid, dpError);
        Assert.NotNull(dataPoint);

        // Act
        var auditView = store.GetFragmentAuditView("data-point", dataPoint.Id);

        // Assert
        Assert.NotNull(auditView);
        Assert.NotEmpty(auditView.ProvenanceWarnings);
        Assert.Contains(auditView.ProvenanceWarnings, w => w.MissingLinkType == "source");
        Assert.Contains(auditView.ProvenanceWarnings, w => w.MissingLinkType == "evidence");
    }

    [Fact]
    public void GenerateStableFragmentIdentifier_ForSection_ShouldUseCatalogCode()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        // Act
        var identifier = store.GenerateStableFragmentIdentifier("section", sectionId);

        // Assert
        Assert.Equal("ENV-001", identifier);
    }

    [Fact]
    public void GenerateStableFragmentIdentifier_ForDataPoint_ShouldIncludeSectionCatalogCode()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);
        
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Manual entry",
            InformationType = "fact",
            CompletenessStatus = "complete"
        };
        
        var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(dpValid, dpError);
        Assert.NotNull(dataPoint);

        // Act
        var identifier = store.GenerateStableFragmentIdentifier("data-point", dataPoint.Id);

        // Assert
        Assert.StartsWith("dp-ENV-001-", identifier);
        Assert.Contains(dataPoint.Id, identifier);
    }
}
