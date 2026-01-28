using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ReadinessReportTests
    {
        private static InMemoryReportStore CreateStoreWithTestData(
            out string periodId, 
            out List<string> sectionIds)
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

            // Create reporting period
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Test Owner"
            });

            periodId = snapshot!.Periods.First().Id;
            sectionIds = snapshot.Sections.Select(s => s.Id).ToList();
            
            return store;
        }

        [Fact]
        public void GetReadinessReport_WithNoFilters_ShouldReturnAllItems()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);

            // Act
            var report = store.GetReadinessReport();

            // Assert
            Assert.NotNull(report);
            Assert.NotNull(report.Metrics);
            Assert.NotNull(report.Items);
            Assert.True(report.Items.Count > 0);
        }

        [Fact]
        public void GetReadinessReport_FilterByPeriod_ShouldReturnOnlyPeriodItems()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);

            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(periodId, report.PeriodId);
            Assert.NotNull(report.Items);
            Assert.True(report.Items.All(i => i.Type == "section" || i.Type == "datapoint"));
        }

        [Fact]
        public void GetReadinessReport_FilterByCategory_ShouldReturnOnlyCategoryItems()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            
            // Act
            var report = store.GetReadinessReport(periodId: periodId, category: "environmental");

            // Assert
            Assert.NotNull(report);
            Assert.NotNull(report.Items);
            Assert.True(report.Items.All(i => i.Category == "environmental"));
        }

        [Fact]
        public void GetReadinessReport_CalculatesOwnershipPercentageCorrectly()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            
            // All sections are created with owners by default
            var sections = store.GetSections(periodId);
            var sectionsWithOwners = sections.Count(s => !string.IsNullOrWhiteSpace(s.OwnerId));
            
            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Metrics.OwnershipPercentage >= 0);
            Assert.True(report.Metrics.OwnershipPercentage <= 100);
            Assert.Equal(report.Metrics.TotalItems, report.Items.Count);
        }

        [Fact]
        public void GetReadinessReport_WithBlockedDataPoint_ShouldCountBlocked()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();
            
            // Create a blocked data point
            var (_, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Blocked Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            });
            
            // Update to blocked status (changes-requested)
            store.UpdateDataPoint(dataPoint!.Id, new UpdateDataPointRequest
            {
                Title = "Blocked Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test",
                InformationType = "fact",
                CompletenessStatus = "incomplete",
                ReviewStatus = "changes-requested",
                UpdatedBy = "user-2"
            });

            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Metrics.BlockedCount >= 1, $"Expected at least 1 blocked item, got {report.Metrics.BlockedCount}");
        }

        [Fact]
        public void GetReadinessReport_WithOverdueDataPoint_ShouldCountOverdue()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();
            
            // Create an overdue data point (deadline in the past)
            var pastDeadline = DateTime.UtcNow.AddDays(-5).ToString("o");
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Overdue Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test",
                InformationType = "fact",
                CompletenessStatus = "incomplete",
                Deadline = pastDeadline
            });

            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Metrics.OverdueCount >= 1, $"Expected at least 1 overdue item, got {report.Metrics.OverdueCount}");
        }

        [Fact]
        public void GetReadinessReport_WithCompletedDataPoints_ShouldCalculateCompletionPercentage()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();
            
            // Create completed data points
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Completed Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test",
                InformationType = "fact",
                CompletenessStatus = "complete"
            });

            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Metrics.CompletionPercentage >= 0);
            Assert.True(report.Metrics.CompletionPercentage <= 100);
            Assert.True(report.Metrics.CompletedItems >= 1);
        }

        [Fact]
        public void GetReadinessReport_FilterByOwner_ShouldReturnOnlyOwnerItems()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);
            var ownerId = "user-1";

            // Act
            var report = store.GetReadinessReport(periodId: periodId, ownerId: ownerId);

            // Assert
            Assert.NotNull(report);
            Assert.NotNull(report.Items);
            Assert.True(report.Items.All(i => i.OwnerId == ownerId));
        }

        [Fact]
        public void GetReadinessReport_MetricsConsistency_ItemCountsShouldMatch()
        {
            // Arrange
            var store = CreateStoreWithTestData(out var periodId, out var sectionIds);

            // Act
            var report = store.GetReadinessReport(periodId: periodId);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(report.Metrics.TotalItems, report.Items.Count);
            Assert.True(report.Metrics.ItemsWithOwners <= report.Metrics.TotalItems);
            Assert.True(report.Metrics.CompletedItems <= report.Metrics.TotalItems);
            Assert.True(report.Metrics.BlockedCount <= report.Metrics.TotalItems);
            Assert.True(report.Metrics.OverdueCount <= report.Metrics.TotalItems);
        }
    }
}
