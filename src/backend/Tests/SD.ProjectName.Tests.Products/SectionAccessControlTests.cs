using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    /// <summary>
    /// Tests for granular section-level access control functionality.
    /// </summary>
    public class SectionAccessControlTests
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
                Name = "Contributor One",
                Email = "contributor1@test.com",
                Role = "contributor",
                RoleIds = new List<string> { "role-contributor" },
                IsActive = true,
                CanExport = false
            });

            users.Add(new User
            {
                Id = "user-4",
                Name = "Contributor Two",
                Email = "contributor2@test.com",
                Role = "contributor",
                RoleIds = new List<string> { "role-contributor" },
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

            // Create sections
            var periodId = snapshot!.Periods.First().Id;
            
            var section1 = new ReportSection
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
            
            var section2 = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Social Disclosures",
                Category = "social",
                Description = "Social section",
                OwnerId = "user-2",
                Status = "draft",
                Order = 2
            };

            // Add sections directly to the store's internal collection
            var sectionsField = typeof(InMemoryReportStore)
                .GetField("_sections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = (List<ReportSection>)sectionsField!.GetValue(store)!;
            sections.Add(section1);
            sections.Add(section2);

            return store;
        }

        [Fact]
        public void GrantSectionAccess_ValidRequest_SuccessfullyGrantsAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2",
                Reason = "Contributor needs access to environmental data"
            };

            // Act
            var result = store.GrantSectionAccess(request);

            // Assert
            Assert.Single(result.GrantedAccess);
            Assert.Empty(result.Failures);
            Assert.Equal(section.Id, result.GrantedAccess[0].SectionId);
            Assert.Equal("user-3", result.GrantedAccess[0].UserId);
            Assert.Equal("user-2", result.GrantedAccess[0].GrantedBy);
            Assert.Equal("Contributor needs access to environmental data", result.GrantedAccess[0].Reason);
        }

        [Fact]
        public void GrantSectionAccess_MultipleUsers_GrantsAccessToAll()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3", "user-4" },
                GrantedBy = "user-2",
                Reason = "Multiple contributors need access"
            };

            // Act
            var result = store.GrantSectionAccess(request);

            // Assert
            Assert.Equal(2, result.GrantedAccess.Count);
            Assert.Empty(result.Failures);
        }

        [Fact]
        public void GrantSectionAccess_NonExistentSection_ReturnsFailure()
        {
            // Arrange
            var store = CreateStoreWithTestData();

            var request = new GrantSectionAccessRequest
            {
                SectionId = "non-existent-section",
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };

            // Act
            var result = store.GrantSectionAccess(request);

            // Assert
            Assert.Empty(result.GrantedAccess);
            Assert.Single(result.Failures);
            Assert.Contains("not found", result.Failures[0].Reason);
        }

        [Fact]
        public void GrantSectionAccess_NonExistentUser_ReturnsFailureForThatUser()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3", "non-existent-user" },
                GrantedBy = "user-2"
            };

            // Act
            var result = store.GrantSectionAccess(request);

            // Assert
            Assert.Single(result.GrantedAccess);
            Assert.Single(result.Failures);
            Assert.Equal("non-existent-user", result.Failures[0].UserId);
            Assert.Equal("User not found", result.Failures[0].Reason);
        }

        [Fact]
        public void GrantSectionAccess_DuplicateGrant_ReturnsFailure()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };

            // First grant
            store.GrantSectionAccess(request);

            // Act - Try to grant again
            var result = store.GrantSectionAccess(request);

            // Assert
            Assert.Empty(result.GrantedAccess);
            Assert.Single(result.Failures);
            Assert.Contains("already has access", result.Failures[0].Reason);
        }

        [Fact]
        public void RevokeSectionAccess_ExistingGrant_SuccessfullyRevokes()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Grant access first
            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            var revokeRequest = new RevokeSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                RevokedBy = "user-2",
                Reason = "Access no longer needed"
            };

            // Act
            var result = store.RevokeSectionAccess(revokeRequest);

            // Assert
            Assert.Single(result.RevokedUserIds);
            Assert.Empty(result.Failures);
            Assert.Equal("user-3", result.RevokedUserIds[0]);
        }

        [Fact]
        public void RevokeSectionAccess_NonExistentGrant_ReturnsFailure()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new RevokeSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                RevokedBy = "user-2"
            };

            // Act
            var result = store.RevokeSectionAccess(request);

            // Assert
            Assert.Empty(result.RevokedUserIds);
            Assert.Single(result.Failures);
            Assert.Contains("does not have explicit access", result.Failures[0].Reason);
        }

        [Fact]
        public void HasSectionAccess_SectionOwner_ReturnsTrue()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Act
            var hasAccess = store.HasSectionAccess("user-2", section.Id);

            // Assert
            Assert.True(hasAccess);
        }

        [Fact]
        public void HasSectionAccess_AdminUser_ReturnsTrue()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Act
            var hasAccess = store.HasSectionAccess("user-1", section.Id);

            // Assert
            Assert.True(hasAccess);
        }

        [Fact]
        public void HasSectionAccess_UserWithExplicitGrant_ReturnsTrue()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            // Act
            var hasAccess = store.HasSectionAccess("user-3", section.Id);

            // Assert
            Assert.True(hasAccess);
        }

        [Fact]
        public void HasSectionAccess_UserWithoutAccess_ReturnsFalse()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Act
            var hasAccess = store.HasSectionAccess("user-4", section.Id);

            // Assert
            Assert.False(hasAccess);
        }

        [Fact]
        public void GetUserSectionAccess_ReturnsOnlyExplicitGrants()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section1 = sections[0];
            var section2 = sections[1];

            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section1.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section2.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            // Act
            var grants = store.GetUserSectionAccess("user-3");

            // Assert
            Assert.Equal(2, grants.Count);
            Assert.All(grants, g => Assert.Equal("user-3", g.UserId));
        }

        [Fact]
        public void GetSectionAccessSummary_IncludesOwnerAndGrants()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3", "user-4" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            // Act
            var summary = store.GetSectionAccessSummary(section.Id);

            // Assert
            Assert.Equal(section.Id, summary.SectionId);
            Assert.Equal(section.Title, summary.SectionTitle);
            Assert.NotNull(summary.Owner);
            Assert.Equal("user-2", summary.Owner!.Id);
            Assert.Equal(2, summary.AccessGrants.Count);
        }

        [Fact]
        public void GetAccessibleSections_AdminSeesAllSections()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var allSections = store.GetSections(null);

            // Act
            var accessibleSections = store.GetAccessibleSections("user-1", null);

            // Assert
            Assert.Equal(allSections.Count, accessibleSections.Count);
        }

        [Fact]
        public void GetAccessibleSections_UserSeesOwnedAndGrantedSections()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            // Act
            var accessibleSections = store.GetAccessibleSections("user-3", null);

            // Assert
            Assert.Single(accessibleSections);
            Assert.Equal(section.Id, accessibleSections[0].Id);
        }

        [Fact]
        public void GetAccessibleSections_UserWithoutAccess_ReturnsEmpty()
        {
            // Arrange
            var store = CreateStoreWithTestData();

            // Act
            var accessibleSections = store.GetAccessibleSections("user-4", null);

            // Assert
            Assert.Empty(accessibleSections);
        }

        [Fact]
        public void GetAccessibleSectionSummaries_FiltersCorrectly()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            // Act
            var accessibleSummaries = store.GetAccessibleSectionSummaries("user-3", null);

            // Assert
            Assert.Single(accessibleSummaries);
            Assert.Equal(section.Id, accessibleSummaries[0].Id);
        }

        [Fact]
        public void GrantSectionAccess_CreatesAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            var request = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2",
                Reason = "Testing audit trail"
            };

            // Act
            store.GrantSectionAccess(request);

            // Get audit log
            var auditEntries = store.GetAuditLog(entityType: "SectionAccessGrant");

            // Assert
            Assert.NotEmpty(auditEntries);
            var grantEntry = auditEntries.First(e => e.Action == "grant-section-access");
            Assert.Equal("user-2", grantEntry.UserId);
            Assert.Contains("Testing audit trail", grantEntry.ChangeNote ?? "");
        }

        [Fact]
        public void RevokeSectionAccess_CreatesAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var sections = store.GetSections(null);
            var section = sections.First();

            // Grant access first
            var grantRequest = new GrantSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                GrantedBy = "user-2"
            };
            store.GrantSectionAccess(grantRequest);

            var revokeRequest = new RevokeSectionAccessRequest
            {
                SectionId = section.Id,
                UserIds = new List<string> { "user-3" },
                RevokedBy = "user-2",
                Reason = "Testing revoke audit"
            };

            // Act
            store.RevokeSectionAccess(revokeRequest);

            // Get audit log
            var auditEntries = store.GetAuditLog(entityType: "SectionAccessGrant");

            // Assert
            var revokeEntry = auditEntries.FirstOrDefault(e => e.Action == "revoke-section-access");
            Assert.NotNull(revokeEntry);
            Assert.Equal("user-2", revokeEntry!.UserId);
            Assert.Contains("Testing revoke audit", revokeEntry.ChangeNote ?? "");
        }
    }
}
