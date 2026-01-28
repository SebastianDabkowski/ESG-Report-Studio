using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class DataPointStatusUpdateTests
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
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithAllRequiredFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point with all required fields
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1",
                ChangeNote = "All data collected and verified"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(validationError);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("complete", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithoutValue_ShouldFailWithMissingFieldDetails()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point WITHOUT a value
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Energy Policy",
                Content = "Our energy management policy",
                OwnerId = "owner-1",
                Source = "Policy Document",
                InformationType = "declaration",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.Equal("Cannot mark data point as complete. Required fields are missing.", validationError.Message);
            Assert.NotEmpty(validationError.MissingFields);
            Assert.Contains(validationError.MissingFields, f => f.Field == "Value");
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithoutPeriod_ShouldFailWithMissingFieldDetails()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point WITHOUT a deadline/period
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact"
                // No Deadline
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.Contains(validationError.MissingFields, f => f.Field == "Period");
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithoutSource_ShouldFailWithMissingFieldDetails()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point - but we'll manipulate source to be empty
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Temp Source",  // Will clear this
                InformationType = "fact",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Clear the source using reflection to simulate missing data
            dataPoint.Source = "";

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.Contains(validationError.MissingFields, f => f.Field == "Source");
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithoutOwner_ShouldFailWithMissingFieldDetails()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Clear the owner using reflection
            dataPoint.OwnerId = "";

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.Contains(validationError.MissingFields, f => f.Field == "Owner");
        }

        [Fact]
        public void UpdateDataPointStatus_ToComplete_WithMultipleMissingFields_ShouldListAll()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point missing multiple required fields
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Policy Statement",
                Content = "Our policy",
                OwnerId = "owner-1",
                Source = "Policy",
                InformationType = "declaration",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Clear owner and source
            dataPoint.OwnerId = "";
            dataPoint.Source = "";

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "user-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.True(validationError.MissingFields.Count >= 2);
            Assert.Contains(validationError.MissingFields, f => f.Field == "Value");
            Assert.Contains(validationError.MissingFields, f => f.Field == "Source");
            Assert.Contains(validationError.MissingFields, f => f.Field == "Owner");
        }

        [Fact]
        public void UpdateDataPointStatus_ToIncomplete_ShouldSucceedWithoutValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point that's currently complete
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                Deadline = "2024-12-31",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);
            Assert.Equal("complete", dataPoint.CompletenessStatus);

            // Act - change back to incomplete (no validation should block this)
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "incomplete",
                UpdatedBy = "owner-1",
                ChangeNote = "Found data quality issue, needs review"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(validationError);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("incomplete", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void UpdateDataPointStatus_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                Deadline = "2024-12-31",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "complete",
                UpdatedBy = "owner-1",
                ChangeNote = "All verification complete"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            
            // Get audit log
            var auditLog = store.GetAuditLog();
            var statusUpdateEntry = auditLog.FirstOrDefault(e => 
                e.Action == "update-status" && 
                e.EntityId == dataPoint.Id);

            Assert.NotNull(statusUpdateEntry);
            Assert.Equal("owner-1", statusUpdateEntry.UserId);
            Assert.Equal("Test Owner", statusUpdateEntry.UserName);
            Assert.Equal("All verification complete", statusUpdateEntry.ChangeNote);
            Assert.Contains(statusUpdateEntry.Changes, c => 
                c.Field == "CompletenessStatus" && 
                c.OldValue == "incomplete" && 
                c.NewValue == "complete");
        }

        [Fact]
        public void UpdateDataPointStatus_WithInvalidStatus_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test",
                Content = "Test content",
                Value = "100",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact",
                Deadline = "2024-12-31"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var updateRequest = new UpdateDataPointStatusRequest
            {
                CompletenessStatus = "invalid-status",
                UpdatedBy = "owner-1"
            };

            var (isValid, validationError, updatedDataPoint) = store.UpdateDataPointStatus(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(validationError);
            Assert.Contains("must be one of", validationError.Message);
        }
    }
}
