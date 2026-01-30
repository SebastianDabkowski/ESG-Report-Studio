using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products
{
    public class VarianceExplanationTests
    {
        private static InMemoryReportStore CreateStoreWithTestData()
        {
            var textDiffService = new TextDiffService();
            var store = new InMemoryReportStore(textDiffService);

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

            // Create reporting periods
            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2023",
                StartDate = "2023-01-01",
                EndDate = "2023-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner"
            });

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

            return store;
        }

        [Fact]
        public void CreateVarianceThresholdConfig_WithValidRequest_ShouldCreateConfig()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First(p => p.Name == "FY 2024").Id;

            var request = new CreateVarianceThresholdConfigRequest
            {
                PercentageThreshold = 10,
                AbsoluteThreshold = 1000,
                RequireBothThresholds = false,
                RequireReviewerApproval = true,
                CreatedBy = "test-user"
            };

            // Act
            var (isValid, errorMessage, config) = store.CreateVarianceThresholdConfig(periodId, request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(config);
            Assert.Equal(10, config.PercentageThreshold);
            Assert.Equal(1000, config.AbsoluteThreshold);
            Assert.False(config.RequireBothThresholds);
            Assert.True(config.RequireReviewerApproval);
        }

        [Fact]
        public void CreateVarianceThresholdConfig_WithoutAnyThreshold_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First(p => p.Name == "FY 2024").Id;

            var request = new CreateVarianceThresholdConfigRequest
            {
                RequireBothThresholds = false,
                RequireReviewerApproval = true,
                CreatedBy = "test-user"
            };

            // Act
            var (isValid, errorMessage, config) = store.CreateVarianceThresholdConfig(periodId, request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("At least one threshold", errorMessage);
            Assert.Null(config);
        }

        [Fact]
        public void CreateVarianceExplanation_WithValidRequest_ShouldCreateExplanation()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var period2023 = snapshot.Periods.First(p => p.Name == "FY 2023");
            var period2024 = snapshot.Periods.First(p => p.Name == "FY 2024");

            // Create sections
            var section2023 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2023.Id,
                Title = "Emissions",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var section2024 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2024.Id,
                Title = "Emissions",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            // Add sections via reflection (simulating data already in the system)
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section2023);
            sections!.Add(section2024);

            // Create data points
            var dataPoint2023Request = new CreateDataPointRequest
            {
                SectionId = section2023.Id,
                Type = "metric",
                Title = "CO2 Emissions",
                Value = "1000",
                Unit = "tonnes",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            };
            var (_, _, dataPoint2023) = store.CreateDataPoint(dataPoint2023Request);

            var dataPoint2024Request = new CreateDataPointRequest
            {
                SectionId = section2024.Id,
                Type = "metric",
                Title = "CO2 Emissions",
                Value = "1250",
                Unit = "tonnes",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            };
            var (_, _, dataPoint2024) = store.CreateDataPoint(dataPoint2024Request);
            
            // Set the source data point ID via property
            dataPoint2024!.SourceDataPointId = dataPoint2023!.Id;

            var explanationRequest = new CreateVarianceExplanationRequest
            {
                DataPointId = dataPoint2024.Id,
                PriorPeriodId = period2023.Id,
                Explanation = "Increased production capacity led to higher emissions",
                RootCause = "Business expansion",
                Category = "business-expansion",
                CreatedBy = "test-user"
            };

            // Act
            var (isValid, errorMessage, explanation) = store.CreateVarianceExplanation(explanationRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(explanation);
            Assert.Equal(dataPoint2024.Id, explanation.DataPointId);
            Assert.Equal(period2023.Id, explanation.PriorPeriodId);
            Assert.Equal("Increased production capacity led to higher emissions", explanation.Explanation);
            Assert.Equal("Business expansion", explanation.RootCause);
            Assert.Equal("business-expansion", explanation.Category);
            Assert.Equal("draft", explanation.Status);
            Assert.True(explanation.IsFlagged);
        }

        [Fact]
        public void UpdateVarianceExplanation_InDraftStatus_ShouldUpdate()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var period2023 = snapshot.Periods.First(p => p.Name == "FY 2023");
            var period2024 = snapshot.Periods.First(p => p.Name == "FY 2024");

            // Create minimal setup with reflection
            var section2023 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2023.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var section2024 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2024.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var sectionsField = typeof(InMemoryReportStore).GetField("_sections",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section2023);
            sections!.Add(section2024);

            var (_, _, dataPoint2023) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2023.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "100",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });

            var (_, _, dataPoint2024) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2024.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "150",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });
            
            // Set the source data point ID via property
            dataPoint2024!.SourceDataPointId = dataPoint2023!.Id;

            var (_, _, explanation) = store.CreateVarianceExplanation(new CreateVarianceExplanationRequest
            {
                DataPointId = dataPoint2024.Id,
                PriorPeriodId = period2023.Id,
                Explanation = "Original explanation",
                CreatedBy = "test-user"
            });

            var updateRequest = new UpdateVarianceExplanationRequest
            {
                Explanation = "Updated explanation with more details",
                RootCause = "New root cause",
                Category = "operational-change",
                UpdatedBy = "test-user"
            };

            // Act
            var (isValid, errorMessage, updated) = store.UpdateVarianceExplanation(explanation!.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updated);
            Assert.Equal("Updated explanation with more details", updated.Explanation);
            Assert.Equal("New root cause", updated.RootCause);
            Assert.Equal("operational-change", updated.Category);
        }

        [Fact]
        public void SubmitVarianceExplanation_InDraftStatus_ShouldChangeToSubmitted()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var period2023 = snapshot.Periods.First(p => p.Name == "FY 2023");
            var period2024 = snapshot.Periods.First(p => p.Name == "FY 2024");

            // Create minimal setup
            var section2023 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2023.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var section2024 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2024.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var sectionsField = typeof(InMemoryReportStore).GetField("_sections",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section2023);
            sections!.Add(section2024);

            var (_, _, dataPoint2023) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2023.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "100",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });

            var (_, _, dataPoint2024) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2024.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "150",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });
            
            // Set the source data point ID via property
            dataPoint2024!.SourceDataPointId = dataPoint2023!.Id;

            var (_, _, explanation) = store.CreateVarianceExplanation(new CreateVarianceExplanationRequest
            {
                DataPointId = dataPoint2024.Id,
                PriorPeriodId = period2023.Id,
                Explanation = "Test explanation",
                CreatedBy = "test-user"
            });

            // Act
            var (isValid, errorMessage, submitted) = store.SubmitVarianceExplanation(explanation!.Id,
                new SubmitVarianceExplanationRequest { SubmittedBy = "test-user" });

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(submitted);
            Assert.Equal("submitted", submitted.Status);
        }

        [Fact]
        public void ReviewVarianceExplanation_ApproveSubmitted_ShouldApproveAndClearFlag()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var period2023 = snapshot.Periods.First(p => p.Name == "FY 2023");
            var period2024 = snapshot.Periods.First(p => p.Name == "FY 2024");

            // Create minimal setup
            var section2023 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2023.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var section2024 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = period2024.Id,
                Title = "Test",
                Category = "environmental",
                Description = "Test",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };

            var sectionsField = typeof(InMemoryReportStore).GetField("_sections",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section2023);
            sections!.Add(section2024);

            var (_, _, dataPoint2023) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2023.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "100",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });

            var (_, _, dataPoint2024) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section2024.Id,
                Type = "metric",
                Title = "Test Metric",
                Value = "150",
                Unit = "units",
                OwnerId = "owner-1",
                Source = "Internal",
                InformationType = "fact"
            });
            
            // Set the source data point ID via property
            dataPoint2024!.SourceDataPointId = dataPoint2023!.Id;

            var (_, _, explanation) = store.CreateVarianceExplanation(new CreateVarianceExplanationRequest
            {
                DataPointId = dataPoint2024.Id,
                PriorPeriodId = period2023.Id,
                Explanation = "Test explanation",
                CreatedBy = "test-user"
            });

            store.SubmitVarianceExplanation(explanation!.Id,
                new SubmitVarianceExplanationRequest { SubmittedBy = "test-user" });

            // Act
            var (isValid, errorMessage, reviewed) = store.ReviewVarianceExplanation(explanation.Id,
                new ReviewVarianceExplanationRequest
                {
                    Decision = "approve",
                    Comments = "Looks good",
                    ReviewedBy = "reviewer-1"
                });

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(reviewed);
            Assert.Equal("approved", reviewed.Status);
            Assert.False(reviewed.IsFlagged);
            Assert.Equal("reviewer-1", reviewed.ReviewedBy);
            Assert.Equal("Looks good", reviewed.ReviewComments);
        }
    }
}
