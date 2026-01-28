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
    }
}
