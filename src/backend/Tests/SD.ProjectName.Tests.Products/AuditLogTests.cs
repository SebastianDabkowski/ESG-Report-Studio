using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class AuditLogTests
    {
        private static InMemoryReportStore CreateStoreWithTestData()
        {
            var store = new InMemoryReportStore();
            
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
                OwnerName = "Sarah Chen"
            });

            // Create section
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
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
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);

            return store;
        }

        [Fact]
        public void UpdateAssumption_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, assumption) = store.CreateAssumption(
                sectionId,
                "Original Title",
                "Original Description",
                "Company-wide",
                "2024-01-01",
                "2024-12-31",
                "Original Methodology",
                "Original Limitations",
                new List<string>(),
                "Original Rationale",
                new List<AssumptionSource>(),
                "user-1"
            );

            // Act
            var (isValid, errorMessage, updatedAssumption) = store.UpdateAssumption(
                assumption!.Id,
                "Updated Title",
                "Updated Description",
                "Specific facility",
                "2024-01-01",
                "2024-12-31",
                "Updated Methodology",
                "Updated Limitations",
                new List<string>(),
                "Updated Rationale",
                new List<AssumptionSource>(),
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "Assumption", entityId: assumption.Id);
            
            // Should have update entry (create is not logged)
            Assert.Single(auditEntries);
            
            var updateEntry = auditEntries.First();
            Assert.Equal("update", updateEntry.Action);
            Assert.Equal("user-1", updateEntry.UserId);
            Assert.Equal("Assumption", updateEntry.EntityType);
            Assert.Equal(assumption.Id, updateEntry.EntityId);
            
            // Check that field changes were captured
            Assert.NotEmpty(updateEntry.Changes);
            Assert.Contains(updateEntry.Changes, c => c.Field == "Title" && c.OldValue == "Original Title" && c.NewValue == "Updated Title");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Description" && c.OldValue == "Original Description" && c.NewValue == "Updated Description");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Scope" && c.OldValue == "Company-wide" && c.NewValue == "Specific facility");
        }

        [Fact]
        public void DeprecateAssumption_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, assumption) = store.CreateAssumption(
                sectionId,
                "Test Assumption",
                "Description",
                "Company-wide",
                "2024-01-01",
                "2024-12-31",
                "Methodology",
                "Limitations",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user-1"
            );

            // Act
            var (isValid, errorMessage) = store.DeprecateAssumption(
                assumption!.Id,
                null,
                "No longer valid due to new regulations",
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "Assumption", entityId: assumption.Id);
            
            // Should have deprecate entry (create is not logged)
            Assert.Single(auditEntries);
            
            var deprecateEntry = auditEntries.First();
            Assert.Equal("deprecate", deprecateEntry.Action);
            Assert.Equal("user-1", deprecateEntry.UserId);
            Assert.Contains(deprecateEntry.Changes, c => c.Field == "Status" && c.OldValue == "active" && c.NewValue == "invalid");
            Assert.Contains(deprecateEntry.Changes, c => c.Field == "DeprecationJustification" && c.NewValue == "No longer valid due to new regulations");
        }

        [Fact]
        public void LinkAssumptionToDataPoint_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, assumption) = store.CreateAssumption(
                sectionId,
                "Test Assumption",
                "Description",
                "Company-wide",
                "2024-01-01",
                "2024-12-31",
                "Methodology",
                "Limitations",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user-1"
            );

            var (_, _, dataPoint) = store.CreateDataPoint(new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy used",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            });

            // Act
            var (isValid, errorMessage) = store.LinkAssumptionToDataPoint(
                assumption!.Id,
                dataPoint!.Id,
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "Assumption", entityId: assumption.Id);
            
            // Should have link entry (create is not logged)
            Assert.Single(auditEntries);
            
            var linkEntry = auditEntries.First();
            Assert.Equal("link", linkEntry.Action);
            Assert.Equal("user-1", linkEntry.UserId);
            Assert.Contains(linkEntry.Changes, c => c.Field == "LinkedDataPointIds");
        }

        [Fact]
        public void CreateGap_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            // Act
            var (isValid, errorMessage, gap) = store.CreateGap(
                sectionId,
                "Missing Data",
                "Critical data unavailable",
                "high",
                "Implement data collection process",
                "2024-06-30",
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            Assert.NotNull(gap);
            
            var auditEntries = store.GetAuditLog(entityType: "Gap", entityId: gap.Id);
            Assert.Single(auditEntries);
            
            var createEntry = auditEntries.First();
            Assert.Equal("create", createEntry.Action);
            Assert.Equal("user-1", createEntry.UserId);
            Assert.Equal("Gap", createEntry.EntityType);
            Assert.Contains(createEntry.Changes, c => c.Field == "Title" && c.NewValue == "Missing Data");
            Assert.Contains(createEntry.Changes, c => c.Field == "Impact" && c.NewValue == "high");
        }

        [Fact]
        public void UpdateGap_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, gap) = store.CreateGap(
                sectionId,
                "Original Title",
                "Original Description",
                "medium",
                null,
                null,
                "user-1"
            );

            // Act
            var (isValid, errorMessage, updatedGap) = store.UpdateGap(
                gap!.Id,
                "Updated Title",
                "Updated Description",
                "high",
                "New improvement plan",
                "2024-12-31",
                "user-1",
                "Increased severity due to compliance requirements"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "Gap", entityId: gap.Id);
            
            // Should have create and update entries
            Assert.Equal(2, auditEntries.Count);
            
            var updateEntry = auditEntries.First();
            Assert.Equal("update", updateEntry.Action);
            Assert.Equal("Increased severity due to compliance requirements", updateEntry.ChangeNote);
            Assert.Contains(updateEntry.Changes, c => c.Field == "Title" && c.OldValue == "Original Title" && c.NewValue == "Updated Title");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Impact" && c.OldValue == "medium" && c.NewValue == "high");
        }

        [Fact]
        public void ResolveGap_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, gap) = store.CreateGap(
                sectionId,
                "Test Gap",
                "Description",
                "medium",
                null,
                null,
                "user-1"
            );

            // Act
            var (isValid, errorMessage, resolvedGap) = store.ResolveGap(
                gap!.Id,
                "user-1",
                "Data now available from new system"
            );

            // Assert
            Assert.True(isValid);
            Assert.True(resolvedGap!.Resolved);
            
            var auditEntries = store.GetAuditLog(entityType: "Gap", entityId: gap.Id);
            Assert.Equal(2, auditEntries.Count);
            
            var resolveEntry = auditEntries.First();
            Assert.Equal("resolve", resolveEntry.Action);
            Assert.Equal("Data now available from new system", resolveEntry.ChangeNote);
            Assert.Contains(resolveEntry.Changes, c => c.Field == "Resolved" && c.OldValue == "false" && c.NewValue == "true");
        }

        [Fact]
        public void ReopenGap_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, gap) = store.CreateGap(
                sectionId,
                "Test Gap",
                "Description",
                "medium",
                null,
                null,
                "user-1"
            );

            store.ResolveGap(gap!.Id, "user-1", "Resolved");

            // Act
            var (isValid, errorMessage, reopenedGap) = store.ReopenGap(
                gap.Id,
                "user-1",
                "New information suggests gap still exists"
            );

            // Assert
            Assert.True(isValid);
            Assert.False(reopenedGap!.Resolved);
            
            var auditEntries = store.GetAuditLog(entityType: "Gap", entityId: gap.Id);
            Assert.Equal(3, auditEntries.Count); // create, resolve, reopen
            
            var reopenEntry = auditEntries.First();
            Assert.Equal("reopen", reopenEntry.Action);
            Assert.Equal("New information suggests gap still exists", reopenEntry.ChangeNote);
            Assert.Contains(reopenEntry.Changes, c => c.Field == "Resolved" && c.OldValue == "true" && c.NewValue == "false");
        }

        [Fact]
        public void GetAuditLog_WithEntityTypeFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            // Create various entities
            store.CreateGap(sectionId, "Gap 1", "Desc", "high", null, null, "user-1");
            store.CreateGap(sectionId, "Gap 2", "Desc", "medium", null, null, "user-1");
            store.CreateAssumption(sectionId, "Assumption 1", "Desc", "Company-wide", "2024-01-01", "2024-12-31", "Method", "Limits", new List<string>(), null, new List<AssumptionSource>(), "user-1");

            // Act
            var gapAuditEntries = store.GetAuditLog(entityType: "Gap");
            var assumptionAuditEntries = store.GetAuditLog(entityType: "Assumption");

            // Assert - Only Gaps create audit entries, not Assumptions
            Assert.Equal(2, gapAuditEntries.Count);
            Assert.Empty(assumptionAuditEntries); // CreateAssumption doesn't log
            Assert.All(gapAuditEntries, entry => Assert.Equal("Gap", entry.EntityType));
        }

        [Fact]
        public void SimplificationUpdate_ShouldHaveAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, simplification) = store.CreateSimplification(
                sectionId,
                "Original Title",
                "Original Description",
                new List<string> { "Entity1" },
                new List<string>(),
                new List<string>(),
                "medium",
                null,
                "user-1"
            );

            // Act
            var (isValid, errorMessage, updated) = store.UpdateSimplification(
                simplification!.Id,
                "Updated Title",
                "Updated Description",
                new List<string> { "Entity1", "Entity2" },
                new List<string>(),
                new List<string>(),
                "high",
                "Updated impact notes",
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "simplification", entityId: simplification.Id);
            
            // Should have update entry (create doesn't log for simplifications currently)
            Assert.NotEmpty(auditEntries);
            var updateEntry = auditEntries.First();
            Assert.Equal("update", updateEntry.Action);
            Assert.Contains(updateEntry.Changes, c => c.Field == "Title");
            Assert.Contains(updateEntry.Changes, c => c.Field == "ImpactLevel" && c.OldValue == "medium" && c.NewValue == "high");
        }
    }
}
