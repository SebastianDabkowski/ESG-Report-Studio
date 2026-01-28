using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class EstimateValidationTests
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
        public void CreateDataPoint_WithEstimateAndAllRequiredFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions for remote work",
                Value = "1250",
                Unit = "tCO2e",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on average employee commute distance and home energy usage patterns",
                EstimateType = "extrapolated",
                EstimateMethod = "Employee survey data extrapolated to full workforce using regional averages",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
            Assert.Equal("estimate", dataPoint.InformationType);
            Assert.Equal("extrapolated", dataPoint.EstimateType);
            Assert.Equal("Employee survey data extrapolated to full workforce using regional averages", dataPoint.EstimateMethod);
            Assert.Equal("medium", dataPoint.ConfidenceLevel);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithoutEstimateType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateMethod = "Survey data",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("EstimateType is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithInvalidEstimateType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateType = "calculated", // Invalid type
                EstimateMethod = "Survey data",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("EstimateType must be one of: point, range, proxy-based, extrapolated", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithoutEstimateMethod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateType = "point",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("EstimateMethod is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithoutConfidenceLevel_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateType = "point",
                EstimateMethod = "Survey data",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("ConfidenceLevel is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateWithInvalidConfidenceLevel_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Content = "Estimated GHG emissions",
                OwnerId = "owner-1",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateType = "point",
                EstimateMethod = "Survey data",
                ConfidenceLevel = "very-high", // Invalid level
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("ConfidenceLevel must be one of: low, medium, high", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void UpdateDataPoint_ToEstimateWithAllFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Energy Consumption",
                Content = "Actual measured consumption",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Estimated Energy Consumption",
                Content = "Estimated energy consumption for remote offices",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on floor space and occupancy",
                EstimateType = "proxy-based",
                EstimateMethod = "Floor space multiplied by average kWh per sqm for similar buildings",
                ConfidenceLevel = "low",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("estimate", updatedDataPoint.InformationType);
            Assert.Equal("proxy-based", updatedDataPoint.EstimateType);
            Assert.Equal("Floor space multiplied by average kWh per sqm for similar buildings", updatedDataPoint.EstimateMethod);
            Assert.Equal("low", updatedDataPoint.ConfidenceLevel);
        }

        [Fact]
        public void UpdateDataPoint_ToEstimateWithoutEstimateType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Energy Consumption",
                Content = "Actual measured consumption",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Estimated Energy Consumption",
                Content = "Estimated energy consumption",
                Source = "Calculation Model",
                InformationType = "estimate",
                Assumptions = "Based on averages",
                EstimateMethod = "Survey data",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.Equal("EstimateType is required when InformationType is 'estimate'.", errorMessage);
            Assert.Null(updatedDataPoint);
        }

        [Fact]
        public void CreateDataPoint_WithEstimateAllTypes_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var estimateTypes = new[] { "point", "range", "proxy-based", "extrapolated" };

            foreach (var estimateType in estimateTypes)
            {
                var request = new CreateDataPointRequest
                {
                    SectionId = sectionId,
                    Title = $"Test {estimateType}",
                    Content = $"Testing {estimateType} estimate",
                    OwnerId = "owner-1",
                    Source = "Test Source",
                    InformationType = "estimate",
                    Assumptions = "Test assumptions",
                    EstimateType = estimateType,
                    EstimateMethod = "Test method",
                    ConfidenceLevel = "high",
                    CompletenessStatus = "complete"
                };

                // Act
                var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

                // Assert
                Assert.True(isValid, $"Failed for estimate type: {estimateType}");
                Assert.Null(errorMessage);
                Assert.NotNull(dataPoint);
                Assert.Equal(estimateType, dataPoint.EstimateType);
            }
        }

        [Fact]
        public void CreateDataPoint_WithEstimateAllConfidenceLevels_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var confidenceLevels = new[] { "low", "medium", "high" };

            foreach (var confidenceLevel in confidenceLevels)
            {
                var request = new CreateDataPointRequest
                {
                    SectionId = sectionId,
                    Title = $"Test {confidenceLevel}",
                    Content = $"Testing {confidenceLevel} confidence",
                    OwnerId = "owner-1",
                    Source = "Test Source",
                    InformationType = "estimate",
                    Assumptions = "Test assumptions",
                    EstimateType = "point",
                    EstimateMethod = "Test method",
                    ConfidenceLevel = confidenceLevel,
                    CompletenessStatus = "complete"
                };

                // Act
                var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

                // Assert
                Assert.True(isValid, $"Failed for confidence level: {confidenceLevel}");
                Assert.Null(errorMessage);
                Assert.NotNull(dataPoint);
                Assert.Equal(confidenceLevel, dataPoint.ConfidenceLevel);
            }
        }

        [Fact]
        public void UpdateDataPoint_EstimateVersioning_ShouldTrackChanges()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Water Usage",
                Content = "Estimated water consumption",
                OwnerId = "owner-1",
                Source = "Initial Model",
                InformationType = "estimate",
                Assumptions = "Initial assumptions",
                EstimateType = "point",
                EstimateMethod = "Initial method",
                ConfidenceLevel = "low",
                CompletenessStatus = "complete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            // Act - Update estimate
            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Estimated Water Usage",
                Content = "Updated estimated water consumption",
                Source = "Improved Model",
                InformationType = "estimate",
                Assumptions = "Updated assumptions with more data",
                EstimateType = "range",
                EstimateMethod = "Improved method with actual measurements from similar sites",
                ConfidenceLevel = "medium",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1",
                ChangeNote = "Updated estimate with better methodology"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);

            // Verify audit log contains changes
            var auditLog = store.GetAuditLog(entityId: createdDataPoint.Id);
            Assert.NotEmpty(auditLog);
            
            var latestEntry = auditLog.First();
            Assert.Equal("update", latestEntry.Action);
            Assert.Equal("DataPoint", latestEntry.EntityType);
            Assert.Equal(createdDataPoint.Id, latestEntry.EntityId);
            
            // Verify specific field changes were tracked
            Assert.Contains(latestEntry.Changes, c => c.Field == "EstimateType" && c.OldValue == "point" && c.NewValue == "range");
            Assert.Contains(latestEntry.Changes, c => c.Field == "EstimateMethod");
            Assert.Contains(latestEntry.Changes, c => c.Field == "ConfidenceLevel" && c.OldValue == "low" && c.NewValue == "medium");
        }
    }
}
