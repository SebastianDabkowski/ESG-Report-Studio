using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class CompletenessComparisonTests
    {
        private static (InMemoryReportStore store, string period1Id, string period2Id) CreateTestPeriods()
        {
            var store = new InMemoryReportStore();

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

            // Create first period (prior)
            var (_, _, snapshot1) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2023",
                StartDate = "2023-01-01",
                EndDate = "2023-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner 1"
            });

            var period1Id = snapshot1!.Periods.First().Id;

            // Create second period (current)
            var (_, _, snapshot2) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner 1"
            });

            var period2Id = snapshot2!.Periods.First().Id;

            return (store, period1Id, period2Id);
        }

        [Fact]
        public void CompareCompletenessStats_ValidPeriods_ShouldReturnComparison()
        {
            // Arrange
            var (store, priorPeriodId, currentPeriodId) = CreateTestPeriods();

            // Act
            var comparison = store.CompareCompletenessStats(currentPeriodId, priorPeriodId);

            // Assert
            Assert.NotNull(comparison);
            Assert.Equal(currentPeriodId, comparison.CurrentPeriod.Id);
            Assert.Equal(priorPeriodId, comparison.PriorPeriod.Id);
            Assert.NotNull(comparison.Overall);
            Assert.NotNull(comparison.ByCategory);
            Assert.NotNull(comparison.BySection);
            Assert.NotNull(comparison.Summary);
        }

        [Fact]
        public void CompareCompletenessStats_InvalidCurrentPeriod_ShouldThrowException()
        {
            // Arrange
            var (store, priorPeriodId, _) = CreateTestPeriods();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                store.CompareCompletenessStats("invalid-id", priorPeriodId));
        }

        [Fact]
        public void CompareCompletenessStats_InvalidPriorPeriod_ShouldThrowException()
        {
            // Arrange
            var (store, _, currentPeriodId) = CreateTestPeriods();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                store.CompareCompletenessStats(currentPeriodId, "invalid-id"));
        }

        [Fact]
        public void CompareCompletenessStats_ByCategory_ShouldIncludeAllCategories()
        {
            // Arrange
            var (store, priorPeriodId, currentPeriodId) = CreateTestPeriods();

            // Act
            var comparison = store.CompareCompletenessStats(currentPeriodId, priorPeriodId);

            // Assert
            Assert.NotNull(comparison.ByCategory);
            Assert.Equal(3, comparison.ByCategory.Count);
            
            var categories = comparison.ByCategory.Select(c => c.Id).OrderBy(c => c).ToList();
            Assert.Equal(new[] { "environmental", "governance", "social" }, categories);
        }

        [Fact]
        public void CompareCompletenessStats_SummaryStatistics_ShouldBeAccurate()
        {
            // Arrange
            var (store, priorPeriodId, currentPeriodId) = CreateTestPeriods();

            // Act
            var comparison = store.CompareCompletenessStats(currentPeriodId, priorPeriodId);

            // Assert
            Assert.NotNull(comparison.Summary);
            
            // Verify counts add up correctly
            var totalSections = comparison.Summary.RegressionCount + 
                               comparison.Summary.ImprovementCount + 
                               comparison.Summary.UnchangedCount +
                               comparison.Summary.AddedSectionCount +
                               comparison.Summary.RemovedSectionCount;
            
            Assert.Equal(comparison.BySection.Count, totalSections);
        }

        [Fact]
        public void CompareCompletenessStats_OwnerInformation_ShouldBeIncluded()
        {
            // Arrange
            var (store, priorPeriodId, currentPeriodId) = CreateTestPeriods();

            // Act
            var comparison = store.CompareCompletenessStats(currentPeriodId, priorPeriodId);

            // Assert
            var sectionsWithOwners = comparison.BySection
                .Where(s => s.ExistsInBothPeriods)
                .ToList();
            
            Assert.All(sectionsWithOwners, s =>
            {
                Assert.NotNull(s.OwnerId);
                Assert.NotNull(s.OwnerName);
            });
        }

        [Fact]
        public void CompareCompletenessStats_ByOrganizationalUnit_ShouldCompareCorrectly()
        {
            // Arrange
            var (store, priorPeriodId, currentPeriodId) = CreateTestPeriods();

            // Act
            var comparison = store.CompareCompletenessStats(currentPeriodId, priorPeriodId);

            // Assert
            Assert.NotNull(comparison.ByOrganizationalUnit);
            // Note: May be empty if no data points exist yet
        }
    }
}
