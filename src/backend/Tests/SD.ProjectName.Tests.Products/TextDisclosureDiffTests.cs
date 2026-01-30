using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using System.Reflection;

namespace SD.ProjectName.Tests.Products;

public sealed class TextDisclosureDiffTests
{
    // Helper method to create test periods with sections and data points
    private (InMemoryReportStore store, string period1Id, string section1Id, string period2Id, string section2Id) 
        SetupTestPeriodsAndSections()
    {
        var store = new InMemoryReportStore();
        
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

        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Organization Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });

        // Period 1
        store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2023",
            StartDate = "2023-01-01",
            EndDate = "2023-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "owner-1",
            OwnerName = "Test Owner"
        });

        var snapshot1 = store.GetSnapshot();
        var period1 = snapshot1.Periods.First(p => p.Name == "2023");

        // Period 2
        store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "owner-1",
            OwnerName = "Test Owner"
        });

        var snapshot2 = store.GetSnapshot();
        var period2 = snapshot2.Periods.First(p => p.Name == "2024");

        // Create sections using reflection
        var section1 = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = period1.Id,
            Title = "Energy",
            CatalogCode = "ENV-001",
            Category = "environmental",
            Description = "Energy and emissions",
            OwnerId = "owner-1",
            Status = "in-progress",
            Completeness = "partial",
            Order = 1
        };

        var section2 = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = period2.Id,
            Title = "Energy",
            CatalogCode = "ENV-001",
            Category = "environmental",
            Description = "Energy and emissions",
            OwnerId = "owner-1",
            Status = "in-progress",
            Completeness = "partial",
            Order = 1
        };

        var sectionsField = typeof(InMemoryReportStore).GetField("_sections",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var sections = sectionsField!.GetValue(store) as List<ReportSection>;
        sections!.Add(section1);
        sections!.Add(section2);

        return (store, period1.Id, section1.Id, period2.Id, section2.Id);
    }

    [Fact]
    public void TextDiffService_WordLevelDiff_IdentifiesAddedWords()
    {
        // Arrange
        var service = new TextDiffService();
        var oldText = "The company achieved carbon neutrality.";
        var newText = "The company achieved full carbon neutrality.";

        // Act
        var segments = service.ComputeWordLevelDiff(oldText, newText);

        // Assert
        Assert.NotEmpty(segments);
        Assert.Contains(segments, s => s.ChangeType == "added" && s.Text.Contains("full"));
    }

    [Fact]
    public void TextDiffService_WordLevelDiff_IdentifiesRemovedWords()
    {
        // Arrange
        var service = new TextDiffService();
        var oldText = "The company achieved full carbon neutrality.";
        var newText = "The company achieved carbon neutrality.";

        // Act
        var segments = service.ComputeWordLevelDiff(oldText, newText);

        // Assert
        Assert.NotEmpty(segments);
        Assert.Contains(segments, s => s.ChangeType == "removed" && s.Text.Contains("full"));
    }

    [Fact]
    public void TextDiffService_WordLevelDiff_IdentifiesUnchangedText()
    {
        // Arrange
        var service = new TextDiffService();
        var oldText = "The company achieved carbon neutrality.";
        var newText = "The company achieved carbon neutrality.";

        // Act
        var segments = service.ComputeWordLevelDiff(oldText, newText);

        // Assert
        Assert.All(segments, s => Assert.Equal("unchanged", s.ChangeType));
    }

    [Fact]
    public void TextDiffService_SentenceLevelDiff_IdentifiesAddedSentences()
    {
        // Arrange
        var service = new TextDiffService();
        var oldText = "We reduced emissions by 20%.";
        var newText = "We reduced emissions by 20%. Additionally, we planted 1000 trees.";

        // Act
        var segments = service.ComputeSentenceLevelDiff(oldText, newText);

        // Assert
        Assert.NotEmpty(segments);
        Assert.Contains(segments, s => s.ChangeType == "added" && s.Text.Contains("trees"));
    }

    [Fact]
    public void TextDiffService_GenerateSummary_ReturnsCorrectStatistics()
    {
        // Arrange
        var service = new TextDiffService();
        var oldText = "Old text here.";
        var newText = "New text here.";

        // Act
        var summary = service.GenerateSummary(oldText, newText);

        // Assert
        Assert.True(summary.HasChanges);
        Assert.True(summary.TotalSegments > 0);
    }

    [Fact]
    public void CompareTextDisclosures_DraftCopyNotEdited_ShowsNoChanges()
    {
        // Arrange
        var (store, period1Id, section1Id, period2Id, section2Id) = SetupTestPeriodsAndSections();

        var (_, _, dp1) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section1Id,
            Title = "Energy Disclosure",
            Content = "We consumed 1000 MWh in 2023.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "Energy System",
            InformationType = "fact",
            CompletenessStatus = "complete"
        });

        // Create draft copy with same content (not yet edited)
        var (_, _, dp2) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section2Id,
            Title = "Energy Disclosure",
            Content = "We consumed 1000 MWh in 2023.", // Same content
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "Energy System",
            InformationType = "fact",
            CompletenessStatus = "complete",
            ReviewStatus = "draft"
        });

        // Set rollover lineage
        var dataPoint2 = store.GetDataPoint(dp2!.Id);
        if (dataPoint2 != null)
        {
            dataPoint2.SourceDataPointId = dp1!.Id;
            dataPoint2.SourcePeriodId = period1Id;
        }

        // Act
        var (success, error, response) = store.CompareTextDisclosures(dp2!.Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.True(response.IsDraftCopy);
        Assert.False(response.HasBeenEdited);
        Assert.False(response.Summary.HasChanges);
    }

    [Fact]
    public void CompareTextDisclosures_DraftCopyEdited_ShowsChanges()
    {
        // Arrange
        var (store, period1Id, section1Id, period2Id, section2Id) = SetupTestPeriodsAndSections();

        var (_, _, dp1) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section1Id,
            Title = "Energy Disclosure",
            Content = "We consumed 1000 MWh.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "Energy System",
            InformationType = "fact",
            CompletenessStatus = "complete"
        });

        // Create draft copy with edited content
        var (_, _, dp2) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section2Id,
            Title = "Energy Disclosure",
            Content = "We consumed 1200 MWh with improved efficiency.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "Energy System",
            InformationType = "fact",
            CompletenessStatus = "complete",
            ReviewStatus = "draft"
        });

        // Set rollover lineage
        var dataPoint2 = store.GetDataPoint(dp2!.Id);
        if (dataPoint2 != null)
        {
            dataPoint2.SourceDataPointId = dp1!.Id;
            dataPoint2.SourcePeriodId = period1Id;
        }

        // Act
        var (success, error, response) = store.CompareTextDisclosures(dp2!.Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.True(response.IsDraftCopy);
        Assert.True(response.HasBeenEdited);
        Assert.True(response.Summary.HasChanges);
        Assert.True(response.Summary.AddedSegments > 0 || response.Summary.RemovedSegments > 0);
    }

    [Fact]
    public void CompareTextDisclosures_NoPreviousVersion_ShowsAllAsAdded()
    {
        // Arrange
        var (store, _, _, _, section2Id) = SetupTestPeriodsAndSections();

        var (_, _, dp) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section2Id,
            Title = "New Disclosure",
            Content = "This is new content.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "System",
            InformationType = "fact",
            CompletenessStatus = "complete"
        });

        // Act
        var (success, error, response) = store.CompareTextDisclosures(dp!.Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.Null(response.PreviousDataPoint);
        Assert.Single(response.Segments);
        Assert.Equal("added", response.Segments[0].ChangeType);
        Assert.True(response.Summary.HasChanges);
    }

    [Fact]
    public void CompareTextDisclosures_WithExplicitPreviousPeriod_FindsMatchingDataPoint()
    {
        // Arrange
        var (store, period1Id, section1Id, _, section2Id) = SetupTestPeriodsAndSections();

        var (_, _, dp1) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section1Id,
            Title = "Energy Disclosure",
            Content = "Old version.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "System",
            InformationType = "fact",
            CompletenessStatus = "complete"
        });

        var (_, _, dp2) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section2Id,
            Title = "Energy Disclosure",
            Content = "New version.",
            Type = "narrative",
            OwnerId = "owner-1",
            Source = "System",
            InformationType = "fact",
            CompletenessStatus = "complete"
        });

        // Act - explicitly specify previous period
        var (success, error, response) = store.CompareTextDisclosures(dp2!.Id, period1Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(response);
        Assert.NotNull(response.PreviousDataPoint);
        Assert.Equal(dp1!.Id, response.PreviousDataPoint.Id);
        Assert.True(response.Summary.HasChanges);
    }
}
