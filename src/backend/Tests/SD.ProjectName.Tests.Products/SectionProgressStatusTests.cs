using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    /// <summary>
    /// Tests for section progress status calculation based on data point statuses.
    /// </summary>
    public class SectionProgressStatusTests
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
                Name = "Test Organizational Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });

            // Create reporting period
            var (periodValid, periodError, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner"
            });

            if (!periodValid || snapshot == null)
            {
                throw new InvalidOperationException($"Failed to create period: {periodError}");
            }

            periodId = snapshot.Periods.First().Id;
            sectionIds = snapshot.Sections.Select(s => s.Id).ToList();
        }

        private static void CreateTestDataPoint(
            InMemoryReportStore store, 
            string sectionId, 
            string completenessStatus,
            string reviewStatus = "draft",
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
                CompletenessStatus = completenessStatus,
                ReviewStatus = reviewStatus
            });
        }

        [Fact]
        public void SectionProgressStatus_WithNoDataPoints_ShouldBeNotStarted()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var firstSection = summaries.First();

            // Assert
            Assert.Equal("not-started", firstSection.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithAllMissingDataPoints_ShouldBeNotStarted()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "missing");
            CreateTestDataPoint(store, sectionId, "missing");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("not-started", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithBlockedDataPoint_ShouldBeBlocked()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete", "approved");
            CreateTestDataPoint(store, sectionId, "incomplete", "changes-requested"); // Blocked

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("blocked", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithAllCompleteDataPoints_ShouldBeCompleted()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "complete");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("completed", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithCompleteAndNotApplicable_ShouldBeCompleted()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "not applicable");
            CreateTestDataPoint(store, sectionId, "complete");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("completed", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithMixedStatuses_ShouldBeInProgress()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "incomplete");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("in-progress", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_WithSomeCompleteAndSomeMissing_ShouldBeInProgress()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "missing");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("in-progress", section.ProgressStatus);
        }

        [Fact]
        public void SectionProgressStatus_BlockedTakesPrecedenceOverComplete()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "complete", "approved");
            CreateTestDataPoint(store, sectionId, "complete", "approved");
            CreateTestDataPoint(store, sectionId, "complete", "changes-requested"); // One blocked

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal("blocked", section.ProgressStatus);
        }

        [Fact]
        public void CompletenessPercentage_ExcludesNotApplicableFromDenominator()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            // 2 complete, 2 not applicable = should be 100% (2 complete out of 2 relevant)
            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "not applicable");
            CreateTestDataPoint(store, sectionId, "not applicable");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal(100, section.CompletenessPercentage);
        }

        [Fact]
        public void CompletenessPercentage_WithAllNotApplicable_ShouldBe100Percent()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            CreateTestDataPoint(store, sectionId, "not applicable");
            CreateTestDataPoint(store, sectionId, "not applicable");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal(100, section.CompletenessPercentage);
            // Note: Even though percentage is 100%, progress status should be "completed" 
            // since all data points are handled (even if not applicable)
            Assert.Equal("completed", section.ProgressStatus);
        }

        [Fact]
        public void CompletenessPercentage_CalculatesCorrectly()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store, out var periodId, out var sectionIds);
            var sectionId = sectionIds.First();

            // 2 complete out of 4 relevant = 50%
            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "complete");
            CreateTestDataPoint(store, sectionId, "incomplete");
            CreateTestDataPoint(store, sectionId, "missing");

            // Act
            var summaries = store.GetSectionSummaries(periodId);
            var section = summaries.First(s => s.Id == sectionId);

            // Assert
            Assert.Equal(50, section.CompletenessPercentage);
            Assert.Equal("in-progress", section.ProgressStatus);
        }
    }
}
