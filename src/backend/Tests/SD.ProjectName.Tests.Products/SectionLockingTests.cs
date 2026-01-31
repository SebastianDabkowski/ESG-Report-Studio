using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    /// <summary>
    /// Tests for section content editing states and locking functionality.
    /// </summary>
    public class SectionLockingTests
    {
        private InMemoryReportStore CreateStoreWithTestData()
        {
            var diffService = new TextDiffService();
            var store = new InMemoryReportStore(diffService);

            // Clear existing sample users and add test users
            var usersField = typeof(InMemoryReportStore)
                .GetField("_users", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var users = (List<User>)usersField!.GetValue(store)!;
            users.Clear();
            
            users.Add(new User
            {
                Id = "user-1",
                Name = "Admin User",
                Email = "admin@test.com",
                Role = "admin",
                RoleIds = new List<string> { "role-admin" },
                IsActive = true,
                CanExport = true
            });

            users.Add(new User
            {
                Id = "user-2",
                Name = "Report Owner",
                Email = "owner@test.com",
                Role = "report-owner",
                RoleIds = new List<string> { "role-data-owner" },
                IsActive = true,
                CanExport = true
            });

            users.Add(new User
            {
                Id = "user-3",
                Name = "Contributor",
                Email = "contributor@test.com",
                Role = "contributor",
                RoleIds = new List<string> { "role-contributor" },
                IsActive = true,
                CanExport = false
            });

            users.Add(new User
            {
                Id = "user-4",
                Name = "Reviewer",
                Email = "reviewer@test.com",
                Role = "report-owner",
                RoleIds = new List<string> { "role-data-owner" },
                IsActive = true,
                CanExport = false
            });

            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Organization",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "TEST-ORG-001",
                CreatedBy = "admin-1",
                CoverageType = "full",
                CoverageJustification = "Full organizational coverage for testing"
            });

            // Create organizational unit
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test organizational unit",
                CreatedBy = "admin-1"
            });

            // Create reporting period
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-2",
                OwnerName = "Report Owner"
            });

            Assert.True(isValid, errorMessage);
            Assert.NotNull(snapshot);

            // Create a test section
            var periodId = snapshot!.Periods.First().Id;
            
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Environmental Disclosures",
                Category = "environmental",
                Description = "Environmental section",
                OwnerId = "user-2",
                Status = "draft",
                Order = 1
            };

            // Add section directly to the store's internal collection
            var sectionsField = typeof(InMemoryReportStore)
                .GetField("_sections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = (List<ReportSection>)sectionsField!.GetValue(store)!;
            sections.Add(section);

            return store;
        }

        [Fact]
        public void SubmitSectionForApproval_ValidRequest_SuccessfullyLocksSection()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor",
                SubmissionNote = "Ready for review"
            };

            // Act
            var (isValid, errorMessage, updatedSection) = store.SubmitSectionForApproval(section.Id, request);

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(updatedSection);
            Assert.Equal("submitted-for-approval", updatedSection.Status);
            Assert.NotNull(updatedSection.SubmittedForApprovalAt);
            Assert.Equal("user-3", updatedSection.SubmittedBy);
            Assert.Equal("Contributor", updatedSection.SubmittedByName);
        }

        [Fact]
        public void CanEditSection_WhenSubmittedForApproval_ReturnsFalseWithReason()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            // Act
            var (canEdit, reason) = store.CanEditSection(section.Id);

            // Assert
            Assert.False(canEdit);
            Assert.Contains("submitted for approval", reason);
            Assert.Contains("Contributor", reason);
        }

        [Fact]
        public void ApproveSection_ValidRequest_CreatesVersionAndKeepsLocked()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Submit for approval first
            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            var approveRequest = new ApproveSectionRequest
            {
                ApprovedBy = "user-4",
                ApprovedByName = "Reviewer",
                ApprovalNote = "Looks good"
            };

            // Act
            var (isValid, errorMessage, approvedSection) = store.ApproveSection(section.Id, approveRequest);

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(approvedSection);
            Assert.Equal("approved", approvedSection.Status);
            Assert.NotNull(approvedSection.ApprovedAt);
            Assert.Equal("user-4", approvedSection.ApprovedBy);

            // Verify version was created
            var versions = store.GetSectionVersions(section.Id);
            Assert.Single(versions);
            Assert.Equal(1, versions[0].VersionNumber);
            Assert.Equal("approved", versions[0].Status);
        }

        [Fact]
        public void CanEditSection_WhenApproved_ReturnsFalseWithReason()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Submit and approve
            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            store.ApproveSection(section.Id, new ApproveSectionRequest
            {
                ApprovedBy = "user-4",
                ApprovedByName = "Reviewer"
            });

            // Act
            var (canEdit, reason) = store.CanEditSection(section.Id);

            // Assert
            Assert.False(canEdit);
            Assert.Contains("approved", reason);
            Assert.Contains("new revision", reason);
        }

        [Fact]
        public void RequestSectionChanges_ValidRequest_UnlocksSection()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Submit for approval first
            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            var changesRequest = new RequestSectionChangesRequest
            {
                RequestedBy = "user-4",
                RequestedByName = "Reviewer",
                ChangeNote = "Please add more details"
            };

            // Act
            var (isValid, errorMessage, updatedSection) = store.RequestSectionChanges(section.Id, changesRequest);

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(updatedSection);
            Assert.Equal("changes-requested", updatedSection.Status);
            Assert.Null(updatedSection.SubmittedForApprovalAt);
            Assert.Null(updatedSection.SubmittedBy);

            // Verify section can now be edited
            var (canEdit, _) = store.CanEditSection(section.Id);
            Assert.True(canEdit);
        }

        [Fact]
        public void CreateSectionRevision_FromApprovedSection_CreatesNewDraftVersion()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Submit and approve
            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            store.ApproveSection(section.Id, new ApproveSectionRequest
            {
                ApprovedBy = "user-4",
                ApprovedByName = "Reviewer"
            });

            var revisionRequest = new CreateSectionRevisionRequest
            {
                CreatedBy = "user-3",
                CreatedByName = "Contributor",
                RevisionNote = "Adding new information for next period"
            };

            // Act
            var (isValid, errorMessage, revisedSection) = store.CreateSectionRevision(section.Id, revisionRequest);

            // Assert
            Assert.True(isValid, errorMessage);
            Assert.NotNull(revisedSection);
            Assert.Equal("draft", revisedSection.Status);
            Assert.Equal(2, revisedSection.VersionNumber);
            Assert.Null(revisedSection.SubmittedForApprovalAt);
            Assert.NotNull(revisedSection.ApprovedAt); // Previous approval info retained

            // Verify section can now be edited
            var (canEdit, _) = store.CanEditSection(section.Id);
            Assert.True(canEdit);
        }

        [Fact]
        public void ApproveSection_NotSubmitted_ReturnsError()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var approveRequest = new ApproveSectionRequest
            {
                ApprovedBy = "user-4",
                ApprovedByName = "Reviewer"
            };

            // Act
            var (isValid, errorMessage, _) = store.ApproveSection(section.Id, approveRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("must be submitted for approval", errorMessage);
        }

        [Fact]
        public void RequestChanges_NotSubmitted_ReturnsError()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var changesRequest = new RequestSectionChangesRequest
            {
                RequestedBy = "user-4",
                RequestedByName = "Reviewer",
                ChangeNote = "Changes needed"
            };

            // Act
            var (isValid, errorMessage, _) = store.RequestSectionChanges(section.Id, changesRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Only submitted sections", errorMessage);
        }

        [Fact]
        public void CreateRevision_NotApproved_ReturnsError()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var revisionRequest = new CreateSectionRevisionRequest
            {
                CreatedBy = "user-3",
                CreatedByName = "Contributor"
            };

            // Act
            var (isValid, errorMessage, _) = store.CreateSectionRevision(section.Id, revisionRequest);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Only approved sections", errorMessage);
        }

        [Fact]
        public void SubmitForApproval_AlreadySubmitted_ReturnsError()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            // Act
            var (isValid, errorMessage, _) = store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            // Assert
            Assert.False(isValid);
            Assert.Contains("already submitted", errorMessage);
        }

        [Fact]
        public void SubmitForApproval_AlreadyApproved_ReturnsError()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Submit and approve
            store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            store.ApproveSection(section.Id, new ApproveSectionRequest
            {
                ApprovedBy = "user-4",
                ApprovedByName = "Reviewer"
            });

            // Act
            var (isValid, errorMessage, _) = store.SubmitSectionForApproval(section.Id, new SubmitSectionForApprovalRequest
            {
                SubmittedBy = "user-3",
                SubmittedByName = "Contributor"
            });

            // Assert
            Assert.False(isValid);
            Assert.Contains("already approved", errorMessage);
            Assert.Contains("new revision", errorMessage);
        }
    }
}
