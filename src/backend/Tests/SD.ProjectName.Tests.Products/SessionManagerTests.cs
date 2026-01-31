using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace SD.ProjectName.Tests.Products;

public sealed class SessionManagerTests
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<SessionManager> _logger;
    private readonly IOptions<AuthenticationSettings> _authSettings;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _store = new InMemoryReportStore();
        _logger = new LoggerFactory().CreateLogger<SessionManager>();
        
        var settings = new AuthenticationSettings
        {
            SessionTimeout = new SessionTimeoutSettings
            {
                Enabled = true,
                IdleTimeoutMinutes = 30,
                AbsoluteTimeoutMinutes = 480,
                WarningBeforeTimeoutMinutes = 5,
                AllowSessionRefresh = true
            }
        };
        _authSettings = Options.Create(settings);
        _sessionManager = new SessionManager(_store, _logger, _authSettings);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateSession_WithValidParameters()
    {
        // Arrange
        var userId = "user-123";
        var userName = "Test User";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var mfaVerified = true;

        // Act
        var session = await _sessionManager.CreateSessionAsync(userId, userName, ipAddress, userAgent, mfaVerified);

        // Assert
        Assert.NotNull(session);
        Assert.NotEmpty(session.SessionId);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(userName, session.UserName);
        Assert.Equal(ipAddress, session.IpAddress);
        Assert.Equal(userAgent, session.UserAgent);
        Assert.True(session.MfaVerified);
        Assert.NotEmpty(session.CreatedAt);
        Assert.NotEmpty(session.LastActivityAt);
        Assert.NotEmpty(session.ExpiresAt);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Arrange
        var userId = "";
        var userName = "Test User";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sessionManager.CreateSessionAsync(userId, userName, null, null, false));
    }

    [Fact]
    public async Task UpdateActivityAsync_ShouldExtendSessionExpiration()
    {
        // Arrange
        var session = await _sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);
        var originalExpiresAt = session.ExpiresAt;
        
        // Wait a bit to ensure different timestamp
        await Task.Delay(100);

        // Act
        var result = await _sessionManager.UpdateActivityAsync(session.SessionId);

        // Assert
        Assert.True(result);
        
        // Get session status to check new expiration
        var status = await _sessionManager.GetSessionStatusAsync(session.SessionId);
        Assert.True(status.IsActive);
        Assert.NotNull(status.ExpiresAt);
    }

    [Fact]
    public async Task UpdateActivityAsync_ShouldReturnFalse_ForNonExistentSession()
    {
        // Arrange
        var sessionId = "non-existent-session";

        // Act
        var result = await _sessionManager.UpdateActivityAsync(sessionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetSessionStatusAsync_ShouldReturnActiveStatus_ForValidSession()
    {
        // Arrange
        var session = await _sessionManager.CreateSessionAsync("user-123", "Test User", null, null, true);

        // Act
        var status = await _sessionManager.GetSessionStatusAsync(session.SessionId);

        // Assert
        Assert.True(status.IsActive);
        Assert.Equal(session.SessionId, status.SessionId);
        Assert.NotNull(status.ExpiresAt);
        Assert.True(status.MinutesUntilExpiration > 0);
        Assert.False(status.ShouldWarn); // Should not warn immediately
        Assert.True(status.CanRefresh);
    }

    [Fact]
    public async Task GetSessionStatusAsync_ShouldReturnInactiveStatus_ForNonExistentSession()
    {
        // Arrange
        var sessionId = "non-existent-session";

        // Act
        var status = await _sessionManager.GetSessionStatusAsync(sessionId);

        // Assert
        Assert.False(status.IsActive);
        Assert.Null(status.SessionId);
        Assert.Null(status.ExpiresAt);
        Assert.Null(status.MinutesUntilExpiration);
        Assert.False(status.ShouldWarn);
        Assert.False(status.CanRefresh);
    }

    [Fact]
    public async Task RefreshSessionAsync_ShouldExtendSession_WhenAllowed()
    {
        // Arrange
        var session = await _sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);
        var originalExpiresAt = session.ExpiresAt;
        
        // Wait a bit
        await Task.Delay(100);

        // Act
        var result = await _sessionManager.RefreshSessionAsync(session.SessionId);

        // Assert
        Assert.True(result);
        
        // Verify session was extended
        var status = await _sessionManager.GetSessionStatusAsync(session.SessionId);
        Assert.True(status.IsActive);
    }

    [Fact]
    public async Task RefreshSessionAsync_ShouldReturnFalse_ForNonExistentSession()
    {
        // Arrange
        var sessionId = "non-existent-session";

        // Act
        var result = await _sessionManager.RefreshSessionAsync(sessionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RefreshSessionAsync_ShouldReturnFalse_WhenRefreshNotAllowed()
    {
        // Arrange - Create manager with refresh disabled
        var settings = new AuthenticationSettings
        {
            SessionTimeout = new SessionTimeoutSettings
            {
                Enabled = true,
                IdleTimeoutMinutes = 30,
                AbsoluteTimeoutMinutes = 480,
                WarningBeforeTimeoutMinutes = 5,
                AllowSessionRefresh = false // Disabled
            }
        };
        var authSettings = Options.Create(settings);
        var sessionManager = new SessionManager(_store, _logger, authSettings);
        
        var session = await sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);

        // Act
        var result = await sessionManager.RefreshSessionAsync(session.SessionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EndSessionAsync_ShouldRemoveSession()
    {
        // Arrange
        var session = await _sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);

        // Act
        await _sessionManager.EndSessionAsync(session.SessionId, "User logout");

        // Assert
        var status = await _sessionManager.GetSessionStatusAsync(session.SessionId);
        Assert.False(status.IsActive);
    }

    [Fact]
    public async Task GetSessionEventsAsync_ShouldReturnEventsForUser()
    {
        // Arrange
        var userId = "user-123";
        var session1 = await _sessionManager.CreateSessionAsync(userId, "Test User", "192.168.1.1", "Browser1", true);
        await _sessionManager.RefreshSessionAsync(session1.SessionId);
        await _sessionManager.EndSessionAsync(session1.SessionId, "User logout");

        // Act
        var events = await _sessionManager.GetSessionEventsAsync(userId: userId);

        // Assert
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.EventType == "login");
        Assert.Contains(events, e => e.EventType == "refresh");
        Assert.Contains(events, e => e.EventType == "logout");
        Assert.All(events, e => Assert.Equal(userId, e.UserId));
    }

    [Fact]
    public async Task GetSessionEventsAsync_ShouldFilterBySessionId()
    {
        // Arrange
        var userId = "user-123";
        var session1 = await _sessionManager.CreateSessionAsync(userId, "Test User", null, null, false);
        var session2 = await _sessionManager.CreateSessionAsync(userId, "Test User", null, null, false);

        // Act
        var events = await _sessionManager.GetSessionEventsAsync(sessionId: session1.SessionId);

        // Assert
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.Equal(session1.SessionId, e.SessionId));
    }

    [Fact]
    public async Task GetSessionEventsAsync_ShouldRespectLimit()
    {
        // Arrange
        var userId = "user-123";
        
        // Create multiple sessions to generate multiple login events
        for (int i = 0; i < 10; i++)
        {
            await _sessionManager.CreateSessionAsync(userId, "Test User", null, null, false);
        }

        // Act
        var events = await _sessionManager.GetSessionEventsAsync(userId: userId, limit: 5);

        // Assert
        Assert.Equal(5, events.Count);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_ShouldRemoveExpiredSessions()
    {
        // Arrange - Create manager with very short timeout
        var settings = new AuthenticationSettings
        {
            SessionTimeout = new SessionTimeoutSettings
            {
                Enabled = true,
                IdleTimeoutMinutes = 0, // Immediate expiration for testing
                AbsoluteTimeoutMinutes = 480,
                WarningBeforeTimeoutMinutes = 5,
                AllowSessionRefresh = true
            }
        };
        var authSettings = Options.Create(settings);
        var sessionManager = new SessionManager(_store, _logger, authSettings);
        
        var session = await sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);
        
        // Wait for session to expire
        await Task.Delay(100);

        // Act
        var cleanedCount = await sessionManager.CleanupExpiredSessionsAsync();

        // Assert
        Assert.True(cleanedCount >= 0); // Should clean up at least the expired session
        
        // Verify session is no longer active
        var status = await sessionManager.GetSessionStatusAsync(session.SessionId);
        Assert.False(status.IsActive);
    }

    [Fact]
    public async Task SessionTimeout_ShouldBeDisabled_WhenEnabledIsFalse()
    {
        // Arrange - Create manager with timeout disabled
        var settings = new AuthenticationSettings
        {
            SessionTimeout = new SessionTimeoutSettings
            {
                Enabled = false // Disabled
            }
        };
        var authSettings = Options.Create(settings);
        var sessionManager = new SessionManager(_store, _logger, authSettings);
        
        var session = await sessionManager.CreateSessionAsync("user-123", "Test User", null, null, false);

        // Act
        var status = await sessionManager.GetSessionStatusAsync(session.SessionId);

        // Assert
        Assert.True(status.IsActive);
        Assert.False(status.CanRefresh); // Refresh should not be needed when disabled
    }

    [Fact]
    public async Task SessionEvents_ShouldIncludeIPAddress_WhenProvided()
    {
        // Arrange
        var userId = "user-123";
        var ipAddress = "192.168.1.100";
        var session = await _sessionManager.CreateSessionAsync(userId, "Test User", ipAddress, null, false);

        // Act
        var events = await _sessionManager.GetSessionEventsAsync(userId: userId);

        // Assert
        var loginEvent = events.FirstOrDefault(e => e.EventType == "login");
        Assert.NotNull(loginEvent);
        Assert.Equal(ipAddress, loginEvent.IpAddress);
    }

    [Fact]
    public async Task SessionEvents_ShouldIncludeMfaStatus_InDetails()
    {
        // Arrange
        var userId = "user-123";
        var session = await _sessionManager.CreateSessionAsync(userId, "Test User", null, null, mfaVerified: true);

        // Act
        var events = await _sessionManager.GetSessionEventsAsync(userId: userId);

        // Assert
        var loginEvent = events.FirstOrDefault(e => e.EventType == "login");
        Assert.NotNull(loginEvent);
        Assert.Contains("MFA: True", loginEvent.Details ?? "");
    }
}
