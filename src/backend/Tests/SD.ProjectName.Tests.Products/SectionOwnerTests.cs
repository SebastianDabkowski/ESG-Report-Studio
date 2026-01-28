using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class SectionOwnerTests
    {
        [Fact]
        public void UpdateSectionOwner_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create organization and organizational unit first
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            // Create a reporting period with sections
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2", // admin user
                ChangeNote = "Reassigning section to John Smith"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(section);
            Assert.Equal("user-3", section.OwnerId);
        }
        
        [Fact]
        public void UpdateSectionOwner_ShouldUpdateSectionSummary()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2"
            };
            store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert - Check that summary was updated
            var summaries = store.GetSectionSummaries(null);
            var updatedSummary = summaries.First(s => s.Id == sectionId);
            Assert.Equal("user-3", updatedSummary.OwnerId);
            Assert.Equal("John Smith", updatedSummary.OwnerName);
        }
        
        [Fact]
        public void UpdateSectionOwner_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2",
                ChangeNote = "Section reassignment"
            };
            store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert - Check audit log
            var auditLog = store.GetAuditLog(entityType: "ReportSection", entityId: sectionId);
            Assert.NotEmpty(auditLog);
            
            var entry = auditLog.First();
            Assert.Equal("UpdateSectionOwner", entry.Action);
            Assert.Equal("user-2", entry.UserId);
            Assert.Equal("Admin User", entry.UserName);
            Assert.Equal("Section reassignment", entry.ChangeNote);
            Assert.Single(entry.Changes);
            
            var change = entry.Changes.First();
            Assert.Equal("OwnerId", change.Field);
            Assert.Contains("Sarah Chen", change.OldValue);
            Assert.Contains("user-1", change.OldValue);
            Assert.Contains("John Smith", change.NewValue);
            Assert.Contains("user-3", change.NewValue);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithNonExistentSection_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2"
            };
            
            // Act
            var (isValid, errorMessage, section) = store.UpdateSectionOwner("non-existent-id", updateRequest);
            
            // Assert
            Assert.False(isValid);
            Assert.Equal("Section not found.", errorMessage);
            Assert.Null(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithNonExistentOwner_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "non-existent-user",
                UpdatedBy = "user-2"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.False(isValid);
            Assert.Equal("Owner user not found.", errorMessage);
            Assert.Null(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithNonExistentUpdatingUser_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "non-existent-user"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.False(isValid);
            Assert.Equal("Updating user not found.", errorMessage);
            Assert.Null(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithUnauthorizedUser_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act - user-3 is a contributor, not admin or report-owner
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-4",
                UpdatedBy = "user-3"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.False(isValid);
            Assert.Equal("Only administrators or report owners can change section ownership.", errorMessage);
            Assert.Null(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act - user-2 is an admin
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithReportOwnerUser_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act - user-1 is a report-owner
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-1"
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(section);
        }
        
        [Fact]
        public void UpdateSectionOwner_CompleteWorkflow_ShouldSucceed()
        {
            // Arrange - Complete end-to-end workflow test
            var store = new InMemoryReportStore();
            
            // 1. Setup: Create organization
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            // 2. Setup: Create organizational unit
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            // 3. Create reporting period with sections
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (isValidPeriod, _, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            Assert.True(isValidPeriod);
            Assert.NotNull(snapshot);
            
            var section = snapshot.Sections.First();
            var sectionId = section.Id;
            
            // 4. Verify initial owner
            Assert.Equal("user-1", section.OwnerId);
            var initialSummary = snapshot.SectionSummaries.First(s => s.Id == sectionId);
            Assert.Equal("Sarah Chen", initialSummary.OwnerName);
            
            // 5. Change owner from user-1 to user-3 (by admin user-2)
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2",
                ChangeNote = "Reassigning to John Smith for Q1 reporting"
            };
            var (isValidUpdate, errorMessage, updatedSection) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // 6. Verify update succeeded
            Assert.True(isValidUpdate);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedSection);
            Assert.Equal("user-3", updatedSection.OwnerId);
            
            // 7. Verify section summary was updated
            var updatedSummary = store.GetSectionSummaries(null).First(s => s.Id == sectionId);
            Assert.Equal("user-3", updatedSummary.OwnerId);
            Assert.Equal("John Smith", updatedSummary.OwnerName);
            
            // 8. Verify audit log entry was created
            var auditLog = store.GetAuditLog(entityType: "ReportSection", entityId: sectionId);
            Assert.Single(auditLog);
            
            var auditEntry = auditLog.First();
            Assert.Equal("UpdateSectionOwner", auditEntry.Action);
            Assert.Equal("user-2", auditEntry.UserId);
            Assert.Equal("Admin User", auditEntry.UserName);
            Assert.Equal("Reassigning to John Smith for Q1 reporting", auditEntry.ChangeNote);
            Assert.Single(auditEntry.Changes);
            
            var change = auditEntry.Changes.First();
            Assert.Equal("OwnerId", change.Field);
            Assert.Contains("Sarah Chen", change.OldValue);
            Assert.Contains("user-1", change.OldValue);
            Assert.Contains("John Smith", change.NewValue);
            Assert.Contains("user-3", change.NewValue);
            
            // 9. Change owner again (by report-owner user-1)
            var secondUpdateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-4",
                UpdatedBy = "user-1",
                ChangeNote = "Reassigning to Emily Johnson"
            };
            var (isValidSecondUpdate, _, secondUpdatedSection) = store.UpdateSectionOwner(sectionId, secondUpdateRequest);
            
            // 10. Verify second update
            Assert.True(isValidSecondUpdate);
            Assert.Equal("user-4", secondUpdatedSection!.OwnerId);
            
            // 11. Verify audit log now has 2 entries
            var finalAuditLog = store.GetAuditLog(entityType: "ReportSection", entityId: sectionId);
            Assert.Equal(2, finalAuditLog.Count);
            
            // 12. Verify most recent audit entry
            var mostRecentEntry = finalAuditLog.First(); // Ordered by timestamp descending
            Assert.Equal("user-1", mostRecentEntry.UserId);
            Assert.Equal("Sarah Chen", mostRecentEntry.UserName);
            Assert.Contains("Emily Johnson", mostRecentEntry.Changes.First().NewValue);
        }
        
        [Fact]
        public void UpdateSectionOwner_WithReportOwnerOfDifferentPeriod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            var orgRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-1",
                CoverageType = "full"
            };
            store.CreateOrganization(orgRequest);
            
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test Description",
                CreatedBy = "user-1"
            };
            store.CreateOrganizationalUnit(unitRequest);
            
            // Create first period owned by user-1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(period1Request);
            var sectionId = snapshot!.Sections.First().Id;
            
            // Act - owner-1 (a different report-owner) tries to change ownership of user-1's section
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "owner-1" // Different report owner
            };
            var (isValid, errorMessage, section) = store.UpdateSectionOwner(sectionId, updateRequest);
            
            // Assert
            Assert.False(isValid);
            Assert.Equal("Report owners can only change section ownership for their own reporting periods.", errorMessage);
            Assert.Null(section);
        }
    }
}
