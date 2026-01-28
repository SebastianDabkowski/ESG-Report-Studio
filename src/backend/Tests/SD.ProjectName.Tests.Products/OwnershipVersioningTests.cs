using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class OwnershipVersioningTests
    {
        [Fact]
        public void CreatePeriod_ShouldStoreCatalogCode()
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
            
            // Act
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
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.NotEmpty(snapshot.Sections);
            
            // All sections should have catalog codes
            foreach (var section in snapshot.Sections)
            {
                Assert.NotNull(section.CatalogCode);
                Assert.NotEmpty(section.CatalogCode);
                // Verify expected format (e.g., ENV-001, SOC-002, etc.)
                Assert.Matches(@"^(ENV|SOC|GOV)-\d{3}$", section.CatalogCode);
            }
        }
        
        [Fact]
        public void CreatePeriodFromExisting_ShouldCopyOwnership_WhenCodesMatch()
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
            
            // Create first period with default owner
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
            var period1Id = snapshot1!.Periods.First(p => p.Name == "2024 Report").Id;
            
            // Update ownership of first section to different owner
            var firstSection = snapshot1.Sections.First();
            var updateOwnerRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2",
                ChangeNote = "Reassigning to John Smith"
            };
            store.UpdateSectionOwner(firstSection.Id, updateOwnerRequest);
            
            // Act - Create second period copying ownership from first
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "2025 Report",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen",
                CopyOwnershipFromPeriodId = period1Id
            };
            var (isValid, errorMessage, snapshot2) = store.ValidateAndCreatePeriod(period2Request);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot2);
            
            // Find matching sections by catalog code
            var section1 = snapshot1.Sections.First();
            var section2 = snapshot2!.Sections.First(s => s.CatalogCode == section1.CatalogCode);
            
            // Ownership should be copied
            Assert.Equal(section1.OwnerId, section2.OwnerId);
            Assert.Equal("user-3", section2.OwnerId);
        }
        
        [Fact]
        public void CreatePeriodFromExisting_ShouldMarkNewSectionsAsUnassigned()
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
            
            // Create first period in simplified mode (6 sections)
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
            var period1Id = snapshot1!.Periods.First(p => p.Name == "2024 Report").Id;
            
            // Act - Create second period in extended mode (13 sections)
            // This will create new sections not present in the first period
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "2025 Report",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "extended",
                ReportScope = "single-company",
                OwnerId = "user-2",
                OwnerName = "Admin User",
                CopyOwnershipFromPeriodId = period1Id
            };
            var (isValid, errorMessage, snapshot2) = store.ValidateAndCreatePeriod(period2Request);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot2);
            
            // Period 2 should have more sections (extended mode)
            Assert.True(snapshot2!.Sections.Count > snapshot1.Sections.Count);
            
            // Get catalog codes from period 1
            var period1Codes = snapshot1.Sections.Select(s => s.CatalogCode).ToHashSet();
            
            // Find sections in period 2 that are new (not in period 1)
            var newSections = snapshot2.Sections.Where(s => !period1Codes.Contains(s.CatalogCode)).ToList();
            
            // New sections should be unassigned (empty OwnerId)
            Assert.NotEmpty(newSections);
            foreach (var newSection in newSections)
            {
                Assert.Equal(string.Empty, newSection.OwnerId); // unassigned
            }
            
            // Corresponding summaries should also show as unassigned
            foreach (var newSection in newSections)
            {
                var summary = snapshot2.SectionSummaries.First(s => s.Id == newSection.Id);
                Assert.Equal(string.Empty, summary.OwnerId);
                Assert.Equal("Unassigned", summary.OwnerName);
            }
            
            // Existing sections should have copied ownership from period 1
            var existingSections = snapshot2.Sections.Where(s => period1Codes.Contains(s.CatalogCode)).ToList();
            foreach (var existingSection in existingSections)
            {
                // These should have ownership from period 1 (user-1 was the owner there)
                Assert.Equal("user-1", existingSection.OwnerId);
            }
        }
        
        [Fact]
        public void CreatePeriodFromExisting_ShouldUpdateSummaryOwnerName()
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
            var period1Id = snapshot1!.Periods.First(p => p.Name == "2024 Report").Id;
            
            // Update ownership of first section to user-3
            var firstSection = snapshot1.Sections.First();
            var updateOwnerRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2",
                ChangeNote = "Reassigning to John Smith"
            };
            store.UpdateSectionOwner(firstSection.Id, updateOwnerRequest);
            
            // Act - Create second period copying ownership
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "2025 Report",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen",
                CopyOwnershipFromPeriodId = period1Id
            };
            var (isValid, errorMessage, snapshot2) = store.ValidateAndCreatePeriod(period2Request);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot2);
            
            // Find the matching summary
            var summary2 = snapshot2!.SectionSummaries.First(s => s.CatalogCode == firstSection.CatalogCode);
            
            // Verify both OwnerId and OwnerName are updated
            Assert.Equal("user-3", summary2.OwnerId);
            Assert.Equal("John Smith", summary2.OwnerName);
        }
        
        [Fact]
        public void CreatePeriod_WithoutCopyOwnership_ShouldUsePeriodOwner()
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
            
            // Act - Create period without copying ownership
            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "2024 Report",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-3",
                OwnerName = "John Smith"
            };
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(periodRequest);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            
            // All sections should have the period owner
            foreach (var section in snapshot!.Sections)
            {
                Assert.Equal("user-3", section.OwnerId);
            }
            
            foreach (var summary in snapshot.SectionSummaries)
            {
                Assert.Equal("user-3", summary.OwnerId);
                Assert.Equal("John Smith", summary.OwnerName);
            }
        }
    }
}
