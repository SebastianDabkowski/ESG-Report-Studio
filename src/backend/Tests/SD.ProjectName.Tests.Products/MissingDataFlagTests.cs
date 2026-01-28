using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class MissingDataFlagTests
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
        public void FlagMissingData_WithValidReason_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Scope 3 Emissions",
                Content = "Emissions from supply chain",
                OwnerId = "owner-1",
                Source = "Supplier Data",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "unavailable-from-supplier",
                MissingReason = "Supplier has not provided Scope 3 data for Q3 and Q4"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.FlagMissingData(dataPoint.Id, flagRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.True(updatedDataPoint.IsMissing);
            Assert.Equal("unavailable-from-supplier", updatedDataPoint.MissingReasonCategory);
            Assert.Equal("Supplier has not provided Scope 3 data for Q3 and Q4", updatedDataPoint.MissingReason);
            Assert.Equal("owner-1", updatedDataPoint.MissingFlaggedBy);
            Assert.NotNull(updatedDataPoint.MissingFlaggedAt);
            Assert.Equal("missing", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void FlagMissingData_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Water Consumption",
                Content = "Total water usage",
                OwnerId = "owner-1",
                Source = "Water Meters",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "not-measured",
                MissingReason = "Water meters not installed in new facility yet"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.FlagMissingData(dataPoint.Id, flagRequest);

            // Assert
            Assert.True(isValid);
            
            var auditLog = store.GetAuditLog();
            var flagEntry = auditLog.FirstOrDefault(e => 
                e.Action == "flag-missing" && 
                e.EntityId == dataPoint.Id);

            Assert.NotNull(flagEntry);
            Assert.Equal("owner-1", flagEntry.UserId);
            Assert.Equal("Test Owner", flagEntry.UserName);
            Assert.Contains("not-measured", flagEntry.ChangeNote);
            Assert.Contains(flagEntry.Changes, c => 
                c.Field == "IsMissing" && 
                c.NewValue == "True");
        }

        [Fact]
        public void FlagMissingData_WithoutReason_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "other",
                MissingReason = "" // Empty reason
            };

            var (isValid, errorMessage, updatedDataPoint) = store.FlagMissingData(dataPoint.Id, flagRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("MissingReason cannot be empty", errorMessage);
        }

        [Fact]
        public void FlagMissingData_WithInvalidCategory_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "invalid-category",
                MissingReason = "Some reason"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.FlagMissingData(dataPoint.Id, flagRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("must be one of", errorMessage);
        }

        [Fact]
        public void UnflagMissingData_ShouldClearMissingFields()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Carbon Emissions",
                Content = "Total carbon footprint",
                OwnerId = "owner-1",
                Source = "Carbon Calculator",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Flag it first
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "data-quality-issue",
                MissingReason = "Data from old system has accuracy issues"
            };
            store.FlagMissingData(dataPoint.Id, flagRequest);

            // Act
            var unflagRequest = new UnflagMissingDataRequest
            {
                UnflaggedBy = "owner-1",
                ChangeNote = "New carbon calculator system now in place"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.UnflagMissingData(dataPoint.Id, unflagRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.False(updatedDataPoint.IsMissing);
            Assert.Null(updatedDataPoint.MissingReason);
            Assert.Null(updatedDataPoint.MissingReasonCategory);
            Assert.Equal("incomplete", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void UnflagMissingData_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Waste Generation",
                Content = "Total waste produced",
                OwnerId = "owner-1",
                Source = "Waste Management System",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Flag it first
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "system-limitation",
                MissingReason = "Old system couldn't track waste categories"
            };
            store.FlagMissingData(dataPoint.Id, flagRequest);

            // Act
            var unflagRequest = new UnflagMissingDataRequest
            {
                UnflaggedBy = "owner-1",
                ChangeNote = "Upgraded to new waste tracking system"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.UnflagMissingData(dataPoint.Id, unflagRequest);

            // Assert
            Assert.True(isValid);
            
            var auditLog = store.GetAuditLog();
            var unflagEntry = auditLog.FirstOrDefault(e => 
                e.Action == "unflag-missing" && 
                e.EntityId == dataPoint.Id);

            Assert.NotNull(unflagEntry);
            Assert.Equal("owner-1", unflagEntry.UserId);
            Assert.Equal("Test Owner", unflagEntry.UserName);
            Assert.Contains(unflagEntry.Changes, c => 
                c.Field == "IsMissing" && 
                c.NewValue == "False");
        }

        [Fact]
        public void UnflagMissingData_OnNonFlaggedDataPoint_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "owner-1",
                Source = "Test Source",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Try to unflag without flagging first
            var unflagRequest = new UnflagMissingDataRequest
            {
                UnflaggedBy = "owner-1"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.UnflagMissingData(dataPoint.Id, unflagRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not currently flagged as missing", errorMessage);
        }

        [Fact]
        public void FlagMissingData_ShouldPreserveHistoryInAuditLog()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Employee Headcount",
                Content = "Total number of employees",
                OwnerId = "owner-1",
                Source = "HR System",
                InformationType = "fact"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Flag it
            var flagRequest = new FlagMissingDataRequest
            {
                FlaggedBy = "owner-1",
                MissingReasonCategory = "not-applicable",
                MissingReason = "No employees at this location during reporting period"
            };
            store.FlagMissingData(dataPoint.Id, flagRequest);

            // Unflag it
            var unflagRequest = new UnflagMissingDataRequest
            {
                UnflaggedBy = "owner-1",
                ChangeNote = "New office opened, now have employees"
            };
            store.UnflagMissingData(dataPoint.Id, unflagRequest);

            // Assert - Check audit log has both entries
            var auditLog = store.GetAuditLog();
            var flagEntry = auditLog.FirstOrDefault(e => 
                e.Action == "flag-missing" && 
                e.EntityId == dataPoint.Id);
            var unflagEntry = auditLog.FirstOrDefault(e => 
                e.Action == "unflag-missing" && 
                e.EntityId == dataPoint.Id);

            Assert.NotNull(flagEntry);
            Assert.NotNull(unflagEntry);
            Assert.Contains("not-applicable", flagEntry.ChangeNote);
            Assert.Contains("New office opened", unflagEntry.ChangeNote); // Custom change note
        }

        [Fact]
        public void FlagMissingData_WithAllCategories_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var categories = new[] 
            { 
                "not-measured", 
                "not-applicable", 
                "unavailable-from-supplier",
                "data-quality-issue",
                "system-limitation",
                "other"
            };

            foreach (var category in categories)
            {
                var createRequest = new CreateDataPointRequest
                {
                    SectionId = sectionId,
                    Type = "metric",
                    Title = $"Test Data Point - {category}",
                    Content = "Test content",
                    OwnerId = "owner-1",
                    Source = "Test Source",
                    InformationType = "fact"
                };

                var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
                Assert.NotNull(dataPoint);

                // Act
                var flagRequest = new FlagMissingDataRequest
                {
                    FlaggedBy = "owner-1",
                    MissingReasonCategory = category,
                    MissingReason = $"Testing {category} category"
                };

                var (isValid, errorMessage, updatedDataPoint) = store.FlagMissingData(dataPoint.Id, flagRequest);

                // Assert
                Assert.True(isValid, $"Failed for category: {category}");
                Assert.Null(errorMessage);
                Assert.Equal(category, updatedDataPoint!.MissingReasonCategory);
            }
        }
    }
}
