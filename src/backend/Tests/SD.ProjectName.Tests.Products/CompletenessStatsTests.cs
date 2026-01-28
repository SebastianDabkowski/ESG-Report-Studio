using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class CompletenessStatsTests
    {
        private static void CreateTestConfiguration(InMemoryReportStore store, out string periodId, out List<string> sectionIds)
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
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner"
            });

            periodId = snapshot!.Periods.First().Id;
            sectionIds = snapshot.Sections.Select(s => s.Id).ToList();
        }

        private static void CreateTestDataPoint(
            InMemoryReportStore store, 
            string sectionId, 
            string completenessStatus,
            string ownerId = "owner-1")
        {
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Classification = "fact",
                Title = $"Data Point - {completenessStatus}",
                Content = "Test content",
                OwnerId = ownerId,
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = completenessStatus
            });
        }

        [Fact]
        public void GetCompletenessStats_WithNoDataPoints_ShouldReturnZeroStats()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.NotNull(stats);
            Assert.NotNull(stats.Overall);
            Assert.Equal(0, stats.Overall.TotalCount);
            Assert.Equal(0, stats.Overall.MissingCount);
            Assert.Equal(0, stats.Overall.IncompleteCount);
            Assert.Equal(0, stats.Overall.CompleteCount);
            Assert.Equal(0, stats.Overall.NotApplicableCount);
            Assert.Equal(0.0, stats.Overall.CompletePercentage);
        }

        [Fact]
        public void GetCompletenessStats_WithMixedStatusDataPoints_ShouldCalculateCorrectly()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            // Create data points with different statuses
            var envSectionId = sectionIds.First(id => 
            {
                var section = store.GetSections(periodId).First(s => s.Id == id);
                return section.Category == "environmental";
            });

            CreateTestDataPoint(store, envSectionId, "complete");
            CreateTestDataPoint(store, envSectionId, "complete");
            CreateTestDataPoint(store, envSectionId, "incomplete");
            CreateTestDataPoint(store, envSectionId, "missing");
            CreateTestDataPoint(store, envSectionId, "not applicable");

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(5, stats.Overall.TotalCount);
            Assert.Equal(1, stats.Overall.MissingCount);
            Assert.Equal(1, stats.Overall.IncompleteCount);
            Assert.Equal(2, stats.Overall.CompleteCount);
            Assert.Equal(1, stats.Overall.NotApplicableCount);
            Assert.Equal(40.0, stats.Overall.CompletePercentage); // 2 out of 5 = 40%
        }

        [Fact]
        public void GetCompletenessStats_ByCategory_ShouldGroupCorrectly()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            var sections = store.GetSections(periodId).ToList();
            var envSection = sections.First(s => s.Category == "environmental");
            var socialSection = sections.First(s => s.Category == "social");

            CreateTestDataPoint(store, envSection.Id, "complete");
            CreateTestDataPoint(store, envSection.Id, "complete");
            CreateTestDataPoint(store, socialSection.Id, "incomplete");
            CreateTestDataPoint(store, socialSection.Id, "missing");

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.NotNull(stats.ByCategory);
            Assert.Equal(3, stats.ByCategory.Count); // environmental, social, governance

            var envStats = stats.ByCategory.First(c => c.Id == "environmental");
            Assert.Equal("Environmental", envStats.Name);
            Assert.Equal(2, envStats.TotalCount);
            Assert.Equal(2, envStats.CompleteCount);
            Assert.Equal(100.0, envStats.CompletePercentage);

            var socialStats = stats.ByCategory.First(c => c.Id == "social");
            Assert.Equal("Social", socialStats.Name);
            Assert.Equal(2, socialStats.TotalCount);
            Assert.Equal(1, socialStats.IncompleteCount);
            Assert.Equal(1, socialStats.MissingCount);
            Assert.Equal(0, socialStats.CompleteCount);
            Assert.Equal(0.0, socialStats.CompletePercentage);
        }

        [Fact]
        public void GetCompletenessStats_FilterByCategory_ShouldReturnOnlyFilteredData()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            var sections = store.GetSections(periodId).ToList();
            var envSection = sections.First(s => s.Category == "environmental");
            var socialSection = sections.First(s => s.Category == "social");

            CreateTestDataPoint(store, envSection.Id, "complete");
            CreateTestDataPoint(store, envSection.Id, "complete");
            CreateTestDataPoint(store, socialSection.Id, "incomplete");

            // Act
            var stats = store.GetCompletenessStats(periodId, "environmental");

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.Overall.TotalCount);
            Assert.Equal(2, stats.Overall.CompleteCount);
            Assert.Equal(100.0, stats.Overall.CompletePercentage);
        }

        [Fact]
        public void GetCompletenessStats_ByOrganizationalUnit_ShouldGroupByOwner()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            var sections = store.GetSections(periodId).ToList();
            var section1 = sections.First();
            var section2 = sections.Skip(1).First();

            // Create data points for different sections (which may have different owners)
            CreateTestDataPoint(store, section1.Id, "complete", section1.OwnerId);
            CreateTestDataPoint(store, section1.Id, "complete", section1.OwnerId);
            CreateTestDataPoint(store, section2.Id, "incomplete", section2.OwnerId);

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.NotNull(stats.ByOrganizationalUnit);
            Assert.True(stats.ByOrganizationalUnit.Count > 0);
            
            // Check that totals add up
            var totalByUnit = stats.ByOrganizationalUnit.Sum(u => u.TotalCount);
            Assert.Equal(stats.Overall.TotalCount, totalByUnit);
        }

        [Fact]
        public void GetCompletenessStats_WithNoPeriod_ShouldReturnAllData()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            var section = store.GetSections(periodId).First();
            CreateTestDataPoint(store, section.Id, "complete");
            CreateTestDataPoint(store, section.Id, "incomplete");

            // Act - no period filter
            var stats = store.GetCompletenessStats();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.Overall.TotalCount);
        }

        [Fact]
        public void GetCompletenessStats_CompletePercentage_ShouldBeAccurate()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            var section = store.GetSections(periodId).First();
            
            // Create 3 complete, 1 incomplete = 75% complete
            CreateTestDataPoint(store, section.Id, "complete");
            CreateTestDataPoint(store, section.Id, "complete");
            CreateTestDataPoint(store, section.Id, "complete");
            CreateTestDataPoint(store, section.Id, "incomplete");

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.Equal(4, stats.Overall.TotalCount);
            Assert.Equal(3, stats.Overall.CompleteCount);
            Assert.Equal(75.0, stats.Overall.CompletePercentage);
        }

        [Fact]
        public void GetCompletenessStats_AllCategories_ShouldHaveCorrectNames()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            // Act
            var stats = store.GetCompletenessStats(periodId);

            // Assert
            Assert.NotNull(stats.ByCategory);
            Assert.Equal(3, stats.ByCategory.Count);
            
            var categoryNames = stats.ByCategory.Select(c => c.Name).OrderBy(n => n).ToList();
            Assert.Equal(new[] { "Environmental", "Governance", "Social" }, categoryNames);
        }
    }
}
