using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class ResponsibilityMatrixTests
    {
        [Fact]
        public void GetResponsibilityMatrix_WithNoFilter_ShouldReturnAllSections()
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
            
            // Act
            var matrix = store.GetResponsibilityMatrix(snapshot!.Periods.First().Id);
            
            // Assert
            Assert.NotNull(matrix);
            Assert.Equal(6, matrix.TotalSections); // simplified mode has 6 sections
            Assert.True(matrix.Assignments.Count > 0);
        }
        
        [Fact]
        public void GetResponsibilityMatrix_WithUnassignedFilter_ShouldReturnOnlyUnassignedSections()
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
            
            // Update one section to have an owner
            var firstSection = snapshot!.Sections.First();
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-1",
                ChangeNote = "Assigning section"
            };
            store.UpdateSectionOwner(firstSection.Id, updateRequest);
            
            // Clear the owner of another section to make it unassigned
            var secondSection = snapshot.Sections.Skip(1).First();
            var clearRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = string.Empty,
                UpdatedBy = "user-1",
                ChangeNote = "Clearing owner"
            };
            store.UpdateSectionOwner(secondSection.Id, clearRequest);
            
            // Act
            var matrix = store.GetResponsibilityMatrix(snapshot.Periods.First().Id, "unassigned");
            
            // Assert
            Assert.NotNull(matrix);
            Assert.True(matrix.TotalSections > 0);
            Assert.All(matrix.Assignments, assignment => 
                Assert.True(string.IsNullOrEmpty(assignment.OwnerId), "All returned assignments should be unassigned"));
        }
        
        [Fact]
        public void GetResponsibilityMatrix_WithOwnerFilter_ShouldReturnOnlyAssignedToOwner()
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
            
            // Assign sections to different owners
            var sections = snapshot!.Sections.ToList();
            var updateRequest1 = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-1",
                ChangeNote = "Assigning to user-3"
            };
            store.UpdateSectionOwner(sections[0].Id, updateRequest1);
            
            var updateRequest2 = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-4",
                UpdatedBy = "user-1",
                ChangeNote = "Assigning to user-4"
            };
            store.UpdateSectionOwner(sections[1].Id, updateRequest2);
            
            // Act - filter by user-3
            var matrix = store.GetResponsibilityMatrix(snapshot.Periods.First().Id, "user-3");
            
            // Assert
            Assert.NotNull(matrix);
            Assert.Equal(1, matrix.TotalSections);
            Assert.Single(matrix.Assignments);
            Assert.Equal("user-3", matrix.Assignments[0].OwnerId);
        }
        
        [Fact]
        public void GetResponsibilityMatrix_ShouldGroupSectionsByOwner()
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
            
            // Assign multiple sections to same owner
            var sections = snapshot!.Sections.ToList();
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-1",
                ChangeNote = "Bulk assign to user-3"
            };
            store.UpdateSectionOwner(sections[0].Id, updateRequest);
            store.UpdateSectionOwner(sections[1].Id, updateRequest);
            
            // Act
            var matrix = store.GetResponsibilityMatrix(snapshot.Periods.First().Id);
            
            // Assert
            var user3Assignment = matrix.Assignments.FirstOrDefault(a => a.OwnerId == "user-3");
            Assert.NotNull(user3Assignment);
            Assert.Equal(2, user3Assignment.Sections.Count);
            Assert.Equal("John Smith", user3Assignment.OwnerName);
        }
        
        [Fact]
        public void GetResponsibilityMatrix_ShouldCountUnassignedSections()
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
            
            // Leave all sections with default owner (user-1 from period)
            // Then clear some owners to create unassigned sections
            var sections = snapshot!.Sections.ToList();
            var clearRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = string.Empty,
                UpdatedBy = "user-1",
                ChangeNote = "Clearing owner"
            };
            store.UpdateSectionOwner(sections[0].Id, clearRequest);
            store.UpdateSectionOwner(sections[1].Id, clearRequest);
            
            // Act
            var matrix = store.GetResponsibilityMatrix(snapshot.Periods.First().Id);
            
            // Assert
            Assert.Equal(2, matrix.UnassignedSections);
        }
        
        [Fact]
        public void GetResponsibilityMatrix_WithNullPeriodId_ShouldReturnAllPeriods()
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
            
            // Create first period
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
            var (_, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            
            // Create second period
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "2025 Report",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            };
            var (_, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);
            
            // Act - get matrix for all periods (null periodId)
            var matrix = store.GetResponsibilityMatrix(null);
            
            // Assert
            Assert.Equal(12, matrix.TotalSections); // 6 sections per period * 2 periods
            Assert.Null(matrix.PeriodId);
        }
    }
}
