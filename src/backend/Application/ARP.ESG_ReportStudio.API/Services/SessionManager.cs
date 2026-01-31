using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Reporting;
using Microsoft.Extensions.Options;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for managing user sessions and timeout handling.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates a new session for a user.
    /// </summary>
    Task<UserSession> CreateSessionAsync(string userId, string userName, string? ipAddress, string? userAgent, bool mfaVerified);
    
    /// <summary>
    /// Updates the last activity time for a session.
    /// </summary>
    Task<bool> UpdateActivityAsync(string sessionId);
    
    /// <summary>
    /// Gets the current session status.
    /// </summary>
    Task<SessionStatusResponse> GetSessionStatusAsync(string sessionId);
    
    /// <summary>
    /// Refreshes a session, extending its expiration.
    /// </summary>
    Task<bool> RefreshSessionAsync(string sessionId);
    
    /// <summary>
    /// Ends a session (logout).
    /// </summary>
    Task EndSessionAsync(string sessionId, string reason);
    
    /// <summary>
    /// Checks for expired sessions and invalidates them.
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync();
    
    /// <summary>
    /// Gets session activity events for a user or session.
    /// </summary>
    Task<List<SessionActivityEvent>> GetSessionEventsAsync(string? userId = null, string? sessionId = null, int limit = 100);
}

/// <summary>
/// Implementation of session management service.
/// </summary>
public sealed class SessionManager : ISessionManager
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<SessionManager> _logger;
    private readonly SessionTimeoutSettings _settings;

    // In-memory storage for sessions (in production, use Redis or distributed cache)
    private readonly Dictionary<string, UserSession> _sessions = new();
    private readonly List<SessionActivityEvent> _sessionEvents = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Maximum number of session events to retain in memory.
    /// Events older than this limit are removed to prevent unbounded memory growth.
    /// 10,000 events should cover several days of typical usage.
    /// </summary>
    private const int MaxSessionEventsInMemory = 10000;

    public SessionManager(
        InMemoryReportStore store,
        ILogger<SessionManager> logger,
        IOptions<AuthenticationSettings> authSettings)
    {
        _store = store;
        _logger = logger;
        _settings = authSettings.Value.SessionTimeout;
    }

    public Task<UserSession> CreateSessionAsync(
        string userId, 
        string userName, 
        string? ipAddress, 
        string? userAgent, 
        bool mfaVerified)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        var sessionId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_settings.IdleTimeoutMinutes);

        var session = new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            UserName = userName,
            CreatedAt = now.ToString("O"),
            LastActivityAt = now.ToString("O"),
            ExpiresAt = expiresAt.ToString("O"),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            MfaVerified = mfaVerified
        };

        lock (_lock)
        {
            _sessions[sessionId] = session;
            
            // Log session creation event
            var evt = new SessionActivityEvent
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                EventType = "login",
                Timestamp = now.ToString("O"),
                IpAddress = ipAddress,
                Details = $"Session created with MFA: {mfaVerified}"
            };
            _sessionEvents.Add(evt);
        }

        _logger.LogInformation(
            "Session created for user {UserId} (Session: {SessionId}, MFA: {MfaVerified})",
            userId, sessionId, mfaVerified);

        return Task.FromResult(session);
    }

    public Task<bool> UpdateActivityAsync(string sessionId)
    {
        if (!_settings.Enabled)
        {
            return Task.FromResult(true);
        }

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(false);
            }

            // Check if session has already expired
            if (DateTime.TryParse(session.ExpiresAt, out var expiresAt) && expiresAt < DateTime.UtcNow)
            {
                // Session expired, remove it
                _sessions.Remove(sessionId);
                LogSessionEvent(session, "timeout", "Session expired due to inactivity");
                _logger.LogWarning("Session {SessionId} for user {UserId} timed out", sessionId, session.UserId);
                return Task.FromResult(false);
            }

            // Check absolute timeout
            if (DateTime.TryParse(session.CreatedAt, out var createdAt))
            {
                var absoluteExpiry = createdAt.AddMinutes(_settings.AbsoluteTimeoutMinutes);
                if (absoluteExpiry < DateTime.UtcNow)
                {
                    _sessions.Remove(sessionId);
                    LogSessionEvent(session, "timeout", "Session expired due to absolute timeout");
                    _logger.LogWarning(
                        "Session {SessionId} for user {UserId} reached absolute timeout",
                        sessionId, session.UserId);
                    return Task.FromResult(false);
                }
            }

            // Update activity
            var now = DateTime.UtcNow;
            session.LastActivityAt = now.ToString("O");
            session.ExpiresAt = now.AddMinutes(_settings.IdleTimeoutMinutes).ToString("O");
        }

        return Task.FromResult(true);
    }

    public Task<SessionStatusResponse> GetSessionStatusAsync(string sessionId)
    {
        if (!_settings.Enabled)
        {
            return Task.FromResult(new SessionStatusResponse
            {
                IsActive = true,
                CanRefresh = false,
                ShouldWarn = false
            });
        }

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(new SessionStatusResponse
                {
                    IsActive = false,
                    CanRefresh = false,
                    ShouldWarn = false
                });
            }

            if (!DateTime.TryParse(session.ExpiresAt, out var expiresAt))
            {
                return Task.FromResult(new SessionStatusResponse
                {
                    IsActive = false,
                    CanRefresh = false,
                    ShouldWarn = false
                });
            }

            var now = DateTime.UtcNow;
            var minutesUntilExpiration = (int)(expiresAt - now).TotalMinutes;

            if (minutesUntilExpiration <= 0)
            {
                // Session expired
                _sessions.Remove(sessionId);
                LogSessionEvent(session, "timeout", "Session expired");
                return Task.FromResult(new SessionStatusResponse
                {
                    IsActive = false,
                    CanRefresh = false,
                    ShouldWarn = false
                });
            }

            return Task.FromResult(new SessionStatusResponse
            {
                IsActive = true,
                SessionId = sessionId,
                ExpiresAt = expiresAt.ToString("O"),
                MinutesUntilExpiration = minutesUntilExpiration,
                ShouldWarn = minutesUntilExpiration <= _settings.WarningBeforeTimeoutMinutes,
                CanRefresh = _settings.AllowSessionRefresh
            });
        }
    }

    public Task<bool> RefreshSessionAsync(string sessionId)
    {
        if (!_settings.Enabled || !_settings.AllowSessionRefresh)
        {
            return Task.FromResult(false);
        }

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(false);
            }

            // Check absolute timeout - cannot refresh if absolute timeout reached
            if (DateTime.TryParse(session.CreatedAt, out var createdAt))
            {
                var absoluteExpiry = createdAt.AddMinutes(_settings.AbsoluteTimeoutMinutes);
                if (absoluteExpiry < DateTime.UtcNow)
                {
                    _sessions.Remove(sessionId);
                    LogSessionEvent(session, "timeout", "Cannot refresh - absolute timeout reached");
                    return Task.FromResult(false);
                }
            }

            var now = DateTime.UtcNow;
            session.LastActivityAt = now.ToString("O");
            session.ExpiresAt = now.AddMinutes(_settings.IdleTimeoutMinutes).ToString("O");

            LogSessionEvent(session, "refresh", "Session refreshed by user");
            _logger.LogInformation("Session {SessionId} for user {UserId} refreshed", sessionId, session.UserId);
        }

        return Task.FromResult(true);
    }

    public Task EndSessionAsync(string sessionId, string reason)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                _sessions.Remove(sessionId);
                LogSessionEvent(session, "logout", reason);
                _logger.LogInformation(
                    "Session {SessionId} for user {UserId} ended: {Reason}",
                    sessionId, session.UserId, reason);
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> CleanupExpiredSessionsAsync()
    {
        if (!_settings.Enabled)
        {
            return Task.FromResult(0);
        }

        var expiredCount = 0;
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            var expiredSessions = _sessions.Values
                .Where(s =>
                {
                    // Check idle timeout
                    if (DateTime.TryParse(s.ExpiresAt, out var expiresAt) && expiresAt < now)
                    {
                        return true;
                    }

                    // Check absolute timeout
                    if (DateTime.TryParse(s.CreatedAt, out var createdAt))
                    {
                        var absoluteExpiry = createdAt.AddMinutes(_settings.AbsoluteTimeoutMinutes);
                        if (absoluteExpiry < now)
                        {
                            return true;
                        }
                    }

                    return false;
                })
                .ToList();

            foreach (var session in expiredSessions)
            {
                _sessions.Remove(session.SessionId);
                LogSessionEvent(session, "timeout", "Session cleaned up due to inactivity");
                expiredCount++;
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredCount);
        }

        return Task.FromResult(expiredCount);
    }

    public Task<List<SessionActivityEvent>> GetSessionEventsAsync(
        string? userId = null, 
        string? sessionId = null, 
        int limit = 100)
    {
        lock (_lock)
        {
            var query = _sessionEvents.AsEnumerable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(e => e.SessionId == sessionId);
            }

            var events = query
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToList();

            return Task.FromResult(events);
        }
    }

    private void LogSessionEvent(UserSession session, string eventType, string? details)
    {
        var evt = new SessionActivityEvent
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.SessionId,
            UserId = session.UserId,
            UserName = session.UserName,
            EventType = eventType,
            Timestamp = DateTime.UtcNow.ToString("O"),
            IpAddress = session.IpAddress,
            Details = details
        };

        lock (_lock)
        {
            _sessionEvents.Add(evt);
            
            // Keep only recent events to prevent unbounded memory growth
            // Remove oldest events when limit is reached
            if (_sessionEvents.Count > MaxSessionEventsInMemory)
            {
                _sessionEvents.RemoveRange(0, _sessionEvents.Count - MaxSessionEventsInMemory);
            }
        }
    }
}
