using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for tamper-evident audit log functionality including hash chains and integrity verification.
/// Validates that access and permission changes are audited with cryptographic evidence.
/// </summary>
public sealed class AuditLogTamperEvidenceTests
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
            Id = "admin-1",
            Name = "Admin User",
            Email = "admin@test.com",
            Role = "admin",
            RoleIds = new List<string> { "role-admin" },
            IsActive = true,
            CanExport = true
        });
        
        users.Add(new User
        {
            Id = "auditor-1",
            Name = "Auditor User",
            Email = "auditor@test.com",
            Role = "auditor",
            RoleIds = new List<string> { "role-auditor" },
            IsActive = true,
            CanExport = true
        });
        
        users.Add(new User
        {
            Id = "user-1",
            Name = "Regular User",
            Email = "user@test.com",
            Role = "contributor",
            RoleIds = new List<string> { "role-contributor" },
            IsActive = true,
            CanExport = false
        });
        
        return store;
    }
    
    [Fact]
    public void AuditLogEntry_ShouldHaveHashFields()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Act - Perform a role assignment that creates an audit entry
        var request = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { "role-admin" }
        };
        store.AssignUserRoles("user-1", request, "admin-1", "Admin User");
        
        // Assert - Get the audit log and verify hash fields exist
        var auditEntries = store.GetAuditLog(entityType: "User", entityId: "user-1");
        Assert.NotEmpty(auditEntries);
        
        var latestEntry = auditEntries.First();
        Assert.NotNull(latestEntry.EntryHash);
        Assert.NotEmpty(latestEntry.EntryHash);
        
        // First entry in the log should have null PreviousEntryHash
        // (unless there were other entries before this test)
    }
    
    [Fact]
    public void AuditLogEntry_HashChain_ShouldLinkEntries()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Act - Create multiple audit entries
        var request1 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-contributor" } };
        store.AssignUserRoles("user-1", request1, "admin-1", "Admin User");
        
        var request2 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", request2, "admin-1", "Admin User");
        
        // Assert - Get all audit entries and verify chain
        var allEntries = store.GetAuditLog();
        Assert.True(allEntries.Count >= 2, "Should have at least 2 audit entries");
        
        // Get entries in chronological order
        var chronological = allEntries.OrderBy(e => e.Timestamp).ToList();
        
        // Verify each entry (except first) has a previous hash
        for (int i = 1; i < chronological.Count; i++)
        {
            var currentEntry = chronological[i];
            var previousEntry = chronological[i - 1];
            
            Assert.NotNull(currentEntry.PreviousEntryHash);
            Assert.Equal(previousEntry.EntryHash, currentEntry.PreviousEntryHash);
        }
    }
    
    [Fact]
    public void RoleAssignment_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Act
        var request = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { "role-admin", "role-contributor" }
        };
        store.AssignUserRoles("user-1", request, "admin-1", "Admin User");
        
        // Assert
        var entries = store.GetAuditLog(action: "assign-user-roles", userId: "admin-1");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("assign-user-roles", entry.Action);
        Assert.Equal("User", entry.EntityType);
        Assert.Equal("user-1", entry.EntityId);
        Assert.Equal("admin-1", entry.UserId);
        Assert.NotNull(entry.EntryHash);
        Assert.NotEmpty(entry.EntryHash);
    }
    
    [Fact]
    public void RoleRemoval_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // First assign roles
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { "role-admin", "role-contributor" }
        };
        store.AssignUserRoles("user-1", assignRequest, "admin-1", "Admin User");
        
        // Act - Remove a role
        store.RemoveUserRole("user-1", "role-contributor", "admin-1", "Admin User");
        
        // Assert
        var entries = store.GetAuditLog(action: "remove-user-role");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("remove-user-role", entry.Action);
        Assert.NotNull(entry.EntryHash);
        Assert.NotEmpty(entry.EntryHash);
    }
    
    [Fact]
    public void RoleCreation_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Act
        var request = new CreateRoleRequest
        {
            Name = "Custom Role",
            Description = "A custom role for testing",
            Permissions = new List<string> { "view", "edit" }
        };
        var (success, _, role) = store.CreateRole(request, "admin-1", "Admin User");
        
        // Assert
        Assert.True(success);
        var entries = store.GetAuditLog(action: "create-role");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("create-role", entry.Action);
        Assert.Equal("SystemRole", entry.EntityType);
        Assert.NotNull(entry.EntryHash);
    }
    
    [Fact]
    public void RoleUpdate_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Act
        var request = new UpdateRoleRequest
        {
            Description = "Updated description for admin role"
        };
        store.UpdateRoleDescription(adminRole.Id, request, "admin-1", "Admin User");
        
        // Assert
        var entries = store.GetAuditLog(action: "update-role-description");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.NotNull(entry.EntryHash);
        Assert.Contains(entry.Changes, c => c.Field == "Description");
    }
    
    [Fact]
    public void RoleDeletion_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        var createRequest = new CreateRoleRequest
        {
            Name = "Temporary Role",
            Description = "A role to be deleted",
            Permissions = new List<string> { "view" }
        };
        var (_, _, role) = store.CreateRole(createRequest, "admin-1", "Admin User");
        
        // Act
        store.DeleteRole(role!.Id, "admin-1", "Admin User");
        
        // Assert
        var entries = store.GetAuditLog(action: "delete-role");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("delete-role", entry.Action);
        Assert.NotNull(entry.EntryHash);
    }
    
    [Fact]
    public void TamperEvidentExport_ReturnsValidResponse()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create some audit entries
        var request = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", request, "admin-1", "Admin User");
        
        // Act
        var export = store.GenerateTamperEvidentExport(
            "auditor-1",
            "Auditor User",
            action: "assign-user-roles"
        );
        
        // Assert
        Assert.NotNull(export);
        Assert.NotNull(export.Metadata);
        Assert.NotNull(export.ContentHash);
        Assert.NotEmpty(export.ContentHash);
        Assert.NotNull(export.Signature);
        Assert.NotEmpty(export.Entries);
    }
    
    [Fact]
    public void TamperEvidentExport_IncludesMetadata()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Act
        var export = store.GenerateTamperEvidentExport(
            "auditor-1",
            "Auditor User",
            entityType: "User"
        );
        
        // Assert
        var metadata = export.Metadata;
        Assert.NotNull(metadata.ExportedAt);
        Assert.Equal("auditor-1", metadata.ExportedBy);
        Assert.Equal("Auditor User", metadata.ExportedByName);
        Assert.Equal("SHA-256", metadata.HashAlgorithm);
        Assert.Equal("1.0", metadata.FormatVersion);
        Assert.Contains("entityType", metadata.Filters.Keys);
        Assert.Equal("User", metadata.Filters["entityType"]);
    }
    
    [Fact]
    public void TamperEvidentExport_VerifiesHashChain()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create multiple entries to form a chain
        var request1 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-contributor" } };
        store.AssignUserRoles("user-1", request1, "admin-1", "Admin User");
        
        var request2 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", request2, "admin-1", "Admin User");
        
        // Act
        var export = store.GenerateTamperEvidentExport("auditor-1", "Auditor User");
        
        // Assert
        Assert.True(export.Metadata.HashChainValid);
        Assert.Equal("Hash chain verified successfully", export.Metadata.ValidationMessage);
    }
    
    [Fact]
    public void TamperEvidentExport_EntriesInChronologicalOrder()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create entries - they will naturally have sequential timestamps
        var request1 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-contributor" } };
        store.AssignUserRoles("user-1", request1, "admin-1", "Admin User");
        
        var request2 = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", request2, "admin-1", "Admin User");
        
        // Act
        var export = store.GenerateTamperEvidentExport("auditor-1", "Auditor User");
        
        // Assert - Entries should be in chronological order (oldest first)
        for (int i = 1; i < export.Entries.Count; i++)
        {
            if (DateTime.TryParse(export.Entries[i].Timestamp, out var current) &&
                DateTime.TryParse(export.Entries[i - 1].Timestamp, out var previous))
            {
                Assert.True(current >= previous, "Entries should be in chronological order");
            }
            else
            {
                Assert.Fail("Failed to parse entry timestamps");
            }
        }
    }
    
    [Fact]
    public void TamperEvidentExport_AppliesFilters()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create different types of audit entries
        var assignRequest = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", assignRequest, "admin-1", "Admin User");
        
        var createRequest = new CreateRoleRequest
        {
            Name = "Test Role",
            Description = "Test",
            Permissions = new List<string> { "view" }
        };
        store.CreateRole(createRequest, "admin-1", "Admin User");
        
        // Act - Filter by entity type
        var export = store.GenerateTamperEvidentExport(
            "auditor-1",
            "Auditor User",
            entityType: "User"
        );
        
        // Assert - Should only include User entity entries
        Assert.All(export.Entries, entry =>
        {
            Assert.Equal("User", entry.EntityType);
        });
    }
    
    [Fact]
    public void TamperEvidentExport_ContentHashIsConsistent()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        var request = new AssignUserRolesRequest { RoleIds = new List<string> { "role-admin" } };
        store.AssignUserRoles("user-1", request, "admin-1", "Admin User");
        
        // Act - Generate same export twice
        var export1 = store.GenerateTamperEvidentExport(
            "auditor-1",
            "Auditor User",
            entityType: "User"
        );
        
        var export2 = store.GenerateTamperEvidentExport(
            "auditor-1",
            "Auditor User",
            entityType: "User"
        );
        
        // Assert - Content hash should be different because exportedAt timestamp differs
        // But the entry hashes should be identical
        Assert.NotEqual(export1.ContentHash, export2.ContentHash); // Different due to timestamp
        
        // Entry hashes should be the same
        for (int i = 0; i < export1.Entries.Count; i++)
        {
            Assert.Equal(export1.Entries[i].EntryHash, export2.Entries[i].EntryHash);
        }
    }
    
    [Fact]
    public void SectionAccessGrant_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create organization first
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Org",
            LegalForm = "LLC",
            Country = "US",
            Identifier = "TEST-001",
            CreatedBy = "admin-1",
            CoverageType = "full",
            CoverageJustification = "Full coverage"
        });
        
        // Create organizational unit
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Test organizational unit",
            CreatedBy = "admin-1"
        });
        
        var (isValid, errorMsg, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "admin-1",
            OwnerName = "Admin User"
        });
        Assert.True(isValid, $"Period creation failed: {errorMsg}");
        
        var periodId = snapshot!.Periods.First().Id;
        
        // Create a section
        var sectionsField = typeof(InMemoryReportStore)
            .GetField("_sections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sections = (List<ReportSection>)sectionsField!.GetValue(store)!;
        var section = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = periodId,
            Title = "Test Section",
            Category = "environmental",
            Description = "Test",
            OwnerId = "admin-1",
            Status = "draft",
            Order = 1
        };
        sections.Add(section);
        
        // Act - Grant section access
        var grantRequest = new GrantSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { "user-1" },
            GrantedBy = "admin-1",
            Reason = "Testing audit trail"
        };
        store.GrantSectionAccess(grantRequest);
        
        // Assert
        var entries = store.GetAuditLog(action: "grant-section-access");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("grant-section-access", entry.Action);
        Assert.Equal("SectionAccessGrant", entry.EntityType);
        Assert.NotNull(entry.EntryHash);
        Assert.NotEmpty(entry.EntryHash);
    }
    
    [Fact]
    public void SectionAccessRevoke_CreatesAuditEntryWithHash()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        
        // Create organization
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Org",
            LegalForm = "LLC",
            Country = "US",
            Identifier = "TEST-001",
            CreatedBy = "admin-1",
            CoverageType = "full",
            CoverageJustification = "Full coverage"
        });
        
        // Create organizational unit
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Test organizational unit",
            CreatedBy = "admin-1"
        });
        
        var (isValid, errorMsg, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "admin-1",
            OwnerName = "Admin User"
        });
        Assert.True(isValid, $"Period creation failed: {errorMsg}");
        
        var periodId = snapshot!.Periods.First().Id;
        
        var sectionsField = typeof(InMemoryReportStore)
            .GetField("_sections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sections = (List<ReportSection>)sectionsField!.GetValue(store)!;
        var section = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = periodId,
            Title = "Test Section",
            Category = "environmental",
            Description = "Test",
            OwnerId = "admin-1",
            Status = "draft",
            Order = 1
        };
        sections.Add(section);
        
        // First grant access
        var grantRequest = new GrantSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { "user-1" },
            GrantedBy = "admin-1",
            Reason = "Initial grant"
        };
        store.GrantSectionAccess(grantRequest);
        
        // Act - Revoke access
        var revokeRequest = new RevokeSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { "user-1" },
            RevokedBy = "admin-1",
            Reason = "Access no longer needed"
        };
        store.RevokeSectionAccess(revokeRequest);
        
        // Assert
        var entries = store.GetAuditLog(action: "revoke-section-access");
        Assert.NotEmpty(entries);
        
        var entry = entries.First();
        Assert.Equal("revoke-section-access", entry.Action);
        Assert.NotNull(entry.EntryHash);
        Assert.NotEmpty(entry.EntryHash);
    }
}
