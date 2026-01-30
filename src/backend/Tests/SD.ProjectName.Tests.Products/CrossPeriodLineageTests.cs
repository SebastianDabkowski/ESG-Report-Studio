using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for cross-period lineage tracking functionality.
/// Verifies that data points maintain lineage information when rolled over across reporting periods.
/// </summary>
public class CrossPeriodLineageTests
{
    private static void CreateTestOrganization(InMemoryReportStore store)
    {
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
    }

    private static void CreateTestOrganizationalUnit(InMemoryReportStore store)
    {
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Organization Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });
    }

    private static string CreateTestPeriod(InMemoryReportStore store, string name)
    {
        var request = new CreateReportingPeriodRequest
        {
            Name = name,
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user1",
            OwnerName = "Test User",
            OrganizationId = "org1"
        };

        var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);
        Assert.True(isValid, errorMessage);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First(p => p.Name == name);
        return period.Id;
    }
    
    [Fact]
    public void RolloverPeriod_ShouldPopulateLineageFieldsInDataPoints()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        var sourcePeriodId = CreateTestPeriod(store, "FY2023");
        
        // Get automatically created section
        var sections = store.GetSections(sourcePeriodId);
        var section = sections.First();
        
        // Create data point in source period
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = section.Id,
            Title = "Total Energy",
            Content = "Annual energy consumption",
            Value = "1000",
            Unit = "MWh",
            OwnerId = "user1",
            Source = "Energy meters",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        var (isValid, _, sourceDataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(isValid);
        Assert.NotNull(sourceDataPoint);
        
        // Act - Rollover to new period
        var rolloverRequest = new RolloverRequest
        {
            SourcePeriodId = sourcePeriodId,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true
            }
        };
        
        var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
        
        // Assert
        Assert.True(success, errorMessage);
        Assert.NotNull(result);
        Assert.NotNull(result.TargetPeriod);
        
        // Get target period sections
        var targetSections = store.GetSections(result.TargetPeriod.Id);
        Assert.NotEmpty(targetSections);
        
        var targetSection = targetSections.First();
        
        // Get data points in target period
        var targetDataPoints = store.GetDataPoints(targetSection.Id);
        Assert.Single(targetDataPoints);
        
        var targetDataPoint = targetDataPoints[0];
        
        // Verify lineage fields are populated
        Assert.Equal(sourcePeriodId, targetDataPoint.SourcePeriodId);
        Assert.Equal("FY2023", targetDataPoint.SourcePeriodName);
        Assert.Equal(sourceDataPoint.Id, targetDataPoint.SourceDataPointId);
        Assert.NotNull(targetDataPoint.RolloverTimestamp);
        Assert.Equal("user1", targetDataPoint.RolloverPerformedBy);
        Assert.NotNull(targetDataPoint.RolloverPerformedByName);
    }
    
    [Fact]
    public void GetCrossPeriodLineage_ShouldReturnCompleteLineageChain()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        // Create Period 1 (2022)
        var period1Id = CreateTestPeriod(store, "FY2022");
        var section1 = store.GetSections(period1Id).First();
        
        var (_, _, dp1) = store.CreateDataPoint(new CreateDataPointRequest
        {
            SectionId = section1.Id,
            Title = "Total Energy",
            Content = "Energy consumption 2022",
            Value = "800",
            Unit = "MWh",
            OwnerId = "user1",
            Source = "Meters",
            InformationType = "fact",
            Type = "metric"
        });
        
        // Rollover to Period 2 (2023)
        var rollover1 = store.RolloverPeriod(new RolloverRequest
        {
            SourcePeriodId = period1Id,
            TargetPeriodName = "FY2023",
            TargetPeriodStartDate = "2023-01-01",
            TargetPeriodEndDate = "2023-12-31",
            PerformedBy = "user1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true
            }
        });
        
        Assert.True(rollover1.Success);
        
        var section2 = store.GetSections(rollover1.Result!.TargetPeriod!.Id).First();
        var dp2 = store.GetDataPoints(section2.Id).First();
        
        // Update value in period 2
        store.UpdateDataPoint(dp2.Id, new UpdateDataPointRequest
        {
            Title = dp2.Title,
            Content = "Energy consumption 2023",
            Value = "900",
            Unit = dp2.Unit,
            Source = dp2.Source,
            InformationType = dp2.InformationType,
            OwnerId = dp2.OwnerId,
            CompletenessStatus = "complete",
            UpdatedBy = "user1"
        });
        
        // Rollover to Period 3 (2024)
        var rollover2 = store.RolloverPeriod(new RolloverRequest
        {
            SourcePeriodId = rollover1.Result!.TargetPeriod!.Id,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true
            }
        });
        
        Assert.True(rollover2.Success);
        
        var section3 = store.GetSections(rollover2.Result!.TargetPeriod!.Id).First();
        var dp3 = store.GetDataPoints(section3.Id).First();
        
        // Update value in period 3
        store.UpdateDataPoint(dp3.Id, new UpdateDataPointRequest
        {
            Title = dp3.Title,
            Content = "Energy consumption 2024",
            Value = "1000",
            Unit = dp3.Unit,
            Source = dp3.Source,
            InformationType = dp3.InformationType,
            OwnerId = dp3.OwnerId,
            CompletenessStatus = "complete",
            UpdatedBy = "user1"
        });
        
        // Act - Get cross-period lineage for the most recent data point
        var lineage = store.GetCrossPeriodLineage(dp3.Id);
        
        // Assert
        Assert.NotNull(lineage);
        Assert.Equal(dp3.Id, lineage.DataPointId);
        Assert.Equal("Total Energy", lineage.Title);
        
        // Check current version
        Assert.Equal(dp3.Id, lineage.CurrentVersion.DataPointId);
        Assert.Equal("FY2024", lineage.CurrentVersion.PeriodName);
        Assert.Equal("1000", lineage.CurrentVersion.Value);
        Assert.Equal("Energy consumption 2024", lineage.CurrentVersion.Content);
        Assert.True(lineage.CurrentVersion.IsRolledOver);
        
        // Check previous versions (should have 2: FY2023 and FY2022)
        Assert.Equal(2, lineage.PreviousVersions.Count);
        
        // First previous version (FY2023)
        var prevVersion1 = lineage.PreviousVersions[0];
        Assert.Equal(dp2.Id, prevVersion1.DataPointId);
        Assert.Equal("FY2023", prevVersion1.PeriodName);
        Assert.Equal("900", prevVersion1.Value);
        Assert.True(prevVersion1.IsRolledOver);
        
        // Second previous version (FY2022)
        var prevVersion2 = lineage.PreviousVersions[1];
        Assert.Equal(dp1!.Id, prevVersion2.DataPointId);
        Assert.Equal("FY2022", prevVersion2.PeriodName);
        Assert.Equal("800", prevVersion2.Value);
        Assert.False(prevVersion2.IsRolledOver); // Original, not rolled over
        
        // Check total periods
        Assert.Equal(3, lineage.TotalPeriods);
        Assert.False(lineage.HasMoreHistory);
    }
    
    [Fact]
    public void GetCrossPeriodLineage_NonExistentDataPoint_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var lineage = store.GetCrossPeriodLineage("non-existent-id");
        
        // Assert
        Assert.Null(lineage);
    }
}
