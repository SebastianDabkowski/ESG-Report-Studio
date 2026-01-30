using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Unit tests for role management functionality.
/// Tests role catalog operations including predefined role protection.
/// </summary>
public sealed class RoleManagementTests
{
    [Fact]
    public void InitializePredefinedRoles_ShouldCreate9Roles()
    {
        // Arrange & Act
        var store = new InMemoryReportStore();
        
        // Assert
        var roles = store.GetRoles();
        Assert.Equal(9, roles.Count);
    }
    
    [Fact]
    public void GetRoles_ShouldReturnAllPredefinedRoles()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var roles = store.GetRoles();
        
        // Assert
        var expectedRoleNames = new[]
        {
            "Admin",
            "Management",
            "Compliance Officer",
            "Reviewer",
            "Contributor",
            "Data Owner",
            "Approver",
            "External Advisor (Read)",
            "External Advisor (Edit - Limited)"
        };
        
        foreach (var expectedName in expectedRoleNames)
        {
            Assert.Contains(roles, r => r.Name == expectedName);
        }
    }
    
    [Fact]
    public void GetRole_WithValidId_ShouldReturnRole()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Act
        var role = store.GetRole(adminRole.Id);
        
        // Assert
        Assert.NotNull(role);
        Assert.Equal("Admin", role.Name);
        Assert.Contains("all", role.Permissions);
        Assert.True(role.IsPredefined);
    }
    
    [Fact]
    public void GetRole_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var role = store.GetRole("non-existent-id");
        
        // Assert
        Assert.Null(role);
    }
    
    [Fact]
    public void PredefinedRoles_ShouldHaveDescriptionsAndPermissions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var roles = store.GetRoles();
        
        // Assert
        foreach (var role in roles.Where(r => r.IsPredefined))
        {
            Assert.False(string.IsNullOrWhiteSpace(role.Description), 
                $"Role '{role.Name}' is missing description");
            Assert.NotEmpty(role.Permissions);
        }
    }
    
    [Fact]
    public void CreateRole_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateRoleRequest
        {
            Name = "Custom Role",
            Description = "A custom role for testing",
            Permissions = new List<string> { "view-reports", "edit-reports" }
        };
        
        // Act
        var (success, errorMessage, role) = store.CreateRole(request, "test-user", "Test User");
        
        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.NotNull(role);
        Assert.Equal("Custom Role", role.Name);
        Assert.Equal("A custom role for testing", role.Description);
        Assert.Equal(2, role.Permissions.Count);
        Assert.False(role.IsPredefined);
        Assert.Equal(1, role.Version);
    }
    
    [Fact]
    public void CreateRole_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateRoleRequest
        {
            Name = "Admin", // Duplicate of predefined role
            Description = "Another admin role",
            Permissions = new List<string> { "all" }
        };
        
        // Act
        var (success, errorMessage, role) = store.CreateRole(request, "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("already exists", errorMessage);
        Assert.Null(role);
    }
    
    [Fact]
    public void CreateRole_WithEmptyName_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateRoleRequest
        {
            Name = "",
            Description = "Description",
            Permissions = new List<string> { "view" }
        };
        
        // Act
        var (success, errorMessage, role) = store.CreateRole(request, "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("name is required", errorMessage);
        Assert.Null(role);
    }
    
    [Fact]
    public void CreateRole_WithNoPermissions_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateRoleRequest
        {
            Name = "Empty Role",
            Description = "Role with no permissions",
            Permissions = new List<string>()
        };
        
        // Act
        var (success, errorMessage, role) = store.CreateRole(request, "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("At least one permission is required", errorMessage);
        Assert.Null(role);
    }
    
    [Fact]
    public void UpdateRoleDescription_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        var updateRequest = new UpdateRoleRequest
        {
            Description = "Updated description for admin role"
        };
        
        // Act
        var (success, errorMessage) = store.UpdateRoleDescription(
            adminRole.Id, updateRequest, "test-user", "Test User");
        
        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        
        var updatedRole = store.GetRole(adminRole.Id);
        Assert.NotNull(updatedRole);
        Assert.Equal("Updated description for admin role", updatedRole.Description);
        Assert.Equal(2, updatedRole.Version); // Version incremented
        Assert.NotNull(updatedRole.UpdatedAt);
        Assert.Equal("test-user", updatedRole.UpdatedBy);
    }
    
    [Fact]
    public void UpdateRoleDescription_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        var updateRequest = new UpdateRoleRequest
        {
            Description = "New description"
        };
        
        // Act
        store.UpdateRoleDescription(adminRole.Id, updateRequest, "test-user", "Test User");
        
        // Assert
        var auditEntries = store.GetAuditLog(entityType: "SystemRole", entityId: adminRole.Id);
        
        Assert.NotEmpty(auditEntries);
        var latestEntry = auditEntries.Where(e => e.Action == "update-role-description")
            .OrderByDescending(e => e.Timestamp).First();
        Assert.Equal("test-user", latestEntry.UserId);
        Assert.Contains(latestEntry.Changes, c => c.Field == "Description");
        Assert.Contains(latestEntry.Changes, c => c.Field == "Version");
    }
    
    [Fact]
    public void UpdateRoleDescription_WithEmptyDescription_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        var updateRequest = new UpdateRoleRequest
        {
            Description = ""
        };
        
        // Act
        var (success, errorMessage) = store.UpdateRoleDescription(
            adminRole.Id, updateRequest, "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("cannot be empty", errorMessage);
    }
    
    [Fact]
    public void DeleteRole_PredefinedRole_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Act
        var (success, errorMessage) = store.DeleteRole(adminRole.Id, "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("Cannot delete predefined role", errorMessage);
        Assert.Contains("essential for system access control", errorMessage);
        
        // Verify role still exists
        var roleAfterDelete = store.GetRole(adminRole.Id);
        Assert.NotNull(roleAfterDelete);
    }
    
    [Fact]
    public void DeleteRole_CustomRole_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var createRequest = new CreateRoleRequest
        {
            Name = "Custom Role",
            Description = "A custom role",
            Permissions = new List<string> { "view" }
        };
        var (_, _, createdRole) = store.CreateRole(createRequest, "test-user", "Test User");
        
        // Act
        var (success, errorMessage) = store.DeleteRole(createdRole!.Id, "test-user", "Test User");
        
        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        
        // Verify role is deleted
        var roleAfterDelete = store.GetRole(createdRole.Id);
        Assert.Null(roleAfterDelete);
    }
    
    [Fact]
    public void DeleteRole_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var createRequest = new CreateRoleRequest
        {
            Name = "Custom Role",
            Description = "A custom role",
            Permissions = new List<string> { "view" }
        };
        var (_, _, createdRole) = store.CreateRole(createRequest, "test-user", "Test User");
        
        // Act
        store.DeleteRole(createdRole!.Id, "test-user", "Test User");
        
        // Assert
        var auditEntries = store.GetAuditLog(entityType: "SystemRole", entityId: createdRole.Id);
        
        Assert.NotEmpty(auditEntries);
        var deleteEntry = auditEntries.Where(e => e.Action == "delete-role")
            .OrderByDescending(e => e.Timestamp).First();
        Assert.Equal("test-user", deleteEntry.UserId);
        Assert.Contains(deleteEntry.Changes, c => c.Field == "Status" && c.NewValue == "deleted");
    }
    
    [Fact]
    public void DeleteRole_WithInvalidId_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var (success, errorMessage) = store.DeleteRole("non-existent-id", "test-user", "Test User");
        
        // Assert
        Assert.False(success);
        Assert.Contains("not found", errorMessage);
    }
    
    [Fact]
    public void AllPredefinedRoles_ShouldBePredefined()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var roles = store.GetRoles();
        
        // Assert
        var predefinedRoles = roles.Where(r => r.IsPredefined).ToList();
        Assert.Equal(9, predefinedRoles.Count);
        
        // All initially created roles should be predefined
        Assert.All(predefinedRoles, r => Assert.True(r.IsPredefined));
    }
    
    [Fact]
    public void ComplianceOfficerRole_ShouldHaveCorrectPermissions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var roles = store.GetRoles();
        var complianceRole = roles.First(r => r.Name == "Compliance Officer");
        
        // Assert
        Assert.Contains("view-all-reports", complianceRole.Permissions);
        Assert.Contains("manage-validation-rules", complianceRole.Permissions);
        Assert.Contains("run-audits", complianceRole.Permissions);
        Assert.Contains("export-audit-packages", complianceRole.Permissions);
        Assert.Contains("view-compliance-reports", complianceRole.Permissions);
    }
    
    [Fact]
    public void ExternalAdvisorReadRole_ShouldHaveReadOnlyPermissions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var roles = store.GetRoles();
        var advisorRole = roles.First(r => r.Name == "External Advisor (Read)");
        
        // Assert
        Assert.Contains("view-reports", advisorRole.Permissions);
        Assert.Contains("view-public-sections", advisorRole.Permissions);
        Assert.DoesNotContain(advisorRole.Permissions, p => p.Contains("edit") || p.Contains("delete") || p.Contains("create"));
    }
    
    [Fact]
    public void CreateRole_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateRoleRequest
        {
            Name = "Custom Role",
            Description = "A custom role",
            Permissions = new List<string> { "view-reports" }
        };
        
        // Act
        var (_, _, createdRole) = store.CreateRole(request, "test-user", "Test User");
        
        // Assert
        var auditEntries = store.GetAuditLog(entityType: "SystemRole", entityId: createdRole!.Id);
        
        Assert.NotEmpty(auditEntries);
        var createEntry = auditEntries.Where(e => e.Action == "create-role")
            .OrderByDescending(e => e.Timestamp).First();
        Assert.Equal("test-user", createEntry.UserId);
        Assert.Contains(createEntry.Changes, c => c.Field == "Name" && c.NewValue == "Custom Role");
        Assert.Contains(createEntry.Changes, c => c.Field == "Description");
        Assert.Contains(createEntry.Changes, c => c.Field == "Permissions");
    }
}
