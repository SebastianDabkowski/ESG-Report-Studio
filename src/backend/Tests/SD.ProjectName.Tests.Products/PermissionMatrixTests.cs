using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Unit tests for permission matrix functionality.
/// Tests permission matrix generation, permission checks, and audit logging of permission attempts.
/// </summary>
public sealed class PermissionMatrixTests
{
    [Fact]
    public void GetPermissionMatrix_ShouldReturnAllRoles()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        
        // Assert
        Assert.NotNull(matrix);
        Assert.Equal(9, matrix.Entries.Count); // 9 predefined roles
        Assert.NotEmpty(matrix.ResourceTypes);
        Assert.NotEmpty(matrix.AllActions);
    }
    
    [Fact]
    public void GetPermissionMatrix_ShouldIncludeExpectedResourceTypes()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        
        // Assert
        var expectedResources = new[]
        {
            "report-structure",
            "section-content",
            "esg-data-items",
            "attachments",
            "exports",
            "users"
        };
        
        foreach (var resource in expectedResources)
        {
            Assert.Contains(resource, matrix.ResourceTypes);
        }
    }
    
    [Fact]
    public void GetPermissionMatrix_ShouldIncludeExpectedActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        
        // Assert
        var expectedActions = new[]
        {
            "view",
            "edit",
            "comment",
            "submit",
            "approve",
            "reject",
            "export",
            "manage"
        };
        
        foreach (var action in expectedActions)
        {
            Assert.Contains(action, matrix.AllActions);
        }
    }
    
    [Fact]
    public void GetPermissionMatrix_AdminRole_ShouldHaveAllPermissionsOnAllResources()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        var adminEntry = matrix.Entries.FirstOrDefault(e => e.RoleName == "Admin");
        
        // Assert
        Assert.NotNull(adminEntry);
        
        // Admin should have all actions on all resources
        foreach (var resourceType in matrix.ResourceTypes)
        {
            Assert.True(adminEntry.ResourceActions.ContainsKey(resourceType));
            Assert.NotEmpty(adminEntry.ResourceActions[resourceType]);
            Assert.Contains("view", adminEntry.ResourceActions[resourceType]);
            Assert.Contains("edit", adminEntry.ResourceActions[resourceType]);
        }
    }
    
    [Fact]
    public void GetPermissionMatrix_ComplianceOfficerRole_ShouldHaveViewAndExportOnExports()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        var complianceEntry = matrix.Entries.FirstOrDefault(e => e.RoleName == "Compliance Officer");
        
        // Assert
        Assert.NotNull(complianceEntry);
        Assert.True(complianceEntry.ResourceActions.ContainsKey("exports"));
        Assert.Contains("export", complianceEntry.ResourceActions["exports"]);
        Assert.Contains("view", complianceEntry.ResourceActions["exports"]);
    }
    
    [Fact]
    public void GetPermissionMatrix_ContributorRole_ShouldHaveEditOnAssignedSections()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        var contributorEntry = matrix.Entries.FirstOrDefault(e => e.RoleName == "Contributor");
        
        // Assert
        Assert.NotNull(contributorEntry);
        Assert.True(contributorEntry.ResourceActions.ContainsKey("section-content"));
        Assert.Contains("edit", contributorEntry.ResourceActions["section-content"]);
    }
    
    [Fact]
    public void GetPermissionMatrix_ExternalAdvisorReadRole_ShouldOnlyHaveViewPermissions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Act
        var matrix = store.GetPermissionMatrix();
        var advisorEntry = matrix.Entries.FirstOrDefault(e => e.RoleName == "External Advisor (Read)");
        
        // Assert
        Assert.NotNull(advisorEntry);
        
        // Should only have view permissions, no edit/approve/etc
        foreach (var resourceActions in advisorEntry.ResourceActions.Values)
        {
            Assert.All(resourceActions, action => Assert.Equal("view", action));
        }
    }
    
    [Fact]
    public void CheckPermission_UserWithPermission_ShouldReturnAllowed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var complianceRole = roles.First(r => r.Name == "Compliance Officer");
        
        // Assign Compliance Officer role
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { complianceRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        var checkRequest = new PermissionCheckRequest
        {
            UserId = user.Id,
            ResourceType = "exports",
            Action = "export"
        };
        
        // Act
        var result = store.CheckPermission(checkRequest, user.Name);
        
        // Assert
        Assert.True(result.Allowed);
        Assert.Null(result.DenialReason);
        Assert.Contains("Compliance Officer", result.EvaluatedRoles);
    }
    
    [Fact]
    public void CheckPermission_UserWithoutPermission_ShouldReturnDenied()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var contributorRole = roles.First(r => r.Name == "Contributor");
        
        // Assign Contributor role (does not have export permission)
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { contributorRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        var checkRequest = new PermissionCheckRequest
        {
            UserId = user.Id,
            ResourceType = "exports",
            Action = "export"
        };
        
        // Act
        var result = store.CheckPermission(checkRequest, user.Name);
        
        // Assert
        Assert.False(result.Allowed);
        Assert.NotNull(result.DenialReason);
        Assert.Contains("Missing required permission", result.DenialReason);
    }
    
    [Fact]
    public void CheckPermission_AdminUser_ShouldAllowAllActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Assign Admin role
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { adminRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        // Test multiple different actions
        var actions = new[] { "view", "edit", "approve", "export", "manage" };
        
        // Act & Assert
        foreach (var action in actions)
        {
            var checkRequest = new PermissionCheckRequest
            {
                UserId = user.Id,
                ResourceType = "section-content",
                Action = action
            };
            
            var result = store.CheckPermission(checkRequest, user.Name);
            Assert.True(result.Allowed, $"Admin should be allowed to {action}");
        }
    }
    
    [Fact]
    public void CheckPermission_NonExistentUser_ShouldReturnDenied()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        var checkRequest = new PermissionCheckRequest
        {
            UserId = "non-existent-user",
            ResourceType = "section-content",
            Action = "view"
        };
        
        // Act
        var result = store.CheckPermission(checkRequest, "Unknown User");
        
        // Assert
        Assert.False(result.Allowed);
        Assert.Equal("User not found", result.DenialReason);
    }
    
    [Fact]
    public void CheckPermission_ShouldLogAllowedAttemptToAudit()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Assign Admin role
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { adminRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        var checkRequest = new PermissionCheckRequest
        {
            UserId = user.Id,
            ResourceType = "section-content",
            Action = "view"
        };
        
        // Act
        store.CheckPermission(checkRequest, user.Name);
        
        // Assert
        var auditEntries = store.GetAuditLog(entityType: "Permission");
        var checkEntry = auditEntries.FirstOrDefault(e => e.Action == "permission-check-allowed");
        
        Assert.NotNull(checkEntry);
        Assert.Equal(user.Id, checkEntry.UserId);
        Assert.Contains(checkEntry.Changes, c => c.Field == "ResourceType" && c.NewValue == "section-content");
        Assert.Contains(checkEntry.Changes, c => c.Field == "Action" && c.NewValue == "view");
        Assert.Contains(checkEntry.Changes, c => c.Field == "Allowed" && c.NewValue == "True");
    }
    
    [Fact]
    public void CheckPermission_ShouldLogDeniedAttemptToAudit()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var contributorRole = roles.First(r => r.Name == "Contributor");
        
        // Assign Contributor role (no export permission)
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { contributorRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        var checkRequest = new PermissionCheckRequest
        {
            UserId = user.Id,
            ResourceType = "exports",
            Action = "export"
        };
        
        // Act
        store.CheckPermission(checkRequest, user.Name);
        
        // Assert
        var auditEntries = store.GetAuditLog(entityType: "Permission");
        var checkEntry = auditEntries.FirstOrDefault(e => e.Action == "permission-check-denied");
        
        Assert.NotNull(checkEntry);
        Assert.Equal(user.Id, checkEntry.UserId);
        Assert.Contains(checkEntry.Changes, c => c.Field == "ResourceType" && c.NewValue == "exports");
        Assert.Contains(checkEntry.Changes, c => c.Field == "Action" && c.NewValue == "export");
        Assert.Contains(checkEntry.Changes, c => c.Field == "Allowed" && c.NewValue == "False");
        Assert.Contains(checkEntry.Changes, c => c.Field == "DenialReason");
    }
    
    [Fact]
    public void GetPermissionChangeHistory_ShouldIncludeRoleChanges()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Update role description
        var updateRequest = new UpdateRoleRequest
        {
            Description = "Updated admin description"
        };
        store.UpdateRoleDescription(adminRole.Id, updateRequest, "test-user", "Test User");
        
        // Act
        var history = store.GetPermissionChangeHistory();
        
        // Assert
        Assert.NotEmpty(history);
        Assert.Contains(history, e => 
            e.EntityType == "SystemRole" && 
            e.Action == "update-role-description" &&
            e.EntityId == adminRole.Id);
    }
    
    [Fact]
    public void GetPermissionChangeHistory_ShouldIncludeUserRoleAssignments()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var user = users.First();
        
        var roles = store.GetRoles();
        var adminRole = roles.First(r => r.Name == "Admin");
        
        // Assign role
        var assignRequest = new AssignUserRolesRequest
        {
            RoleIds = new List<string> { adminRole.Id }
        };
        store.AssignUserRoles(user.Id, assignRequest, "test-user", "Test User");
        
        // Act
        var history = store.GetPermissionChangeHistory();
        
        // Assert
        Assert.NotEmpty(history);
        Assert.Contains(history, e => 
            e.EntityType == "User" && 
            e.Action == "assign-user-roles" &&
            e.EntityId == user.Id);
    }
    
    [Fact]
    public void GetPermissionChangeHistory_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create multiple permission changes
        for (int i = 0; i < 10; i++)
        {
            var createRequest = new CreateRoleRequest
            {
                Name = $"Custom Role {i}",
                Description = $"Description {i}",
                Permissions = new List<string> { "view" }
            };
            store.CreateRole(createRequest, "test-user", "Test User");
        }
        
        // Act
        var history = store.GetPermissionChangeHistory(limit: 5);
        
        // Assert
        Assert.Equal(5, history.Count);
    }
    
    [Fact]
    public void GetPermissionChangeHistory_ShouldBeOrderedByTimestampDescending()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create multiple changes
        var createRequest1 = new CreateRoleRequest
        {
            Name = "Custom Role 1",
            Description = "First",
            Permissions = new List<string> { "view" }
        };
        store.CreateRole(createRequest1, "test-user", "Test User");
        
        System.Threading.Thread.Sleep(10); // Ensure different timestamps
        
        var createRequest2 = new CreateRoleRequest
        {
            Name = "Custom Role 2",
            Description = "Second",
            Permissions = new List<string> { "view" }
        };
        store.CreateRole(createRequest2, "test-user", "Test User");
        
        // Act
        var history = store.GetPermissionChangeHistory();
        
        // Assert
        Assert.NotEmpty(history);
        
        // Most recent should be first
        var timestamps = history.Select(e => DateTime.Parse(e.Timestamp)).ToList();
        for (int i = 0; i < timestamps.Count - 1; i++)
        {
            Assert.True(timestamps[i] >= timestamps[i + 1], 
                "History should be ordered by timestamp descending");
        }
    }
}
