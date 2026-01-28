using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class RemediationAuditTests
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
        public void CreateRemediationPlan_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            // Act
            var (isValid, errorMessage, plan) = store.CreateRemediationPlan(
                sectionId,
                "Test Remediation Plan",
                "Plan description",
                "Q2 2024",
                "user-1",
                "John Doe",
                "high",
                null,
                null,
                null,
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            Assert.NotNull(plan);
            
            var auditEntries = store.GetAuditLog(entityType: "RemediationPlan", entityId: plan.Id);
            Assert.Single(auditEntries);
            
            var createEntry = auditEntries.First();
            Assert.Equal("create", createEntry.Action);
            Assert.Equal("user-1", createEntry.UserId);
            Assert.Equal("RemediationPlan", createEntry.EntityType);
            Assert.Contains(createEntry.Changes, c => c.Field == "Title" && c.NewValue == "Test Remediation Plan");
            Assert.Contains(createEntry.Changes, c => c.Field == "Priority" && c.NewValue == "high");
        }

        [Fact]
        public void UpdateRemediationPlan_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, plan) = store.CreateRemediationPlan(
                sectionId,
                "Original Plan",
                "Original Description",
                "Q1 2024",
                "user-1",
                "John Doe",
                "medium",
                null,
                null,
                null,
                "user-1"
            );

            // Act
            var (isValid, errorMessage, updated) = store.UpdateRemediationPlan(
                plan!.Id,
                "Updated Plan",
                "Updated Description",
                "Q2 2024",
                "user-2",
                "Jane Smith",
                "high",
                "in-progress",
                "user-2"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "RemediationPlan", entityId: plan.Id);
            
            Assert.Equal(2, auditEntries.Count); // create + update
            
            var updateEntry = auditEntries.First();
            Assert.Equal("update", updateEntry.Action);
            Assert.Equal("user-2", updateEntry.UserId);
            Assert.Contains(updateEntry.Changes, c => c.Field == "Title" && c.OldValue == "Original Plan" && c.NewValue == "Updated Plan");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Priority" && c.OldValue == "medium" && c.NewValue == "high");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Status" && c.OldValue == "planned" && c.NewValue == "in-progress");
        }

        [Fact]
        public void DeleteRemediationPlan_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, plan) = store.CreateRemediationPlan(
                sectionId,
                "Plan to Delete",
                "Description",
                "Q1 2024",
                "user-1",
                "John Doe",
                "low",
                null,
                null,
                null,
                "user-1"
            );

            // Act
            var deleted = store.DeleteRemediationPlan(plan!.Id, "user-2");

            // Assert
            Assert.True(deleted);
            var auditEntries = store.GetAuditLog(entityType: "RemediationPlan", entityId: plan.Id);
            
            Assert.Equal(2, auditEntries.Count); // create + delete
            
            var deleteEntry = auditEntries.First();
            Assert.Equal("delete", deleteEntry.Action);
            Assert.Equal("user-2", deleteEntry.UserId);
            Assert.Contains(deleteEntry.Changes, c => c.Field == "Title" && c.OldValue == "Plan to Delete");
        }

        [Fact]
        public void CreateRemediationAction_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, plan) = store.CreateRemediationPlan(
                sectionId,
                "Test Plan",
                "Description",
                "Q1 2024",
                "user-1",
                "John Doe",
                "medium",
                null,
                null,
                null,
                "user-1"
            );

            // Act
            var (isValid, errorMessage, action) = store.CreateRemediationAction(
                plan!.Id,
                "Test Action",
                "Action description",
                "user-1",
                "John Doe",
                "2024-06-30",
                "user-1"
            );

            // Assert
            Assert.True(isValid);
            Assert.NotNull(action);
            
            var auditEntries = store.GetAuditLog(entityType: "RemediationAction", entityId: action.Id);
            Assert.Single(auditEntries);
            
            var createEntry = auditEntries.First();
            Assert.Equal("create", createEntry.Action);
            Assert.Equal("user-1", createEntry.UserId);
            Assert.Equal("RemediationAction", createEntry.EntityType);
            Assert.Contains(createEntry.Changes, c => c.Field == "Title" && c.NewValue == "Test Action");
            Assert.Contains(createEntry.Changes, c => c.Field == "DueDate" && c.NewValue == "2024-06-30");
        }

        [Fact]
        public void UpdateRemediationAction_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, plan) = store.CreateRemediationPlan(
                sectionId,
                "Test Plan",
                "Description",
                "Q1 2024",
                "user-1",
                "John Doe",
                "medium",
                null,
                null,
                null,
                "user-1"
            );

            var (_, _, action) = store.CreateRemediationAction(
                plan!.Id,
                "Original Action",
                "Original Description",
                "user-1",
                "John Doe",
                "2024-06-30",
                "user-1"
            );

            // Act
            var (isValid, errorMessage, updated) = store.UpdateRemediationAction(
                action!.Id,
                "Updated Action",
                "Updated Description",
                "user-2",
                "Jane Smith",
                "2024-07-31",
                "completed",
                "user-2"
            );

            // Assert
            Assert.True(isValid);
            var auditEntries = store.GetAuditLog(entityType: "RemediationAction", entityId: action.Id);
            
            Assert.Equal(2, auditEntries.Count); // create + update
            
            var updateEntry = auditEntries.First();
            Assert.Equal("update", updateEntry.Action);
            Assert.Equal("user-2", updateEntry.UserId);
            Assert.Contains(updateEntry.Changes, c => c.Field == "Title" && c.OldValue == "Original Action" && c.NewValue == "Updated Action");
            Assert.Contains(updateEntry.Changes, c => c.Field == "DueDate" && c.OldValue == "2024-06-30" && c.NewValue == "2024-07-31");
            Assert.Contains(updateEntry.Changes, c => c.Field == "Status" && c.OldValue == "pending" && c.NewValue == "completed");
        }

        [Fact]
        public void DeleteRemediationAction_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var snapshot = store.GetSnapshot();
            var sectionId = snapshot.Sections.First().Id;

            var (_, _, plan) = store.CreateRemediationPlan(
                sectionId,
                "Test Plan",
                "Description",
                "Q1 2024",
                "user-1",
                "John Doe",
                "medium",
                null,
                null,
                null,
                "user-1"
            );

            var (_, _, action) = store.CreateRemediationAction(
                plan!.Id,
                "Action to Delete",
                "Description",
                "user-1",
                "John Doe",
                "2024-06-30",
                "user-1"
            );

            // Act
            var deleted = store.DeleteRemediationAction(action!.Id, "user-2");

            // Assert
            Assert.True(deleted);
            var auditEntries = store.GetAuditLog(entityType: "RemediationAction", entityId: action.Id);
            
            Assert.Equal(2, auditEntries.Count); // create + delete
            
            var deleteEntry = auditEntries.First();
            Assert.Equal("delete", deleteEntry.Action);
            Assert.Equal("user-2", deleteEntry.UserId);
            Assert.Contains(deleteEntry.Changes, c => c.Field == "Title" && c.OldValue == "Action to Delete");
        }
    }
}
