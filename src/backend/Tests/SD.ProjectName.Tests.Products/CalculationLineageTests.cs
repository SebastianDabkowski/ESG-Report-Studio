using ARP.ESG_ReportStudio.API.Reporting;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    public class CalculationLineageTests
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
                Description = "Test section for calculation lineage",
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
        public void CreateDataPoint_WithCalculationFields_ShouldStoreLineageMetadata()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            // Create input data points
            var input1Request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Total Energy Consumption",
                Content = "Total energy consumed in kWh",
                Value = "10000",
                Unit = "kWh",
                OwnerId = userId,
                Source = "Energy meter",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input1) = store.CreateDataPoint(input1Request);

            var input2Request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Total Revenue",
                Content = "Total revenue in USD",
                Value = "1000000",
                Unit = "USD",
                OwnerId = userId,
                Source = "Financial system",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input2) = store.CreateDataPoint(input2Request);

            // Create calculated data point
            var calculatedRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Intensity",
                Content = "Energy consumption per dollar of revenue",
                Value = "0.01",
                Unit = "kWh/USD",
                OwnerId = userId,
                Source = "Calculated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = true,
                CalculationFormula = "Total Energy Consumption / Total Revenue",
                CalculationInputIds = new List<string> { input1!.Id, input2!.Id },
                CalculatedBy = userId
            };

            // Act
            var (isValid, errorMessage, calculated) = store.CreateDataPoint(calculatedRequest);

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(calculated);
            Assert.True(calculated.IsCalculated);
            Assert.Equal("Total Energy Consumption / Total Revenue", calculated.CalculationFormula);
            Assert.Equal(2, calculated.CalculationInputIds.Count);
            Assert.Contains(input1.Id, calculated.CalculationInputIds);
            Assert.Contains(input2.Id, calculated.CalculationInputIds);
            Assert.Equal(1, calculated.CalculationVersion);
            Assert.NotNull(calculated.CalculatedAt);
            Assert.Equal(userId, calculated.CalculatedBy);
            Assert.NotNull(calculated.CalculationInputSnapshot);
            Assert.False(calculated.CalculationNeedsRecalculation);
        }

        [Fact]
        public void UpdateInputDataPoint_ShouldFlagCalculatedPointForRecalculation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            // Create input data point
            var inputRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Emissions",
                Content = "Total emissions",
                Value = "100",
                Unit = "tCO2e",
                OwnerId = userId,
                Source = "Emissions tracker",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input) = store.CreateDataPoint(inputRequest);

            // Create calculated data point
            var calculatedRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Emissions per Employee",
                Content = "Emissions intensity",
                Value = "10",
                Unit = "tCO2e/employee",
                OwnerId = userId,
                Source = "Calculated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = true,
                CalculationFormula = "Emissions / Employee Count",
                CalculationInputIds = new List<string> { input!.Id },
                CalculatedBy = userId
            };
            var (_, _, calculated) = store.CreateDataPoint(calculatedRequest);

            // Act - Update the input value
            var updateRequest = new UpdateDataPointRequest
            {
                Type = input.Type,
                Title = input.Title,
                Content = input.Content,
                Value = "150", // Changed from 100
                Unit = input.Unit,
                OwnerId = input.OwnerId,
                Source = input.Source,
                InformationType = input.InformationType,
                CompletenessStatus = input.CompletenessStatus,
                UpdatedBy = userId
            };
            store.UpdateDataPoint(input.Id, updateRequest);

            // Assert - Calculated point should be flagged
            var updatedCalculated = store.GetDataPoint(calculated!.Id);
            Assert.NotNull(updatedCalculated);
            Assert.True(updatedCalculated.CalculationNeedsRecalculation);
            Assert.NotNull(updatedCalculated.RecalculationReason);
            Assert.Contains("Emissions", updatedCalculated.RecalculationReason);
            Assert.NotNull(updatedCalculated.RecalculationFlaggedAt);
        }

        [Fact]
        public void GetCalculationLineage_ForCalculatedDataPoint_ShouldReturnCompleteLineage()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            // Create multiple input data points
            var input1Request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Scope 1 Emissions",
                Content = "Direct emissions",
                Value = "50",
                Unit = "tCO2e",
                OwnerId = userId,
                Source = "Direct measurement",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input1) = store.CreateDataPoint(input1Request);

            var input2Request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Scope 2 Emissions",
                Content = "Indirect emissions",
                Value = "30",
                Unit = "tCO2e",
                OwnerId = userId,
                Source = "Utility bills",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input2) = store.CreateDataPoint(input2Request);

            // Create calculated data point
            var calculatedRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Total Emissions",
                Content = "Sum of all emissions",
                Value = "80",
                Unit = "tCO2e",
                OwnerId = userId,
                Source = "Calculated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = true,
                CalculationFormula = "Scope 1 + Scope 2",
                CalculationInputIds = new List<string> { input1!.Id, input2!.Id },
                CalculatedBy = userId
            };
            var (_, _, calculated) = store.CreateDataPoint(calculatedRequest);

            // Act
            var lineage = store.GetCalculationLineage(calculated!.Id);

            // Assert
            Assert.NotNull(lineage);
            Assert.Equal(calculated.Id, lineage.DataPointId);
            Assert.Equal("Scope 1 + Scope 2", lineage.Formula);
            Assert.Equal(1, lineage.Version);
            Assert.NotNull(lineage.CalculatedAt);
            Assert.Equal(userId, lineage.CalculatedBy);
            Assert.Equal(2, lineage.Inputs.Count);
            Assert.False(lineage.NeedsRecalculation);
            
            var input1Lineage = lineage.Inputs.FirstOrDefault(i => i.DataPointId == input1.Id);
            Assert.NotNull(input1Lineage);
            Assert.Equal("Scope 1 Emissions", input1Lineage.Title);
            Assert.Equal("50", input1Lineage.CurrentValue);
            Assert.Equal("tCO2e", input1Lineage.Unit);
            Assert.False(input1Lineage.HasChanged);
        }

        [Fact]
        public void GetCalculationLineage_ForNonCalculatedDataPoint_ShouldReturnNull()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            var dataPointRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Regular Data Point",
                Content = "Not calculated",
                Value = "100",
                Unit = "units",
                OwnerId = userId,
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = false
            };
            var (_, _, dataPoint) = store.CreateDataPoint(dataPointRequest);

            // Act
            var lineage = store.GetCalculationLineage(dataPoint!.Id);

            // Assert
            Assert.Null(lineage);
        }

        [Fact]
        public void RecalculateDataPoint_ShouldUpdateVersionAndSnapshot()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";
            var calculatorId = "user-2";

            // Create input
            var inputRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Base Value",
                Content = "Input value",
                Value = "100",
                Unit = "units",
                OwnerId = userId,
                Source = "System",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input) = store.CreateDataPoint(inputRequest);

            // Create calculated
            var calculatedRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Doubled Value",
                Content = "Base * 2",
                Value = "200",
                Unit = "units",
                OwnerId = userId,
                Source = "Calculated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = true,
                CalculationFormula = "Base Value * 2",
                CalculationInputIds = new List<string> { input!.Id },
                CalculatedBy = userId
            };
            var (_, _, calculated) = store.CreateDataPoint(calculatedRequest);
            var originalVersion = calculated!.CalculationVersion;

            // Update input to trigger recalculation flag
            var updateRequest = new UpdateDataPointRequest
            {
                Type = input.Type,
                Title = input.Title,
                Content = input.Content,
                Value = "150",
                Unit = input.Unit,
                OwnerId = input.OwnerId,
                Source = input.Source,
                InformationType = input.InformationType,
                CompletenessStatus = input.CompletenessStatus,
                UpdatedBy = userId
            };
            store.UpdateDataPoint(input.Id, updateRequest);

            // Act - Recalculate
            var recalcRequest = new RecalculateDataPointRequest
            {
                CalculatedBy = calculatorId,
                ChangeNote = "Recalculated after input change"
            };
            var (isValid, errorMessage, recalculated) = store.RecalculateDataPoint(
                calculated.Id, 
                recalcRequest, 
                "300", // New calculated value
                "units"
            );

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(recalculated);
            Assert.Equal("300", recalculated.Value);
            Assert.Equal(originalVersion + 1, recalculated.CalculationVersion);
            Assert.False(recalculated.CalculationNeedsRecalculation);
            Assert.Null(recalculated.RecalculationReason);
            Assert.Equal(calculatorId, recalculated.CalculatedBy);
            Assert.NotNull(recalculated.CalculatedAt);
            
            // Verify snapshot was updated
            Assert.NotNull(recalculated.CalculationInputSnapshot);
            Assert.Contains("150", recalculated.CalculationInputSnapshot); // New input value
        }

        [Fact]
        public void RecalculateDataPoint_ForNonCalculatedPoint_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            var dataPointRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Manual Data",
                Content = "Not calculated",
                Value = "100",
                Unit = "units",
                OwnerId = userId,
                Source = "Manual",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = false
            };
            var (_, _, dataPoint) = store.CreateDataPoint(dataPointRequest);

            // Act
            var recalcRequest = new RecalculateDataPointRequest
            {
                CalculatedBy = userId
            };
            var (isValid, errorMessage, _) = store.RecalculateDataPoint(
                dataPoint!.Id, 
                recalcRequest, 
                "200", 
                "units"
            );

            // Assert
            Assert.False(isValid);
            Assert.Contains("not a calculated value", errorMessage);
        }

        [Fact]
        public void CalculationLineage_WithChangedInputs_ShouldDetectChanges()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);
            // Use predefined user-1
            var userId = "user-1";

            // Create input
            var inputRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Use",
                Content = "Energy consumption",
                Value = "500",
                Unit = "kWh",
                OwnerId = userId,
                Source = "Meter",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, input) = store.CreateDataPoint(inputRequest);

            // Create calculated
            var calculatedRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Cost",
                Content = "Cost calculation",
                Value = "50",
                Unit = "USD",
                OwnerId = userId,
                Source = "Calculated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                IsCalculated = true,
                CalculationFormula = "Energy Use * 0.1",
                CalculationInputIds = new List<string> { input!.Id },
                CalculatedBy = userId
            };
            var (_, _, calculated) = store.CreateDataPoint(calculatedRequest);

            // Update input
            var updateRequest = new UpdateDataPointRequest
            {
                Type = input.Type,
                Title = input.Title,
                Content = input.Content,
                Value = "600", // Changed
                Unit = input.Unit,
                OwnerId = input.OwnerId,
                Source = input.Source,
                InformationType = input.InformationType,
                CompletenessStatus = input.CompletenessStatus,
                UpdatedBy = userId
            };
            store.UpdateDataPoint(input.Id, updateRequest);

            // Act
            var lineage = store.GetCalculationLineage(calculated!.Id);

            // Assert
            Assert.NotNull(lineage);
            Assert.True(lineage.NeedsRecalculation);
            Assert.NotNull(lineage.RecalculationReason);
            Assert.Single(lineage.Inputs);
            
            var inputLineage = lineage.Inputs[0];
            Assert.True(inputLineage.HasChanged);
            Assert.Equal("600", inputLineage.CurrentValue);
            Assert.Equal("500", inputLineage.ValueAtCalculation);
        }
    }
}
