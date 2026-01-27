using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ReportingPeriodValidationTests
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

        [Fact]
        public void CreatePeriod_WithValidDates_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Single(snapshot.Periods);
        }

        [Fact]
        public void CreatePeriod_WithStartDateAfterEndDate_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "Invalid Period",
                StartDate = "2024-12-31",
                EndDate = "2024-01-01",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Start date must be before end date", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithStartDateEqualToEndDate_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "Same Day Period",
                StartDate = "2024-06-15",
                EndDate = "2024-06-15",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Start date must be before end date", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithInvalidDateFormat_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "Invalid Format Period",
                StartDate = "invalid-date",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Invalid date format", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithOverlappingDates_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            
            // Create first period
            var firstRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, _) = store.ValidateAndCreatePeriod(firstRequest);
            Assert.True(isValid1);

            // Try to create overlapping period
            var overlappingRequest = new CreateReportingPeriodRequest
            {
                Name = "Q4 2024",
                StartDate = "2024-10-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(overlappingRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("overlaps with existing period", errorMessage);
            Assert.Contains("FY 2024", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithCompletelyContainedPeriod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            
            // Create first period
            var firstRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            store.ValidateAndCreatePeriod(firstRequest);

            // Try to create a period completely contained in the existing one
            var containedRequest = new CreateReportingPeriodRequest
            {
                Name = "Q2 2024",
                StartDate = "2024-04-01",
                EndDate = "2024-06-30",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(containedRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("overlaps", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithNonOverlappingDates_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            
            // Create first period
            var firstRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            store.ValidateAndCreatePeriod(firstRequest);

            // Create a non-overlapping period (next year)
            var nextRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(nextRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Equal(2, snapshot.Periods.Count);
        }

        [Fact]
        public void CreatePeriod_WithAdjacentDates_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            
            // Create first period
            var firstRequest = new CreateReportingPeriodRequest
            {
                Name = "H1 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-06-30",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            store.ValidateAndCreatePeriod(firstRequest);

            // Create adjacent period (starts right after the first one)
            var adjacentRequest = new CreateReportingPeriodRequest
            {
                Name = "H2 2024",
                StartDate = "2024-07-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(adjacentRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Equal(2, snapshot.Periods.Count);
        }

        [Fact]
        public void CreatePeriod_WithReportScope_ShouldSetCorrectScope()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "group",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Single(snapshot.Periods);
            Assert.Equal("group", snapshot.Periods[0].ReportScope);
        }

        [Fact]
        public void CreatePeriod_WithoutReportScope_ShouldDefaultToSingleCompany()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganizationalUnit(store);
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Single(snapshot.Periods);
            Assert.Equal("single-company", snapshot.Periods[0].ReportScope);
        }
    }
}
