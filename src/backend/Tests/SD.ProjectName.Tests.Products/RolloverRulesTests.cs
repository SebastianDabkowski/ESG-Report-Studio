using ARP.ESG_ReportStudio.API.Reporting;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    public class RolloverRulesTests
    {
        private static void CreateTestOrganization(InMemoryReportStore store)
        {
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
        }

        private static void CreateTestOrganizationalUnit(InMemoryReportStore store)
        {
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });
        }

        private static string CreateTestPeriod(InMemoryReportStore store, string name)
        {
            var request = new CreateReportingPeriodRequest
            {
                Name = name,
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user1",
                OwnerName = "Test User",
                OrganizationId = "org1"
            };

            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);
            Assert.True(isValid, errorMessage);
            Assert.NotNull(snapshot);
            
            var period = snapshot.Periods.First(p => p.Name == name);
            return period.Id;
        }

        [Fact]
        public void SaveRolloverRule_ShouldCreateNewRule()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new SaveDataTypeRolloverRuleRequest
            {
                DataType = "policy",
                RuleType = "copy",
                Description = "Policies should be copied to maintain consistency",
                SavedBy = "admin-user"
            };

            // Act
            var rule = store.SaveRolloverRule(request);

            // Assert
            Assert.NotNull(rule);
            Assert.NotEmpty(rule.Id);
            Assert.Equal("policy", rule.DataType);
            Assert.Equal(DataTypeRolloverRuleType.Copy, rule.RuleType);
            Assert.Equal("Policies should be copied to maintain consistency", rule.Description);
            Assert.Equal("admin-user", rule.CreatedBy);
            Assert.Equal(1, rule.Version);
        }

        [Fact]
        public void SaveRolloverRule_ShouldUpdateExistingRule()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create initial rule
            var createRequest = new SaveDataTypeRolloverRuleRequest
            {
                DataType = "metric",
                RuleType = "copy",
                Description = "Initial description",
                SavedBy = "admin-user"
            };
            var initialRule = store.SaveRolloverRule(createRequest);

            // Act - Update the rule
            var updateRequest = new SaveDataTypeRolloverRuleRequest
            {
                DataType = "metric",
                RuleType = "reset",
                Description = "Updated description - reset metrics for new period",
                SavedBy = "admin-user"
            };
            var updatedRule = store.SaveRolloverRule(updateRequest);

            // Assert
            Assert.Equal(initialRule.Id, updatedRule.Id); // Same ID
            Assert.Equal(DataTypeRolloverRuleType.Reset, updatedRule.RuleType);
            Assert.Equal("Updated description - reset metrics for new period", updatedRule.Description);
            Assert.Equal(2, updatedRule.Version); // Version incremented
            Assert.NotNull(updatedRule.UpdatedAt);
            Assert.Equal("admin-user", updatedRule.UpdatedBy);
        }

        [Fact]
        public void SaveRolloverRule_ShouldCreateHistoryEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new SaveDataTypeRolloverRuleRequest
            {
                DataType = "kpi",
                RuleType = "copy-as-draft",
                Description = "KPIs need review each period",
                SavedBy = "admin-user"
            };

            // Act
            store.SaveRolloverRule(request);
            var history = store.GetRolloverRuleHistory("kpi");

            // Assert
            Assert.Single(history);
            var entry = history.First();
            Assert.Equal("kpi", entry.DataType);
            Assert.Equal(DataTypeRolloverRuleType.CopyAsDraft, entry.RuleType);
            Assert.Equal("created", entry.ChangeType);
            Assert.Equal(1, entry.Version);
        }

        [Fact]
        public void GetRolloverRules_ShouldReturnAllRules()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "policy",
                RuleType = "copy",
                SavedBy = "admin-user"
            });
            
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "metric",
                RuleType = "reset",
                SavedBy = "admin-user"
            });

            // Act
            var rules = store.GetRolloverRules();

            // Assert
            Assert.Equal(2, rules.Count);
            Assert.Contains(rules, r => r.DataType == "policy");
            Assert.Contains(rules, r => r.DataType == "metric");
        }

        [Fact]
        public void GetRolloverRuleForDataType_ShouldReturnCorrectRule()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "target",
                RuleType = "copy-as-draft",
                Description = "Targets need annual review",
                SavedBy = "admin-user"
            });

            // Act
            var rule = store.GetRolloverRuleForDataType("target");

            // Assert
            Assert.NotNull(rule);
            Assert.Equal("target", rule.DataType);
            Assert.Equal(DataTypeRolloverRuleType.CopyAsDraft, rule.RuleType);
        }

        [Fact]
        public void DeleteRolloverRule_ShouldRemoveRule()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "narrative",
                RuleType = "copy",
                SavedBy = "admin-user"
            });

            // Act
            var deleted = store.DeleteRolloverRule("narrative", "admin-user");
            var rules = store.GetRolloverRules();

            // Assert
            Assert.True(deleted);
            Assert.Empty(rules);
        }

        [Fact]
        public void DeleteRolloverRule_ShouldCreateHistoryEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "evidence",
                RuleType = "copy",
                SavedBy = "admin-user"
            });

            // Act
            store.DeleteRolloverRule("evidence", "admin-user");
            var history = store.GetRolloverRuleHistory("evidence");

            // Assert
            Assert.Equal(2, history.Count); // Created + Deleted
            Assert.Contains(history, h => h.ChangeType == "created");
            Assert.Contains(history, h => h.ChangeType == "deleted");
        }

        [Fact]
        public void Rollover_WithCopyRule_ShouldCopyDataValues()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            // Add a section with a data point
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;

            var dataPoint = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Narrative",
                Content = "This is test content",
                OwnerId = "user1",
                Source = "Test",
                InformationType = "measured",
                CompletenessStatus = "complete"
            });

            // Configure "Copy" rule for narrative (default behavior)
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "narrative",
                RuleType = "copy",
                SavedBy = "admin-user"
            });

            // Act
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1"
            };

            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result?.TargetPeriod);

            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var targetSection = targetSections.First();
            var targetDataPoints = store.GetDataPoints(targetSection.Id);
            
            Assert.Single(targetDataPoints);
            var copiedDataPoint = targetDataPoints.First();
            Assert.Equal("This is test content", copiedDataPoint.Content);
            Assert.Equal("narrative", copiedDataPoint.Type);
        }

        [Fact]
        public void Rollover_WithResetRule_ShouldCreateEmptyPlaceholder()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            // Add a section with a metric data point
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;

            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "1000 kWh",
                Value = "1000",
                Unit = "kWh",
                OwnerId = "user1",
                Source = "Meter",
                InformationType = "measured",
                CompletenessStatus = "complete"
            });

            // Configure "Reset" rule for metrics
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "metric",
                RuleType = "reset",
                Description = "Metrics should be reset each period",
                SavedBy = "admin-user"
            });

            // Act
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1"
            };

            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result?.TargetPeriod);

            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var targetSection = targetSections.First();
            var targetDataPoints = store.GetDataPoints(targetSection.Id);
            
            Assert.Single(targetDataPoints);
            var resetDataPoint = targetDataPoints.First();
            Assert.Equal(string.Empty, resetDataPoint.Content); // Content is reset
            Assert.Null(resetDataPoint.Value); // Value is reset
            Assert.Equal("missing", resetDataPoint.CompletenessStatus); // Marked as missing
            Assert.Equal("metric", resetDataPoint.Type);
            Assert.Equal("kWh", resetDataPoint.Unit); // Unit preserved for consistency
        }

        [Fact]
        public void Rollover_WithCopyAsDraftRule_ShouldRequireReview()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            // Add a section with a policy data point
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;

            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "policy",
                Title = "Environmental Policy",
                Content = "Our commitment to sustainability",
                OwnerId = "user1",
                Source = "Policy Document",
                InformationType = "measured",
                CompletenessStatus = "complete"
            });

            // Configure "CopyAsDraft" rule for policies
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "policy",
                RuleType = "copy-as-draft",
                Description = "Policies need annual review",
                SavedBy = "admin-user"
            });

            // Act
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1"
            };

            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result?.TargetPeriod);

            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var targetSection = targetSections.First();
            var targetDataPoints = store.GetDataPoints(targetSection.Id);
            
            Assert.Single(targetDataPoints);
            var draftDataPoint = targetDataPoints.First();
            Assert.Contains("[Carried forward - Requires Review]", draftDataPoint.Content);
            Assert.Contains("Our commitment to sustainability", draftDataPoint.Content);
            Assert.Equal("incomplete", draftDataPoint.CompletenessStatus); // Marked as incomplete
            Assert.Equal("draft", draftDataPoint.ReviewStatus); // Explicitly draft
        }

        [Fact]
        public void Rollover_WithRuleOverride_ShouldUseOverrideInsteadOfConfigured()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            // Add a section with a KPI data point
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;

            store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "kpi",
                Title = "Carbon Emissions",
                Content = "500 tons CO2",
                Value = "500",
                Unit = "tons CO2",
                OwnerId = "user1",
                Source = "ERP",
                InformationType = "measured",
                CompletenessStatus = "complete"
            });

            // Configure "Copy" rule for KPIs
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "kpi",
                RuleType = "copy",
                SavedBy = "admin-user"
            });

            // Act - Override with "Reset" for this specific rollover
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1",
                RuleOverrides = new List<RolloverRuleOverride>
                {
                    new RolloverRuleOverride
                    {
                        DataType = "kpi",
                        RuleType = DataTypeRolloverRuleType.Reset
                    }
                }
            };

            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result?.TargetPeriod);

            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var targetSection = targetSections.First();
            var targetDataPoints = store.GetDataPoints(targetSection.Id);
            
            Assert.Single(targetDataPoints);
            var kpiDataPoint = targetDataPoints.First();
            
            // Should be reset (override rule) not copied (configured rule)
            Assert.Equal(string.Empty, kpiDataPoint.Content);
            Assert.Null(kpiDataPoint.Value);
            Assert.Equal("missing", kpiDataPoint.CompletenessStatus);
        }

        [Fact]
        public void RolloverRuleHistory_ShouldTrackAllChanges()
        {
            // Arrange
            var store = new InMemoryReportStore();

            // Act - Create, update, and delete a rule
            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "target",
                RuleType = "copy",
                Description = "Initial rule",
                SavedBy = "admin1"
            });

            store.SaveRolloverRule(new SaveDataTypeRolloverRuleRequest
            {
                DataType = "target",
                RuleType = "copy-as-draft",
                Description = "Updated rule",
                SavedBy = "admin2"
            });

            store.DeleteRolloverRule("target", "admin3");

            // Get history
            var history = store.GetRolloverRuleHistory("target");

            // Assert
            Assert.Equal(3, history.Count);
            
            // Check all change types are present
            Assert.Contains(history, h => h.ChangeType == "deleted");
            Assert.Contains(history, h => h.ChangeType == "updated");
            Assert.Contains(history, h => h.ChangeType == "created");
            
            // Check versions
            var deletedEntry = history.First(h => h.ChangeType == "deleted");
            var updatedEntry = history.First(h => h.ChangeType == "updated");
            var createdEntry = history.First(h => h.ChangeType == "created");
            
            Assert.Equal(2, deletedEntry.Version);
            Assert.Equal(2, updatedEntry.Version);
            Assert.Equal(1, createdEntry.Version);
        }
    }
}
