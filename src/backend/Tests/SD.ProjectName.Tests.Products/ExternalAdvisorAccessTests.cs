using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Unit tests for external advisor limited access functionality.
/// Tests time-bounded access, scope limitation, and read-only enforcement.
/// </summary>
public sealed class ExternalAdvisorAccessTests
{
    [Fact]
    public void InviteExternalAdvisor_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        var request = new InviteExternalAdvisorRequest
        {
            UserId = advisor.Id,
            RoleId = "role-external-advisor-read",
            SectionIds = new List<string> { section.Id },
            AccessExpiresAt = DateTime.UtcNow.AddDays(30).ToString("O"),
            Reason = "External audit review",
            InvitedBy = manager.Id
        };
        
        // Act
        var response = store.InviteExternalAdvisor(request, "Admin User");
        
        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.User);
        Assert.Contains("role-external-advisor-read", response.User.RoleIds);
        Assert.Equal(request.AccessExpiresAt, response.User.AccessExpiresAt);
        Assert.Single(response.SectionGrants);
        Assert.Equal(section.Id, response.SectionGrants[0].SectionId);
        Assert.Equal(advisor.Id, response.SectionGrants[0].UserId);
        Assert.Equal(request.AccessExpiresAt, response.SectionGrants[0].ExpiresAt);
    }
    
    [Fact]
    public void InviteExternalAdvisor_WithNonAdvisorRole_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        
        var request = new InviteExternalAdvisorRequest
        {
            UserId = advisor.Id,
            RoleId = "role-admin", // Not an advisor role
            SectionIds = new List<string>(),
            InvitedBy = manager.Id
        };
        
        // Act
        var response = store.InviteExternalAdvisor(request, "Admin User");
        
        // Assert
        Assert.False(response.Success);
        Assert.Contains("not an advisor role", response.ErrorMessage);
    }
    
    [Fact]
    public void InviteExternalAdvisor_WithMultipleSections_ShouldGrantAccessToAll()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id).Take(3).ToList();
        
        var request = new InviteExternalAdvisorRequest
        {
            UserId = advisor.Id,
            RoleId = "role-external-advisor-read",
            SectionIds = sections.Select(s => s.Id).ToList(),
            AccessExpiresAt = DateTime.UtcNow.AddDays(30).ToString("O"),
            InvitedBy = manager.Id
        };
        
        // Act
        var response = store.InviteExternalAdvisor(request, "Admin User");
        
        // Assert
        Assert.True(response.Success);
        Assert.Equal(3, response.SectionGrants.Count);
        
        foreach (var section in sections)
        {
            Assert.Contains(response.SectionGrants, g => g.SectionId == section.Id);
        }
    }
    
    [Fact]
    public void CheckPermission_WithExpiredAccess_ShouldDeny()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        
        // Set access to expired time
        advisor.AccessExpiresAt = DateTime.UtcNow.AddDays(-1).ToString("O");
        
        var request = new PermissionCheckRequest
        {
            UserId = advisor.Id,
            ResourceType = "section-content",
            ResourceId = "section-1",
            Action = "view"
        };
        
        // Act
        var result = store.CheckPermission(request, "Contributor User");
        
        // Assert
        Assert.False(result.Allowed);
        Assert.Contains("expired", result.DenialReason);
    }
    
    [Fact]
    public void CheckPermission_WithValidNonExpiredAccess_ShouldAllow()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        // Invite advisor with future expiry
        var inviteRequest = new InviteExternalAdvisorRequest
        {
            UserId = advisor.Id,
            RoleId = "role-external-advisor-read",
            SectionIds = new List<string> { section.Id },
            AccessExpiresAt = DateTime.UtcNow.AddDays(30).ToString("O"),
            InvitedBy = manager.Id
        };
        
        store.InviteExternalAdvisor(inviteRequest, "Admin User");
        
        var permRequest = new PermissionCheckRequest
        {
            UserId = advisor.Id,
            ResourceType = "section-content",
            ResourceId = section.Id,
            Action = "view"
        };
        
        // Act
        var result = store.CheckPermission(permRequest, "Contributor User");
        
        // Assert
        Assert.True(result.Allowed);
        Assert.Null(result.DenialReason);
    }
    
    [Fact]
    public void HasSectionAccess_WithExpiredGrant_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        // Grant access with past expiry
        var grantRequest = new GrantSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { advisor.Id },
            GrantedBy = manager.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1).ToString("O")
        };
        
        store.GrantSectionAccess(grantRequest);
        
        // Act
        var hasAccess = store.HasSectionAccess(advisor.Id, section.Id);
        
        // Assert
        Assert.False(hasAccess);
    }
    
    [Fact]
    public void HasSectionAccess_WithNonExpiredGrant_ShouldReturnTrue()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        // Grant access with future expiry
        var grantRequest = new GrantSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { advisor.Id },
            GrantedBy = manager.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30).ToString("O")
        };
        
        store.GrantSectionAccess(grantRequest);
        
        // Act
        var hasAccess = store.HasSectionAccess(advisor.Id, section.Id);
        
        // Assert
        Assert.True(hasAccess);
    }
    
    [Fact]
    public void GetAccessibleSections_WithExpiredGrant_ShouldExcludeSection()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var users = store.GetUsers();
        var advisor = users.First(u => u.Email == "john.smith@company.com");
        var manager = users.First(u => u.Email == "admin@company.com");
        var periods = store.GetPeriods();
        var period = periods.First();
        var sections = store.GetSections(period.Id);
        var section = sections.First();
        
        // Grant access with past expiry
        var grantRequest = new GrantSectionAccessRequest
        {
            SectionId = section.Id,
            UserIds = new List<string> { advisor.Id },
            GrantedBy = manager.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1).ToString("O")
        };
        
        store.GrantSectionAccess(grantRequest);
        
        // Act
        var accessibleSections = store.GetAccessibleSections(advisor.Id, period.Id);
        
        // Assert
        Assert.DoesNotContain(accessibleSections, s => s.Id == section.Id);
    }
    
    [Fact]
    public void ExternalAdvisorRoles_ShouldBeReadOnly()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var roles = store.GetRoles();
        var advisorReadRole = roles.First(r => r.Id == "role-external-advisor-read");
        var advisorEditRole = roles.First(r => r.Id == "role-external-advisor-edit");
        
        // Assert - Read-only advisor should not have edit permissions
        Assert.DoesNotContain(advisorReadRole.Permissions, p => 
            p.Contains("edit", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("approve", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("reject", StringComparison.OrdinalIgnoreCase));
        
        // Edit advisor should only have comment/recommendation permissions, not data editing
        Assert.DoesNotContain(advisorEditRole.Permissions, p => 
            p.Contains("edit-assigned-sections", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("approve", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("reject", StringComparison.OrdinalIgnoreCase));
        
        Assert.Contains("add-comments", advisorEditRole.Permissions);
        Assert.Contains("add-recommendations", advisorEditRole.Permissions);
    }
}
