using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class DataPointReviewTests
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
            
            // Create a test section manually using reflection
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section for data points",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void CreateDataPoint_DefaultsToReviewStatusDraft()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.NotNull(dataPoint);
            Assert.Equal("draft", dataPoint.ReviewStatus);
            Assert.Null(dataPoint.ReviewedBy);
            Assert.Null(dataPoint.ReviewedAt);
            Assert.Null(dataPoint.ReviewComments);
        }

        [Fact]
        public void CreateDataPoint_WithSpecifiedReviewStatus_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "ready-for-review"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.NotNull(dataPoint);
            Assert.Equal("ready-for-review", dataPoint.ReviewStatus);
        }

        [Fact]
        public void CreateDataPoint_WithInvalidReviewStatus_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "invalid-status"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("ReviewStatus must be one of", errorMessage);
        }

        [Fact]
        public void ApproveDataPoint_WhenReadyForReview_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point in ready-for-review status
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "ready-for-review"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var approveRequest = new ApproveDataPointRequest
            {
                ReviewedBy = "user-2",
                ReviewComments = "Data looks good, approved!"
            };

            // Act
            var (isValid, errorMessage, approvedDataPoint) = store.ApproveDataPoint(dataPoint.Id, approveRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(approvedDataPoint);
            Assert.Equal("approved", approvedDataPoint.ReviewStatus);
            Assert.Equal("user-2", approvedDataPoint.ReviewedBy);
            Assert.NotNull(approvedDataPoint.ReviewedAt);
            Assert.Equal("Data looks good, approved!", approvedDataPoint.ReviewComments);
        }

        [Fact]
        public void ApproveDataPoint_WhenNotReadyForReview_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point in draft status
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "draft"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var approveRequest = new ApproveDataPointRequest
            {
                ReviewedBy = "user-2"
            };

            // Act
            var (isValid, errorMessage, approvedDataPoint) = store.ApproveDataPoint(dataPoint.Id, approveRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("must be in 'ready-for-review' status", errorMessage);
        }

        [Fact]
        public void RequestChanges_WhenReadyForReview_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point in ready-for-review status
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "ready-for-review"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var requestChangesRequest = new RequestChangesRequest
            {
                ReviewedBy = "user-2",
                ReviewComments = "Please provide more detailed breakdown of energy sources."
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.RequestChanges(dataPoint.Id, requestChangesRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("changes-requested", updatedDataPoint.ReviewStatus);
            Assert.Equal("user-2", updatedDataPoint.ReviewedBy);
            Assert.NotNull(updatedDataPoint.ReviewedAt);
            Assert.Equal("Please provide more detailed breakdown of energy sources.", updatedDataPoint.ReviewComments);
        }

        [Fact]
        public void RequestChanges_WithoutComments_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "ready-for-review"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var requestChangesRequest = new RequestChangesRequest
            {
                ReviewedBy = "user-2",
                ReviewComments = ""
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.RequestChanges(dataPoint.Id, requestChangesRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Review comments are required", errorMessage);
        }

        [Fact]
        public void UpdateDataPoint_WhenApproved_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create and approve a data point
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "ready-for-review"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var approveRequest = new ApproveDataPointRequest
            {
                ReviewedBy = "user-2"
            };
            store.ApproveDataPoint(dataPoint.Id, approveRequest);

            // Try to update the approved data point
            var updateRequest = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Updated Energy Consumption",
                Content = "Updated content",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Cannot modify approved data points", errorMessage);
        }

        [Fact]
        public void UpdateDataPoint_ChangeReviewStatus_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                ReviewStatus = "draft"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var updateRequest = new UpdateDataPointRequest
            {
                Type = dataPoint.Type,
                Title = dataPoint.Title,
                Content = dataPoint.Content,
                OwnerId = dataPoint.OwnerId,
                Source = dataPoint.Source,
                InformationType = dataPoint.InformationType,
                ReviewStatus = "ready-for-review"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("ready-for-review", updatedDataPoint.ReviewStatus);
        }
    }
}
