using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    /// <summary>
    /// Tests for report generation validation checks.
    /// Covers validation of evidence, provenance metadata, and reporting mode compliance.
    /// </summary>
    public class ReportGenerationValidationTests
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
        }

        private static string CreateTestPeriod(InMemoryReportStore store, string reportingMode = "simplified")
        {
            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = reportingMode,
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Test Owner"
            });
            
            return store.GetSnapshot().Periods.First().Id;
        }

        private static string CreateTestSection(InMemoryReportStore store, string periodId, string status = "draft")
        {
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section for validation",
                OwnerId = "user-1",
                Status = status,
                Completeness = "empty",
                Order = 1,
                IsEnabled = true
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        #region Evidence and Provenance Validation Tests

        [Fact]
        public void ValidateReadySectionMetadata_SectionReadyWithoutEvidence_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create data point without evidence
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "CO2 Emissions",
                Type = "metric",
                Content = "Total CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e",
                Source = "Energy meter readings"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "ready-section-metadata" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "missing-required-field" && 
                i.Message.Contains("no supporting evidence"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Equal("EvidenceIds", issue.FieldName);
        }

        [Fact]
        public void ValidateReadySectionMetadata_SectionReadyWithoutProvenanceSource_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create evidence first
            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId: sectionId,
                title: "Test Evidence",
                description: "Test evidence document",
                fileName: null,
                fileUrl: null,
                sourceUrl: "https://example.com/evidence.pdf",
                uploadedBy: "user-1");
            Assert.True(isValid);

            // Create data point with evidence but no source
            var dpRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "CO2 Emissions",
                Type = "metric",
                Content = "Total CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e"
            };
            var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dpRequest);
            Assert.True(dpValid);

            // Link evidence
            store.LinkEvidenceToDataPoint(dataPoint!.Id, evidence!.Id, "user-1");

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "ready-section-metadata" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "missing-required-field" && 
                i.Message.Contains("no source information"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Equal("Source", issue.FieldName);
        }

        [Fact]
        public void ValidateReadySectionMetadata_CalculatedDataPointWithoutLineage_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create evidence first
            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId: sectionId,
                title: "Test Evidence",
                description: "Test evidence document",
                fileName: null,
                fileUrl: null,
                sourceUrl: "https://example.com/evidence.pdf",
                uploadedBy: "user-1");
            Assert.True(isValid);

            // Create calculated data point without calculation formula
            var dpRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Total Emissions",
                Type = "metric",
                Content = "Calculated total emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e",
                IsCalculated = true,
                Source = "Calculated from scope 1 and 2"
            };
            var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dpRequest);
            Assert.True(dpValid);

            // Link evidence
            store.LinkEvidenceToDataPoint(dataPoint!.Id, evidence!.Id, "user-1");

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "ready-section-metadata" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "missing-required-field" && 
                i.Message.Contains("no calculation formula"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Equal("CalculationFormula", issue.FieldName);
        }

        [Fact]
        public void ValidateReadySectionMetadata_EstimateDataPoint_ShouldNotRequireEvidence()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create estimate data point without evidence
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Estimated Emissions",
                Type = "metric",
                Content = "Estimated CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "estimate",
                EstimateType = "proxy-based",
                EstimateMethod = "Using industry averages",
                ConfidenceLevel = "medium",
                Value = "500",
                Unit = "tCO2e",
                Source = "Industry benchmark data"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "ready-section-metadata" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            var evidenceIssues = result.Issues.Where(i => 
                i.FieldName == "EvidenceIds" && 
                i.Message.Contains("no supporting evidence")).ToList();
            Assert.Empty(evidenceIssues);
        }

        [Fact]
        public void ValidateReadySectionMetadata_NotApplicableDataPoint_ShouldSkipValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create not-applicable data point
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "N/A Data Point",
                Type = "metric",
                Content = "Not applicable",
                OwnerId = "user-1",
                CompletenessStatus = "not-applicable",
                InformationType = "measured"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "ready-section-metadata" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ErrorCount);
        }

        #endregion

        #region Reporting Mode Compliance Tests

        [Fact]
        public void ValidateReportingModeCompliance_CSRDModeWithoutCatalogCode_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "csrd");
            var sectionId = CreateTestSection(store, periodId);

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "reporting-mode-compliance" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "reporting-mode-compliance" && 
                i.Message.Contains("must have a CatalogCode"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Equal("CatalogCode", issue.FieldName);
        }

        [Fact]
        public void ValidateReportingModeCompliance_ExtendedModeWithoutClassification_ShouldReturnError()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "extended");
            
            // Create section with catalog code
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1,
                IsEnabled = true,
                CatalogCode = "E1-1"
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);

            // Create data point without classification
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section.Id,
                Title = "CO2 Emissions",
                Type = "metric",
                Content = "Total CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "reporting-mode-compliance" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "reporting-mode-compliance" && 
                i.Message.Contains("must have a Classification"));
            Assert.NotNull(issue);
            Assert.Equal("error", issue.Severity);
            Assert.Equal("Classification", issue.FieldName);
        }

        [Fact]
        public void ValidateReportingModeCompliance_CSRDModeWithLowConfidenceEstimate_ShouldReturnWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "csrd");
            
            // Create section with catalog code
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1,
                IsEnabled = true,
                CatalogCode = "E1-1"
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);

            // Create estimate with low confidence
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = section.Id,
                Title = "Estimated Emissions",
                Type = "metric",
                Content = "Estimated CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "estimate",
                EstimateType = "proxy-based",
                EstimateMethod = "Rough estimate",
                ConfidenceLevel = "low",
                Classification = "emissions-scope1",
                Value = "500",
                Unit = "tCO2e"
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "reporting-mode-compliance" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.WarningCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "reporting-mode-compliance" && 
                i.Message.Contains("low confidence"));
            Assert.NotNull(issue);
            Assert.Equal("warning", issue.Severity);
        }

        [Fact]
        public void ValidateReportingModeCompliance_SimplifiedModeWithCatalogCodes_ShouldReturnInfo()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "simplified");
            
            // Create section with catalog code (not required in simplified mode)
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1,
                IsEnabled = true,
                CatalogCode = "E1-1"
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1",
                RuleTypes = new List<string> { "reporting-mode-compliance" }
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InfoCount > 0);
            var issue = result.Issues.FirstOrDefault(i => 
                i.RuleType == "reporting-mode-compliance" && 
                i.Message.Contains("catalog codes, which are not required for simplified"));
            Assert.NotNull(issue);
            Assert.Equal("info", issue.Severity);
        }

        #endregion

        #region Validation History Tests

        [Fact]
        public void RunConsistencyValidation_ShouldStoreResultForAudit()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);
            var history = store.GetValidationHistory(periodId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(history);
            Assert.Single(history);
            Assert.Equal(result.PeriodId, history[0].PeriodId);
            Assert.Equal(result.ValidatedBy, history[0].ValidatedBy);
            Assert.Equal(result.ValidatedAt, history[0].ValidatedAt);
        }

        [Fact]
        public void GetValidationHistory_MultipleRuns_ShouldReturnInDescendingOrder()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act - Run validation multiple times
            var result1 = store.RunConsistencyValidation(request);
            Thread.Sleep(10); // Ensure different timestamps
            var result2 = store.RunConsistencyValidation(request);
            Thread.Sleep(10);
            var result3 = store.RunConsistencyValidation(request);

            var history = store.GetValidationHistory(periodId);

            // Assert
            Assert.NotNull(history);
            Assert.Equal(3, history.Count);
            // Most recent should be first
            Assert.Equal(result3.ValidatedAt, history[0].ValidatedAt);
            Assert.Equal(result2.ValidatedAt, history[1].ValidatedAt);
            Assert.Equal(result1.ValidatedAt, history[2].ValidatedAt);
        }

        [Fact]
        public void GetLatestValidationResult_WithValidations_ShouldReturnMostRecent()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            store.RunConsistencyValidation(request);
            Thread.Sleep(10);
            var latestResult = store.RunConsistencyValidation(request);

            var retrieved = store.GetLatestValidationResult(periodId);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(latestResult.ValidatedAt, retrieved.ValidatedAt);
        }

        [Fact]
        public void GetLatestValidationResult_NoValidations_ShouldReturnNull()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store);

            // Act
            var result = store.GetLatestValidationResult(periodId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ValidateConsistency_AllRules_ShouldRunAllValidations()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "csrd");
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create data point with multiple issues
            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "CO2 Emissions",
                Type = "metric",
                Content = "Total CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e"
                // Missing: ProvenanceSource, EvidenceIds, Classification (required for CSRD)
            });

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
                // No RuleTypes specified - should run all rules
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ErrorCount > 0);
            Assert.False(result.CanPublish);
            
            // Should have issues from multiple rule types
            var ruleTypes = result.Issues.Select(i => i.RuleType).Distinct().ToList();
            Assert.Contains("missing-required-field", ruleTypes);
            Assert.Contains("reporting-mode-compliance", ruleTypes);
        }

        [Fact]
        public void ValidateConsistency_CanPublish_WhenAllRequirementsMet()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var periodId = CreateTestPeriod(store, "simplified");
            var sectionId = CreateTestSection(store, periodId, "ready-for-review");

            // Create evidence
            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId: sectionId,
                title: "Test Evidence",
                description: "Test evidence document",
                fileName: null,
                fileUrl: null,
                sourceUrl: "https://example.com/evidence.pdf",
                uploadedBy: "user-1");
            Assert.True(isValid);

            // Create complete data point
            var dpRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "CO2 Emissions",
                Type = "metric",
                Content = "Total CO2 emissions",
                OwnerId = "user-1",
                CompletenessStatus = "complete",
                InformationType = "measured",
                Value = "1000",
                Unit = "tCO2e",
                Source = "Energy meter readings"
            };
            var (dpValid, dpError, dataPoint) = store.CreateDataPoint(dpRequest);
            Assert.True(dpValid);

            // Link evidence
            store.LinkEvidenceToDataPoint(dataPoint!.Id, evidence!.Id, "user-1");

            var request = new RunValidationRequest
            {
                PeriodId = periodId,
                ValidatedBy = "user-1"
            };

            // Act
            var result = store.RunConsistencyValidation(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ErrorCount);
            Assert.True(result.CanPublish);
            Assert.Equal("passed", result.Status);
        }

        #endregion
    }
}
