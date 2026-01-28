using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class DataPointValidationTests
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
                OwnerId = "owner-1",
                OwnerName = "Test Owner"
            });
        }

        private static string CreateTestSection(InMemoryReportStore store)
        {
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
            // Create a test section manually
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section for data points",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            // Add section directly (assuming there's a method to do this)
            // For now, we'll use reflection or add a helper method
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void CreateDataPoint_WithAllRequiredFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Classification = "fact",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
            Assert.Equal("Energy Consumption", dataPoint.Title);
            Assert.Equal("Energy Management System", dataPoint.Source);
            Assert.Equal("fact", dataPoint.InformationType);
            Assert.Equal("complete", dataPoint.CompletenessStatus);
            Assert.NotEmpty(dataPoint.Id);
            Assert.NotEmpty(dataPoint.CreatedAt);
            Assert.NotEmpty(dataPoint.UpdatedAt);
        }

        [Fact]
        public void CreateDataPoint_WithoutTitle_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Title is required.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithoutSource_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Source is required.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithoutInformationType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("InformationType is required.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithoutCompletenessStatus_ShouldAutoCalculate()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = ""
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert - Should succeed and auto-calculate status as "incomplete" (no evidence)
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
            Assert.Equal("incomplete", dataPoint.CompletenessStatus);
        }

        [Fact]
        public void CreateDataPoint_WithInvalidCompletenessStatus_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "partial" // Invalid - not one of the allowed values
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("CompletenessStatus must be one of", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void UpdateDataPoint_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Original Title",
                Content = "Original content",
                OwnerId = "owner-1",
                Source = "Original Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);
            var originalUpdatedAt = createdDataPoint!.UpdatedAt;

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Updated Title",
                Content = "Updated content",
                Source = "Updated Source",
                InformationType = "estimate",
                Assumptions = "These are test assumptions for the estimate",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("Updated Title", updatedDataPoint.Title);
            Assert.Equal("Updated Source", updatedDataPoint.Source);
            Assert.Equal("estimate", updatedDataPoint.InformationType);
            Assert.Equal("These are test assumptions for the estimate", updatedDataPoint.Assumptions);
            Assert.Equal("complete", updatedDataPoint.CompletenessStatus);
            Assert.NotEqual(originalUpdatedAt, updatedDataPoint.UpdatedAt);
        }

        [Fact]
        public void UpdateDataPoint_WithoutRequiredMetadata_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Original Title",
                Content = "Original content",
                OwnerId = "owner-1",
                Source = "Original Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Updated Title",
                Content = "Updated content",
                Source = "",
                InformationType = "estimate",
                Assumptions = "Test assumptions",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Source is required.", errorMessage);
            Assert.Null(updatedDataPoint);
        }

        [Fact]
        public void DeleteDataPoint_WithValidId_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            // Act
            var deleted = store.DeleteDataPoint(createdDataPoint!.Id);

            // Assert
            Assert.True(deleted);
            var retrieved = store.GetDataPoint(createdDataPoint.Id);
            Assert.Null(retrieved);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateAndAssumptions_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions for remote work",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on average employee commute distance and home energy usage patterns",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
            Assert.Equal("estimate", dataPoint.InformationType);
            Assert.Equal("Based on average employee commute distance and home energy usage patterns", dataPoint.Assumptions);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithoutAssumptions_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions for remote work",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Assumptions field is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void UpdateDataPoint_ToEstimateWithoutAssumptions_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Original Title",
                Content = "Original content",
                OwnerId = "owner-1",
                Source = "Original Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Updated Title",
                Content = "Updated content",
                Source = "Updated Source",
                InformationType = "estimate",
                Assumptions = "",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Assumptions field is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(updatedDataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithInvalidInformationType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Content = "Some content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "measured", // Old value, should be rejected
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("InformationType must be one of: fact, estimate, declaration, plan", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateAndWhitespaceAssumptions_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions for remote work",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "   ", // Whitespace only
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Assumptions field is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(dataPoint);
        }
    }
}
