using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for year-over-year metric comparison functionality.
/// Verifies that numeric metrics can be compared across reporting periods
/// with proper handling of unit compatibility and missing data.
/// </summary>
public class MetricComparisonTests
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

    private static string CreateTestPeriod(InMemoryReportStore store, string name, string startDate, string endDate)
    {
        var request = new CreateReportingPeriodRequest
        {
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
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
    public void CompareMetrics_WithValidNumericData_ShouldCalculatePercentageChange()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        var period2023 = CreateTestPeriod(store, "FY2023", "2023-01-01", "2023-12-31");
        
        // Get automatically created section
        var sections2023 = store.GetSections(period2023);
        var section2023 = sections2023.First();
        
        // Create data point in 2023
        var dataPointRequest2023 = new CreateDataPointRequest
        {
            SectionId = section2023.Id,
            Title = "Total Energy Consumption",
            Content = "Annual energy consumption",
            Value = "1000",
            Unit = "MWh",
            OwnerId = "user-1",
            Source = "Energy meters",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        var (isValid2023, _, dataPoint2023) = store.CreateDataPoint(dataPointRequest2023);
        Assert.True(isValid2023);
        Assert.NotNull(dataPoint2023);
        
        // Rollover to 2024
        var rolloverRequest = new RolloverRequest
        {
            SourcePeriodId = period2023,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user-1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true,
                CopyDisclosures = false,
                CopyAttachments = false
            }
        };
        
        var (success, errorMessage, rolloverResult) = store.RolloverPeriod(rolloverRequest);
        Assert.True(success, errorMessage);
        Assert.NotNull(rolloverResult);
        
        // Get the rolled-over data point in 2024
        var period2024 = rolloverResult.TargetPeriod;
        Assert.NotNull(period2024);
        
        var sections2024 = store.GetSections(period2024.Id);
        var dataPoints2024 = store.GetDataPoints(sections2024.First().Id, null);
        var dataPoint2024 = dataPoints2024.First(dp => dp.Title == "Total Energy Consumption");
        
        // Update 2024 value to 1200
        var updateRequest = new UpdateDataPointRequest
        {
            Title = dataPoint2024.Title,
            Content = dataPoint2024.Content,
            Type = dataPoint2024.Type,
            Classification = dataPoint2024.Classification,
            Value = "1200",
            Unit = "MWh",
            OwnerId = dataPoint2024.OwnerId,
            Source = dataPoint2024.Source,
            InformationType = dataPoint2024.InformationType,
            CompletenessStatus = string.IsNullOrWhiteSpace(dataPoint2024.CompletenessStatus) ? "complete" : dataPoint2024.CompletenessStatus,
            UpdatedBy = "user-1"
        };
        var (isValidUpdate, errorMessageUpdate, updatedDataPoint) = store.UpdateDataPoint(dataPoint2024.Id, updateRequest);
        Assert.True(isValidUpdate, $"Update failed: {errorMessageUpdate}");
        Assert.NotNull(updatedDataPoint);
        
        // Act - Compare metrics
        var comparison = store.CompareMetrics(dataPoint2024.Id, null);
        
        // Assert
        Assert.NotNull(comparison);
        Assert.Equal(dataPoint2024.Id, comparison.DataPointId);
        Assert.Equal("Total Energy Consumption", comparison.Title);
        
        // Current period checks
        Assert.NotNull(comparison.CurrentPeriod);
        Assert.Equal("1200", comparison.CurrentPeriod.Value);
        Assert.Equal(1200m, comparison.CurrentPeriod.NumericValue);
        Assert.Equal("MWh", comparison.CurrentPeriod.Unit);
        
        // Prior period checks
        Assert.NotNull(comparison.PriorPeriod);
        Assert.Equal("1000", comparison.PriorPeriod.Value);
        Assert.Equal(1000m, comparison.PriorPeriod.NumericValue);
        Assert.Equal("MWh", comparison.PriorPeriod.Unit);
        
        // Comparison calculations
        Assert.True(comparison.IsComparisonAvailable);
        Assert.Null(comparison.UnavailableReason);
        Assert.True(comparison.UnitsCompatible);
        Assert.Null(comparison.UnitWarning);
        
        Assert.NotNull(comparison.AbsoluteChange);
        Assert.Equal(200m, comparison.AbsoluteChange.Value);
        
        Assert.NotNull(comparison.PercentageChange);
        Assert.Equal(20m, comparison.PercentageChange.Value); // (1200-1000)/1000 * 100 = 20%
        
        // Available baselines
        Assert.Single(comparison.AvailableBaselines);
        Assert.Equal("Previous Year", comparison.AvailableBaselines[0].Label);
        Assert.True(comparison.AvailableBaselines[0].HasData);
    }
    
    [Fact]
    public void CompareMetrics_WithUnitMismatch_ShouldMarkAsUnavailable()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        var period2023 = CreateTestPeriod(store, "FY2023", "2023-01-01", "2023-12-31");
        var sections2023 = store.GetSections(period2023);
        var section2023 = sections2023.First();
        
        // Create data point in 2023 with unit "kWh"
        var dataPointRequest2023 = new CreateDataPointRequest
        {
            SectionId = section2023.Id,
            Title = "Total Energy Consumption",
            Content = "Annual energy consumption",
            Value = "1000000",
            Unit = "kWh",
            OwnerId = "user-1",
            Source = "Energy meters",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        var (isValid2023, _, dataPoint2023) = store.CreateDataPoint(dataPointRequest2023);
        Assert.True(isValid2023);
        
        // Rollover to 2024
        var rolloverRequest = new RolloverRequest
        {
            SourcePeriodId = period2023,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user-1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true,
                CopyDisclosures = false,
                CopyAttachments = false
            }
        };
        
        var (success, errorMessage, rolloverResult) = store.RolloverPeriod(rolloverRequest);
        Assert.True(success, errorMessage);
        var sections2024 = store.GetSections(rolloverResult!.TargetPeriod!.Id);
        var dataPoints2024 = store.GetDataPoints(sections2024.First().Id, null);
        var dataPoint2024 = dataPoints2024.First();
        
        // Update 2024 value with different unit "MWh"
        var updateRequest = new UpdateDataPointRequest
        {
            Title = dataPoint2024.Title,
            Content = dataPoint2024.Content,
            Type = dataPoint2024.Type,
            Classification = dataPoint2024.Classification,
            Value = "1200",
            Unit = "MWh",
            OwnerId = dataPoint2024.OwnerId,
            Source = dataPoint2024.Source,
            InformationType = dataPoint2024.InformationType,
            CompletenessStatus = string.IsNullOrWhiteSpace(dataPoint2024.CompletenessStatus) ? "complete" : dataPoint2024.CompletenessStatus,
            UpdatedBy = "user-1"
        };
        store.UpdateDataPoint(dataPoint2024.Id, updateRequest);
        
        // Act - Compare metrics
        var comparison = store.CompareMetrics(dataPoint2024.Id, null);
        
        // Assert
        Assert.NotNull(comparison);
        Assert.False(comparison.IsComparisonAvailable);
        Assert.Equal("Unit mismatch", comparison.UnavailableReason);
        Assert.False(comparison.UnitsCompatible);
        Assert.NotNull(comparison.UnitWarning);
        Assert.Contains("MWh", comparison.UnitWarning);
        Assert.Contains("kWh", comparison.UnitWarning);
        Assert.Null(comparison.PercentageChange);
        Assert.Null(comparison.AbsoluteChange);
    }
    
    [Fact]
    public void CompareMetrics_WithMissingPriorData_ShouldIndicateUnavailable()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        var period2024 = CreateTestPeriod(store, "FY2024", "2024-01-01", "2024-12-31");
        var sections2024 = store.GetSections(period2024);
        var section2024 = sections2024.First();
        
        // Create data point only in 2024 (no prior period)
        var dataPointRequest = new CreateDataPointRequest
        {
            SectionId = section2024.Id,
            Title = "New Metric",
            Content = "Newly tracked metric",
            Value = "500",
            Unit = "tons",
            OwnerId = "user-1",
            Source = "Internal tracking",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        var (isValid, _, dataPoint) = store.CreateDataPoint(dataPointRequest);
        Assert.True(isValid);
        Assert.NotNull(dataPoint);
        
        // Act - Compare metrics
        var comparison = store.CompareMetrics(dataPoint.Id, null);
        
        // Assert
        Assert.NotNull(comparison);
        Assert.False(comparison.IsComparisonAvailable);
        Assert.Equal("No prior period data available", comparison.UnavailableReason);
        Assert.Null(comparison.PriorPeriod);
        Assert.Null(comparison.PercentageChange);
        Assert.Null(comparison.AbsoluteChange);
        Assert.Empty(comparison.AvailableBaselines);
    }
    
    [Fact]
    public void CompareMetrics_WithNonNumericValues_ShouldIndicateUnavailable()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        var period2023 = CreateTestPeriod(store, "FY2023", "2023-01-01", "2023-12-31");
        var sections2023 = store.GetSections(period2023);
        var section2023 = sections2023.First();
        
        // Create data point with non-numeric value
        var dataPointRequest2023 = new CreateDataPointRequest
        {
            SectionId = section2023.Id,
            Title = "Sustainability Rating",
            Content = "Company sustainability rating",
            Value = "Grade A",
            Unit = "Rating",
            OwnerId = "user-1",
            Source = "External audit",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        var (isValid2023, _, dataPoint2023) = store.CreateDataPoint(dataPointRequest2023);
        Assert.True(isValid2023);
        
        // Rollover to 2024
        var rolloverRequest = new RolloverRequest
        {
            SourcePeriodId = period2023,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user-1",
            Options = new RolloverOptions
            {
                CopyStructure = true,
                CopyDataValues = true,
                CopyDisclosures = false,
                CopyAttachments = false
            }
        };
        
        var (success, errorMessage, rolloverResult) = store.RolloverPeriod(rolloverRequest);
        Assert.True(success, errorMessage);
        var sections2024 = store.GetSections(rolloverResult!.TargetPeriod!.Id);
        var dataPoints2024 = store.GetDataPoints(sections2024.First().Id, null);
        var dataPoint2024 = dataPoints2024.First();
        
        // Act - Compare metrics
        var comparison = store.CompareMetrics(dataPoint2024.Id, null);
        
        // Assert
        Assert.NotNull(comparison);
        Assert.False(comparison.IsComparisonAvailable);
        Assert.Equal("Non-numeric values", comparison.UnavailableReason);
        Assert.Null(comparison.PercentageChange);
        Assert.Null(comparison.AbsoluteChange);
    }
    
    [Fact]
    public void CompareMetrics_WithMultipleBaselines_ShouldListAllAvailablePeriods()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestOrganization(store);
        CreateTestOrganizationalUnit(store);
        
        // Create 3 years of data
        var period2022 = CreateTestPeriod(store, "FY2022", "2022-01-01", "2022-12-31");
        var sections2022 = store.GetSections(period2022);
        var section2022 = sections2022.First();
        
        var dataPointRequest2022 = new CreateDataPointRequest
        {
            SectionId = section2022.Id,
            Title = "Total Energy",
            Content = "Energy consumption",
            Value = "800",
            Unit = "MWh",
            OwnerId = "user-1",
            Source = "Meters",
            InformationType = "fact",
            CompletenessStatus = "complete",
            Type = "metric"
        };
        store.CreateDataPoint(dataPointRequest2022);
        
        // Rollover to 2023
        var rollover2023 = new RolloverRequest
        {
            SourcePeriodId = period2022,
            TargetPeriodName = "FY2023",
            TargetPeriodStartDate = "2023-01-01",
            TargetPeriodEndDate = "2023-12-31",
            PerformedBy = "user-1",
            Options = new RolloverOptions { CopyStructure = true, CopyDataValues = true, CopyDisclosures = false, CopyAttachments = false }
        };
        var (success2023, errorMessage2023, result2023) = store.RolloverPeriod(rollover2023);
        Assert.True(success2023, errorMessage2023);
        var sections2023 = store.GetSections(result2023!.TargetPeriod!.Id);
        var dataPoints2023 = store.GetDataPoints(sections2023.First().Id, null);
        var dataPoint2023 = dataPoints2023.First();
        
        // Update 2023 value
        var update2023 = new UpdateDataPointRequest
        {
            Title = dataPoint2023.Title,
            Content = dataPoint2023.Content,
            Type = dataPoint2023.Type,
            Classification = dataPoint2023.Classification,
            Value = "1000",
            Unit = "MWh",
            OwnerId = dataPoint2023.OwnerId,
            Source = dataPoint2023.Source,
            InformationType = dataPoint2023.InformationType,
            CompletenessStatus = string.IsNullOrWhiteSpace(dataPoint2023.CompletenessStatus) ? "complete" : dataPoint2023.CompletenessStatus,
            UpdatedBy = "user-1"
        };
        store.UpdateDataPoint(dataPoint2023.Id, update2023);
        
        // Rollover to 2024
        var rollover2024 = new RolloverRequest
        {
            SourcePeriodId = result2023.TargetPeriod.Id,
            TargetPeriodName = "FY2024",
            TargetPeriodStartDate = "2024-01-01",
            TargetPeriodEndDate = "2024-12-31",
            PerformedBy = "user-1",
            Options = new RolloverOptions { CopyStructure = true, CopyDataValues = true, CopyDisclosures = false, CopyAttachments = false }
        };
        var (success2024, errorMessage2024, result2024) = store.RolloverPeriod(rollover2024);
        Assert.True(success2024, errorMessage2024);
        var sections2024 = store.GetSections(result2024!.TargetPeriod!.Id);
        var dataPoints2024 = store.GetDataPoints(sections2024.First().Id, null);
        var dataPoint2024 = dataPoints2024.First();
        
        // Act - Compare metrics
        var comparison = store.CompareMetrics(dataPoint2024.Id, null);
        
        // Assert
        Assert.NotNull(comparison);
        Assert.Equal(2, comparison.AvailableBaselines.Count);
        
        // Check first baseline (most recent = 2023)
        Assert.Equal("Previous Year", comparison.AvailableBaselines[0].Label);
        Assert.True(comparison.AvailableBaselines[0].HasData);
        Assert.Equal("FY2023", comparison.AvailableBaselines[0].PeriodName);
        
        // Check second baseline (2 years back = 2022)
        Assert.Equal("2 Years Back", comparison.AvailableBaselines[1].Label);
        Assert.True(comparison.AvailableBaselines[1].HasData);
        Assert.Equal("FY2022", comparison.AvailableBaselines[1].PeriodName);
    }
}
