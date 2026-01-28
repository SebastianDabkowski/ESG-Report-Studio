using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ValidationRuleTests
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
        public void CreateValidationRule_WithAllRequiredFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Value must be non-negative.",
                CreatedBy = "user-1"
            };

            // Act
            var (isValid, errorMessage, rule) = store.CreateValidationRule(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(rule);
            Assert.Equal("non-negative", rule.RuleType);
            Assert.Equal("Value must be non-negative.", rule.ErrorMessage);
            Assert.True(rule.IsActive);
            Assert.NotEmpty(rule.Id);
            Assert.NotEmpty(rule.CreatedAt);
        }

        [Fact]
        public void CreateValidationRule_WithoutSectionId_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();

            var request = new CreateValidationRuleRequest
            {
                SectionId = "",
                RuleType = "non-negative",
                ErrorMessage = "Value must be non-negative.",
                CreatedBy = "user-1"
            };

            // Act
            var (isValid, errorMessage, rule) = store.CreateValidationRule(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("SectionId is required.", errorMessage);
            Assert.Null(rule);
        }

        [Fact]
        public void CreateValidationRule_WithInvalidRuleType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "invalid-type",
                ErrorMessage = "Error message",
                CreatedBy = "user-1"
            };

            // Act
            var (isValid, errorMessage, rule) = store.CreateValidationRule(request);

            // Assert
            Assert.False(isValid);
            Assert.Contains("RuleType must be one of:", errorMessage);
            Assert.Null(rule);
        }

        [Fact]
        public void DataPoint_WithNegativeValue_ShouldFailValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a non-negative validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Energy consumption cannot be negative.",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "-100",
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Energy consumption cannot be negative.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void DataPoint_WithPositiveValue_ShouldPassValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a non-negative validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Energy consumption cannot be negative.",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "user-1",
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
            Assert.Equal("1000", dataPoint.Value);
        }

        [Fact]
        public void DataPoint_WithValueButNoUnit_ShouldFailRequiredUnitValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a required-unit validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "required-unit",
                TargetField = "unit",
                ErrorMessage = "Unit is required when providing a numeric value.",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Unit is required when providing a numeric value.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void DataPoint_WithInvalidUnit_ShouldFailAllowedUnitsValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create an allowed-units validation rule
            var allowedUnits = new[] { "MWh", "kWh", "GJ" };
            var parametersJson = System.Text.Json.JsonSerializer.Serialize(allowedUnits);
            
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "allowed-units",
                TargetField = "unit",
                Parameters = parametersJson,
                ErrorMessage = "Unit must be one of: MWh, kWh, GJ.",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "BTU", // Invalid unit
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Unit must be one of: MWh, kWh, GJ.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void DataPoint_WithValidUnit_ShouldPassAllowedUnitsValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create an allowed-units validation rule
            var allowedUnits = new[] { "MWh", "kWh", "GJ" };
            var parametersJson = System.Text.Json.JsonSerializer.Serialize(allowedUnits);
            
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "allowed-units",
                TargetField = "unit",
                Parameters = parametersJson,
                ErrorMessage = "Unit must be one of: MWh, kWh, GJ.",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "MWh", // Valid unit
                OwnerId = "user-1",
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
            Assert.Equal("MWh", dataPoint.Unit);
        }

        [Fact]
        public void DataPoint_WithDateOutsidePeriod_ShouldFailValueWithinPeriodValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a value-within-period validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "value-within-period",
                TargetField = "value",
                ErrorMessage = "Date must be within the reporting period (2024-01-01 to 2024-12-31).",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "date",
                Title = "Policy Adoption Date",
                Content = "Date when the policy was adopted",
                Value = "2025-06-15", // Outside reporting period
                OwnerId = "user-1",
                Source = "Board Meeting Minutes",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Date must be within the reporting period (2024-01-01 to 2024-12-31).", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void DataPoint_WithDateWithinPeriod_ShouldPassValueWithinPeriodValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a value-within-period validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "value-within-period",
                TargetField = "value",
                ErrorMessage = "Date must be within the reporting period (2024-01-01 to 2024-12-31).",
                CreatedBy = "user-1"
            });

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "date",
                Title = "Policy Adoption Date",
                Content = "Date when the policy was adopted",
                Value = "2024-06-15", // Within reporting period
                OwnerId = "user-1",
                Source = "Board Meeting Minutes",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
            Assert.Equal("2024-06-15", dataPoint.Value);
        }

        [Fact]
        public void UpdateDataPoint_WithRuleViolation_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a data point first
            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption for 2024",
                Value = "1000",
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, createdDataPoint) = store.CreateDataPoint(createRequest);

            // Now add a validation rule
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Energy consumption cannot be negative.",
                CreatedBy = "user-1"
            });

            // Try to update with a negative value
            var updateRequest = new UpdateDataPointRequest
            {
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Updated content",
                Value = "-500", // Negative value
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(createdDataPoint!.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Energy consumption cannot be negative.", errorMessage);
            Assert.Null(updatedDataPoint);
        }

        [Fact]
        public void UpdateValidationRule_ShouldApplyToSubsequentSaves()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a validation rule
            var (_, _, rule) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Original error message.",
                CreatedBy = "user-1"
            });

            // Update the validation rule with a new error message
            var updateRuleRequest = new UpdateValidationRuleRequest
            {
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Updated error message: Value cannot be negative.",
                IsActive = true,
                UpdatedBy = "user-1"
            };

            store.UpdateValidationRule(rule!.Id, updateRuleRequest);

            // Try to create a data point that violates the rule
            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "-100",
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert - Should use the updated error message
            Assert.False(isValid);
            Assert.Equal("Updated error message: Value cannot be negative.", errorMessage);
            Assert.Null(dataPoint);
        }

        [Fact]
        public void InactiveValidationRule_ShouldNotBeApplied()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a validation rule
            var (_, _, rule) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Value cannot be negative.",
                CreatedBy = "user-1"
            });

            // Deactivate the rule
            var updateRuleRequest = new UpdateValidationRuleRequest
            {
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Value cannot be negative.",
                IsActive = false,
                UpdatedBy = "user-1"
            };

            store.UpdateValidationRule(rule!.Id, updateRuleRequest);

            // Try to create a data point with negative value
            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "-100",
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert - Should succeed because rule is inactive
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
        }

        [Fact]
        public void DeleteValidationRule_ShouldRemoveValidation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create a validation rule
            var (_, _, rule) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Value cannot be negative.",
                CreatedBy = "user-1"
            });

            // Delete the rule
            var deleted = store.DeleteValidationRule(rule!.Id, "user-1");
            Assert.True(deleted);

            // Try to create a data point with negative value
            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "numeric",
                Title = "Energy Consumption",
                Content = "Total energy consumption",
                Value = "-100",
                Unit = "MWh",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            // Act
            var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

            // Assert - Should succeed because rule is deleted
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(dataPoint);
        }

        [Fact]
        public void GetValidationRules_WithSectionId_ShouldReturnOnlyActiveRulesForSection()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId1 = CreateTestSection(store);
            var sectionId2 = CreateTestSection(store);

            // Create rules for both sections
            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId1,
                RuleType = "non-negative",
                ErrorMessage = "Error 1",
                CreatedBy = "user-1"
            });

            var (_, _, rule2) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId1,
                RuleType = "required-unit",
                ErrorMessage = "Error 2",
                CreatedBy = "user-1"
            });

            store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId2,
                RuleType = "non-negative",
                ErrorMessage = "Error 3",
                CreatedBy = "user-1"
            });

            // Deactivate one rule
            store.UpdateValidationRule(rule2!.Id, new UpdateValidationRuleRequest
            {
                RuleType = "required-unit",
                ErrorMessage = "Error 2",
                IsActive = false,
                UpdatedBy = "user-1"
            });

            // Act
            var rules = store.GetValidationRules(sectionId1);

            // Assert - Should only return active rules for section 1
            Assert.Single(rules);
            Assert.Equal(sectionId1, rules[0].SectionId);
            Assert.Equal("non-negative", rules[0].RuleType);
        }

        [Fact]
        public void CreateValidationRule_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Value must be non-negative.",
                CreatedBy = "user-1"
            };

            // Act
            var (isValid, errorMessage, rule) = store.CreateValidationRule(request);
            var auditLog = store.GetAuditLog(entityType: "ValidationRule", entityId: rule!.Id, action: "create");

            // Assert
            Assert.True(isValid);
            var auditEntry = auditLog.FirstOrDefault();
            
            Assert.NotNull(auditEntry);
            Assert.Equal("user-1", auditEntry.UserId);
            Assert.Equal("create", auditEntry.Action);
            Assert.Contains("Created validation rule", auditEntry.ChangeNote);
            Assert.NotEmpty(auditEntry.Changes);
            Assert.Contains(auditEntry.Changes, c => c.Field == "RuleType" && c.NewValue == "non-negative");
            Assert.Contains(auditEntry.Changes, c => c.Field == "Description" && c.NewValue == "Value must be non-negative.");
        }

        [Fact]
        public void UpdateValidationRule_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, rule) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Original message.",
                CreatedBy = "user-1"
            });

            // Act
            store.UpdateValidationRule(rule!.Id, new UpdateValidationRuleRequest
            {
                RuleType = "required-unit",
                TargetField = "unit",
                ErrorMessage = "Updated message.",
                IsActive = false,
                UpdatedBy = "user-2"
            });
            var auditLog = store.GetAuditLog(entityType: "ValidationRule", entityId: rule.Id, action: "update");

            // Assert
            var auditEntry = auditLog.FirstOrDefault();
            
            Assert.NotNull(auditEntry);
            Assert.Equal("user-2", auditEntry.UserId);
            Assert.Equal("update", auditEntry.Action);
            Assert.Contains("Updated validation rule", auditEntry.ChangeNote);
            Assert.NotEmpty(auditEntry.Changes);
            Assert.Contains(auditEntry.Changes, c => c.Field == "RuleType" && c.OldValue == "non-negative" && c.NewValue == "required-unit");
            Assert.Contains(auditEntry.Changes, c => c.Field == "Description" && c.OldValue == "Original message." && c.NewValue == "Updated message.");
            Assert.Contains(auditEntry.Changes, c => c.Field == "Enabled" && c.OldValue == "True" && c.NewValue == "False");
        }

        [Fact]
        public void DeleteValidationRule_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, rule) = store.CreateValidationRule(new CreateValidationRuleRequest
            {
                SectionId = sectionId,
                RuleType = "non-negative",
                TargetField = "value",
                ErrorMessage = "Test rule.",
                CreatedBy = "user-1"
            });

            // Act
            var deleted = store.DeleteValidationRule(rule!.Id, "user-2");
            var auditLog = store.GetAuditLog(entityType: "ValidationRule", entityId: rule.Id, action: "delete");

            // Assert
            Assert.True(deleted);
            var auditEntry = auditLog.FirstOrDefault();
            
            Assert.NotNull(auditEntry);
            Assert.Equal("user-2", auditEntry.UserId);
            Assert.Equal("delete", auditEntry.Action);
            Assert.Contains("Deleted validation rule", auditEntry.ChangeNote);
            Assert.NotEmpty(auditEntry.Changes);
            Assert.Contains(auditEntry.Changes, c => c.Field == "Description" && c.OldValue == "Test rule." && c.NewValue == "");
        }
    }
}
