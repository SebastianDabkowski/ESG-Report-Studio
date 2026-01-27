using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ReportingPeriodUpdateTests
    {
        private static void CreateTestOrganizationalUnit(InMemoryReportStore store)
        {
            // Create a default organizational unit to satisfy the validation requirement
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });
        }

        private static string CreateTestPeriod(InMemoryReportStore store)
        {
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            
            var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(request);
            Assert.True(isValid);
            Assert.NotNull(snapshot);
            
            return snapshot.Periods[0].Id;
        }

        [Fact]
        public void UpdatePeriod_BeforeReportingStarts_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            var updateRequest = new UpdateReportingPeriodRequest
            {
                Name = "FY 2024 Updated",
                StartDate = "2024-02-01",
                EndDate = "2025-01-31",
                ReportingMode = "extended",
                ReportScope = "group"
            };

            // Act
            var (isValid, errorMessage, period) = store.ValidateAndUpdatePeriod(periodId, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(period);
            Assert.Equal("FY 2024 Updated", period.Name);
            Assert.Equal("2024-02-01", period.StartDate);
            Assert.Equal("2025-01-31", period.EndDate);
            Assert.Equal("extended", period.ReportingMode);
            Assert.Equal("group", period.ReportScope);
        }

        [Fact]
        public void UpdatePeriod_BeforeDataPoints_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Verify reporting has not started (no data points)
            var hasStarted = store.HasReportingStarted(periodId);
            Assert.False(hasStarted);
            
            var updateRequest = new UpdateReportingPeriodRequest
            {
                Name = "FY 2024 Updated",
                StartDate = "2024-02-01",
                EndDate = "2025-01-31",
                ReportingMode = "extended",
                ReportScope = "group"
            };

            // Act
            var (isValid, errorMessage, period) = store.ValidateAndUpdatePeriod(periodId, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(period);
            Assert.Equal("FY 2024 Updated", period.Name);
        }

        [Fact]
        public void UpdatePeriod_WithInvalidDates_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            var updateRequest = new UpdateReportingPeriodRequest
            {
                Name = "FY 2024 Updated",
                StartDate = "2024-12-31",
                EndDate = "2024-01-01", // End before start
                ReportingMode = "simplified",
                ReportScope = "single-company"
            };

            // Act
            var (isValid, errorMessage, period) = store.ValidateAndUpdatePeriod(periodId, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Start date must be before end date", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void UpdatePeriod_WithOverlappingDates_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            
            // Create two non-overlapping periods
            var firstRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (_, _, snapshot1) = store.ValidateAndCreatePeriod(firstRequest);
            Assert.NotNull(snapshot1);
            var period1Id = snapshot1.Periods[0].Id;
            
            var secondRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            store.ValidateAndCreatePeriod(secondRequest);

            // Try to update first period to overlap with second
            var updateRequest = new UpdateReportingPeriodRequest
            {
                Name = "FY 2024 Extended",
                StartDate = "2024-01-01",
                EndDate = "2025-06-30", // Overlaps with FY 2025
                ReportingMode = "simplified",
                ReportScope = "single-company"
            };

            // Act
            var (isValid, errorMessage, period) = store.ValidateAndUpdatePeriod(period1Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("overlaps", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void UpdatePeriod_WithNonexistentPeriod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var updateRequest = new UpdateReportingPeriodRequest
            {
                Name = "FY 2024 Updated",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company"
            };

            // Act
            var (isValid, errorMessage, period) = store.ValidateAndUpdatePeriod("nonexistent-id", updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void HasReportingStarted_WithNoDataPoints_ShouldReturnFalse()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);

            // Act
            var hasStarted = store.HasReportingStarted(periodId);

            // Assert
            Assert.False(hasStarted);
        }
    }
}
