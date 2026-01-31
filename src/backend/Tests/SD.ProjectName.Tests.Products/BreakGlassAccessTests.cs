using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Unit tests for break-glass admin access functionality.
/// Tests activation, deactivation, authorization, and audit trail integration.
/// </summary>
public sealed class BreakGlassAccessTests
{
    [Fact]
    public void ActivateBreakGlass_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2"; // Admin user from sample data
        var adminUserName = "Admin User";
        var reason = "Emergency: Production system down, need to bypass approval workflow";

        // Act
        var (success, errorMessage, session) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            reason,
            "MFA",
            "192.168.1.100"
        );

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.NotNull(session);
        Assert.Equal(adminUserId, session.UserId);
        Assert.Equal(adminUserName, session.UserName);
        Assert.Equal(reason, session.Reason);
        Assert.True(session.IsActive);
        Assert.Equal("MFA", session.AuthenticationMethod);
        Assert.Equal("192.168.1.100", session.IpAddress);
        // Action count starts at 1 because activating break-glass creates an audit entry
        Assert.Equal(1, session.ActionCount);
    }

    [Fact]
    public void ActivateBreakGlass_WithShortReason_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var shortReason = "Too short";

        // Act
        var (success, errorMessage, session) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            shortReason
        );

        // Assert
        Assert.False(success);
        Assert.Contains("at least 20 characters", errorMessage);
        Assert.Null(session);
    }

    [Fact]
    public void ActivateBreakGlass_WithInactiveUser_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var inactiveUserId = "inactive-user";
        var reason = "Valid reason with sufficient length for testing purposes";

        // Act
        var (success, errorMessage, session) = store.ActivateBreakGlass(
            inactiveUserId,
            "Inactive User",
            reason
        );

        // Assert
        Assert.False(success);
        Assert.Contains("not found or inactive", errorMessage);
        Assert.Null(session);
    }

    [Fact]
    public void ActivateBreakGlass_WhenAlreadyActive_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Production system down, need immediate access";

        // Activate first session
        store.ActivateBreakGlass(adminUserId, adminUserName, reason);

        // Act - Try to activate again
        var (success, errorMessage, session) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "Another emergency requiring break-glass access"
        );

        // Assert
        Assert.False(success);
        Assert.Contains("already has an active break-glass session", errorMessage);
        Assert.Null(session);
    }

    [Fact]
    public void DeactivateBreakGlass_WithActiveSession_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Need to recover locked account";

        var (_, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);
        var deactivationNote = "Emergency resolved, access restored";

        // Act
        var (success, errorMessage) = store.DeactivateBreakGlass(
            session!.Id,
            adminUserId,
            adminUserName,
            deactivationNote
        );

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        // Verify session is deactivated
        var activeSession = store.GetActiveBreakGlassSession(adminUserId);
        Assert.Null(activeSession);

        // Verify deactivation details
        var sessions = store.GetBreakGlassSessions(adminUserId);
        var deactivatedSession = sessions.First();
        Assert.False(deactivatedSession.IsActive);
        Assert.NotNull(deactivatedSession.DeactivatedAt);
        Assert.Equal(adminUserId, deactivatedSession.DeactivatedBy);
        Assert.Equal(adminUserName, deactivatedSession.DeactivatedByName);
        Assert.Equal(deactivationNote, deactivatedSession.DeactivationNote);
    }

    [Fact]
    public void DeactivateBreakGlass_WithInvalidSession_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var invalidSessionId = "non-existent-session";

        // Act
        var (success, errorMessage) = store.DeactivateBreakGlass(
            invalidSessionId,
            "user-2",
            "Admin User"
        );

        // Assert
        Assert.False(success);
        Assert.Contains("not found", errorMessage);
    }

    [Fact]
    public void DeactivateBreakGlass_WhenAlreadyDeactivated_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Database corruption detected";

        var (_, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);
        store.DeactivateBreakGlass(session!.Id, adminUserId, adminUserName);

        // Act - Try to deactivate again
        var (success, errorMessage) = store.DeactivateBreakGlass(
            session.Id,
            adminUserId,
            adminUserName
        );

        // Assert
        Assert.False(success);
        Assert.Contains("already deactivated", errorMessage);
    }

    [Fact]
    public void GetActiveBreakGlassSession_WithActiveSession_ShouldReturnSession()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Critical security incident";

        store.ActivateBreakGlass(adminUserId, adminUserName, reason);

        // Act
        var activeSession = store.GetActiveBreakGlassSession(adminUserId);

        // Assert
        Assert.NotNull(activeSession);
        Assert.Equal(adminUserId, activeSession.UserId);
        Assert.True(activeSession.IsActive);
    }

    [Fact]
    public void GetActiveBreakGlassSession_WithNoActiveSession_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";

        // Act
        var activeSession = store.GetActiveBreakGlassSession(adminUserId);

        // Assert
        Assert.Null(activeSession);
    }

    [Fact]
    public void GetBreakGlassSessions_ShouldReturnAllSessions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";

        // Create multiple sessions
        var (_, _, session1) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "First emergency: System outage requiring immediate intervention"
        );
        store.DeactivateBreakGlass(session1!.Id, adminUserId, adminUserName);

        store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "Second emergency: Data integrity issue needs resolution"
        );

        // Act
        var sessions = store.GetBreakGlassSessions(adminUserId);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Single(sessions.Where(s => s.IsActive));
        Assert.Single(sessions.Where(s => !s.IsActive));
    }

    [Fact]
    public void GetBreakGlassSessions_WithActiveOnlyFilter_ShouldReturnOnlyActiveSessions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";

        var (_, _, session1) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "First emergency requiring elevated access privileges"
        );
        store.DeactivateBreakGlass(session1!.Id, adminUserId, adminUserName);

        store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "Second emergency requiring break-glass intervention"
        );

        // Act
        var activeSessions = store.GetBreakGlassSessions(adminUserId, activeOnly: true);

        // Assert
        Assert.Single(activeSessions);
        Assert.All(activeSessions, s => Assert.True(s.IsActive));
    }

    [Fact]
    public void IsAuthorizedForBreakGlass_WithAdminUser_ShouldReturnTrue()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2"; // Admin user from sample data

        // Act
        var isAuthorized = store.IsAuthorizedForBreakGlass(adminUserId);

        // Assert
        Assert.True(isAuthorized);
    }

    [Fact]
    public void IsAuthorizedForBreakGlass_WithNonAdminUser_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var contributorUserId = "user-3"; // Contributor user from sample data

        // Act
        var isAuthorized = store.IsAuthorizedForBreakGlass(contributorUserId);

        // Assert
        Assert.False(isAuthorized);
    }

    [Fact]
    public void IsAuthorizedForBreakGlass_WithInactiveUser_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var inactiveUserId = "inactive-user";

        // Act
        var isAuthorized = store.IsAuthorizedForBreakGlass(inactiveUserId);

        // Assert
        Assert.False(isAuthorized);
    }

    [Fact]
    public void IncrementBreakGlassActionCount_WithActiveSession_ShouldIncrementCount()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Critical system failure";

        var (_, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);

        // Act
        store.IncrementBreakGlassActionCount(session!.Id);
        store.IncrementBreakGlassActionCount(session.Id);
        store.IncrementBreakGlassActionCount(session.Id);

        // Assert
        var activeSession = store.GetActiveBreakGlassSession(adminUserId);
        Assert.NotNull(activeSession);
        // Action count is 1 from activation + 3 manual increments = 4
        Assert.Equal(4, activeSession.ActionCount);
    }

    [Fact]
    public void AuditLog_DuringBreakGlassSession_ShouldTagActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Need to update critical configuration";

        // Activate break-glass - this creates an audit entry
        var (bgSuccess, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);
        Assert.True(bgSuccess);

        // Act - Check that activation was tagged as break-glass action
        var auditEntries = store.GetAuditLog(
            action: "activate-break-glass",
            breakGlassOnly: true
        );

        // Assert
        Assert.NotEmpty(auditEntries);
        var entry = auditEntries.First();
        Assert.True(entry.IsBreakGlassAction);
        Assert.Equal(session!.Id, entry.BreakGlassSessionId);
        Assert.Equal(adminUserId, entry.UserId);
        Assert.Equal("BreakGlassSession", entry.EntityType);
    }

    [Fact]
    public void AuditLog_AfterBreakGlassDeactivation_ShouldNotTagActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Temporary elevated access needed";

        // Activate and then deactivate break-glass
        var (bgSuccess, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);
        Assert.True(bgSuccess);
        var (deactivateSuccess, _) = store.DeactivateBreakGlass(session!.Id, adminUserId, adminUserName);
        Assert.True(deactivateSuccess);

        // Assert - Check that deactivation action is NOT tagged as break-glass
        var deactivationEntries = store.GetAuditLog(
            action: "deactivate-break-glass"
        );

        var deactivationEntry = deactivationEntries.First();
        Assert.False(deactivationEntry.IsBreakGlassAction);
        Assert.Null(deactivationEntry.BreakGlassSessionId);
    }

    [Fact]
    public void BreakGlassActivation_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: Critical production issue";

        // Act
        var (success, _, session) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            reason,
            "MFA",
            "192.168.1.100"
        );

        // Assert
        Assert.True(success);
        var auditEntries = store.GetAuditLog(
            action: "activate-break-glass",
            entityType: "BreakGlassSession"
        );

        Assert.NotEmpty(auditEntries);
        var entry = auditEntries.First();
        Assert.Equal(adminUserId, entry.UserId);
        Assert.Equal("activate-break-glass", entry.Action);
        Assert.Equal(session!.Id, entry.EntityId);
        Assert.Contains("Break-glass access activated", entry.ChangeNote);
    }

    [Fact]
    public void BreakGlassDeactivation_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";
        var reason = "Emergency: System recovery operation";

        var (_, _, session) = store.ActivateBreakGlass(adminUserId, adminUserName, reason);
        var deactivationNote = "Recovery complete, normal operations restored";

        // Act
        var (success, _) = store.DeactivateBreakGlass(
            session!.Id,
            adminUserId,
            adminUserName,
            deactivationNote
        );

        // Assert
        Assert.True(success);
        var auditEntries = store.GetAuditLog(
            action: "deactivate-break-glass",
            entityType: "BreakGlassSession"
        );

        Assert.NotEmpty(auditEntries);
        var entry = auditEntries.First();
        Assert.Equal(adminUserId, entry.UserId);
        Assert.Equal("deactivate-break-glass", entry.Action);
        Assert.Equal(session.Id, entry.EntityId);
        Assert.Equal(deactivationNote, entry.ChangeNote);
    }

    [Fact]
    public void AuditLog_BreakGlassFilter_ShouldReturnOnlyBreakGlassActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var adminUserId = "user-2";
        var adminUserName = "Admin User";

        // Perform activation (creates break-glass action)
        var (_, _, session1) = store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "Emergency: Critical update required immediately"
        );
        
        // Deactivate (creates non-break-glass action)
        store.DeactivateBreakGlass(session1!.Id, adminUserId, adminUserName);
        
        // Activate again (creates another break-glass action)
        store.ActivateBreakGlass(
            adminUserId,
            adminUserName,
            "Second emergency: Another critical issue"
        );

        // Act - Filter for break-glass actions only
        var breakGlassEntries = store.GetAuditLog(breakGlassOnly: true);
        var nonBreakGlassEntries = store.GetAuditLog(breakGlassOnly: false);

        // Assert
        Assert.NotEmpty(breakGlassEntries);
        Assert.All(breakGlassEntries, e => Assert.True(e.IsBreakGlassAction));
        Assert.All(breakGlassEntries, e => Assert.Equal("activate-break-glass", e.Action));
        
        Assert.NotEmpty(nonBreakGlassEntries);
        Assert.All(nonBreakGlassEntries, e => Assert.False(e.IsBreakGlassAction));
        Assert.Contains(nonBreakGlassEntries, e => e.Action == "deactivate-break-glass");
    }
}
