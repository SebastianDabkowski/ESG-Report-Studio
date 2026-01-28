using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ConsistencyValidationTests
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

            // Create user
            var usersField = typeof(InMemoryReportStore).GetField("_users", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var users = usersField!.GetValue(store) as List<User>;
            users!.Add(new User
            {
                Id = "user-1",
                Name = "Test Owner",
                Email = "test@example.com",
                Role = "report-owner"
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

        private static string CreateTestSection(InMemoryReportStore store, string title = "Test Section")
        {
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = title,
                Category = "environmental",
                Description = "Test section for validation",
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
        public void RunConsistencyValidation_WithValidPeriod_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(periodId, result.PeriodId);
            Assert.Equal("FY 2024", result.PeriodName);
            Assert.NotEmpty(result.ValidatedAt);
            Assert.Equal("user-1", result.ValidatedBy);
        }

        [Fact]
        public void RunConsistencyValidation_WithInvalidPeriod_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            var request = new RunValidationRequest
            {
                PeriodId = "invalid-period-id",
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("failed", result.Status);
            Assert.False(result.CanPublish);
            Assert.Equal(1, result.ErrorCount);
            Assert.Single(result.Issues);
            Assert.Equal("period-not-found", result.Issues[0].RuleType);
        }

        [Fact]
        public void ValidateRequiredData_SectionWithoutDataPoints_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "required-data" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "missing-required-field" && i.Message.Contains("no data points"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
        }

        [Fact]
        public void ValidateRequiredData_SectionWithIncompleteDataPoints_ShouldReturnWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create incomplete data point
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Type = "metric",
                Content = "Test content",
                OwnerId = "user-1",
                CompletenessStatus = "incomplete",
                InformationType = "measured"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "required-data" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.WarningCount > 0);
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "missing-required-field" && i.Message.Contains("incomplete or missing"));
            Assert.NotNull(issue);
            Assert.Equal("warning", issue.Severity);
        }

        [Fact]
        public void ValidateRequiredData_DataPointsNeedingReview_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create data point
            var (success, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Data Point",
                Type = "narrative",
                Content = "Test content",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            Assert.True(success);
            Assert.NotNull(dataPoint);

            // Manually set the review status to "changes-requested" to simulate the condition
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            var targetDataPoint = dataPoints!.First(dp => dp.Id == dataPoint.Id);
            targetDataPoint.ReviewStatus = "changes-requested";

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "required-data" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0, $"Expected errors but got {result.ErrorCount}. Warnings: {result.WarningCount}");
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "contradictory-statement");
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Contains(dataPoint.Id, issue.AffectedDataPointIds);
        }

        [Fact]
        public void ValidateUnitNormalization_MetricWithoutUnit_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create metric data point without unit
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Metric",
                Type = "metric",
                Content = "Test content",
                Value = "100",
                Unit = null, // Missing unit
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "unit-normalization" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "missing-required-field" && i.FieldName == "Unit");
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
        }

        [Fact]
        public void ValidateUnitNormalization_InconsistentUnits_ShouldReturnWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create data points with same classification but different units
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Emission 1",
                Type = "metric",
                Classification = "co2-emissions",
                Content = "Test content",
                Value = "100",
                Unit = "kg",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Emission 2",
                Type = "metric",
                Classification = "co2-emissions",
                Content = "Test content",
                Value = "200",
                Unit = "tonnes", // Different unit
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "unit-normalization" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.WarningCount > 0);
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "invalid-unit" && i.Message.Contains("inconsistent units"));
            Assert.NotNull(issue);
            Assert.Equal("warning", issue.Severity);
        }

        [Fact]
        public void ValidatePeriodCoverage_DateOutsidePeriod_ShouldReturnWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create data point with date value within period first
            var (success, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Event Date",
                Type = "narrative",
                Content = "Test content",
                Value = "2024-06-01", // Within 2024 period
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            Assert.True(success);

            // Manually change the value to a date outside the period
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            var targetDataPoint = dataPoints!.First(dp => dp.Id == dataPoint!.Id);
            targetDataPoint.Value = "2023-06-01"; // Change to date outside 2024 period

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "period-coverage" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.WarningCount > 0, $"Expected warnings but got {result.WarningCount}. Total issues: {result.Issues.Count}");
            var issue = result.Issues.FirstOrDefault(i => i.RuleType == "period-coverage");
            Assert.NotNull(issue);
            Assert.Equal("warning", issue.Severity);
        }

        [Fact]
        public void ValidateMissingFields_EstimateWithoutRequiredFields_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create a valid estimate first
            var (success, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Estimate",
                Type = "estimate",
                Content = "Test content",
                Value = "100",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "estimate",
                EstimateType = "point",
                EstimateMethod = "Calculation method",
                ConfidenceLevel = "high"
            });

            Assert.True(success);
            Assert.NotNull(dataPoint);

            // Manually corrupt the data point to simulate missing fields (bypass validation)
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            var targetDataPoint = dataPoints!.First(dp => dp.Id == dataPoint.Id);
            targetDataPoint.EstimateMethod = null;
            targetDataPoint.ConfidenceLevel = null;

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "missing-fields" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount >= 2); // Should have errors for both missing fields
            var methodIssue = result.Issues.FirstOrDefault(i => i.FieldName == "EstimateMethod");
            var confidenceIssue = result.Issues.FirstOrDefault(i => i.FieldName == "ConfidenceLevel");
            Assert.NotNull(methodIssue);
            Assert.NotNull(confidenceIssue);
        }

        [Fact]
        public void ValidateConsistency_AllRulesPass_ShouldReturnPassedStatus()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create valid data point with evidence
            var (success, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Valid Data Point",
                Type = "metric",
                Content = "Test content",
                Value = "100",
                Unit = "kg",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured"
            });

            Assert.True(success);

            // Approve the data point to avoid review warnings
            store.ApproveDataPoint(dataPoint!.Id, new ApproveDataPointRequest
            {
                ReviewedBy = "user-1",
                ReviewComments = "Approved"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            // There may be warnings about missing evidence, but no errors
            Assert.True(result.ErrorCount == 0, $"Expected no errors but got {result.ErrorCount}. Issues: {string.Join(", ", result.Issues.Where(i => i.Severity == "error").Select(i => i.Message))}");
            Assert.True(result.CanPublish);
        }

        [Fact]
        public void ValidateConsistency_WithErrors_CannotPublish()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            var periodId = store.GetSnapshot().Periods.First().Id;

            // Create a valid estimate first
            var (success, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Invalid Estimate",
                Type = "estimate",
                Content = "Test content",
                Value = "100",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "estimate",
                EstimateType = "point",
                EstimateMethod = "Test method",
                ConfidenceLevel = "high"
            });

            Assert.True(success);

            // Manually corrupt the data point to make it invalid
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            var targetDataPoint = dataPoints!.First(dp => dp.Id == dataPoint!.Id);
            targetDataPoint.EstimateMethod = null;
            targetDataPoint.ConfidenceLevel = null;

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("failed", result.Status);
            Assert.False(result.CanPublish);
            Assert.True(result.ErrorCount > 0);
        }
    }
}
