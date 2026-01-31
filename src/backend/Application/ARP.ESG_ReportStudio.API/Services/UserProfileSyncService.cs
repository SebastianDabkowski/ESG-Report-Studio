using System.Security.Claims;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for synchronizing user profiles from OIDC claims.
/// </summary>
public interface IUserProfileSyncService
{
    /// <summary>
    /// Synchronizes a user profile from OIDC claims.
    /// Creates the user if they don't exist, updates if they do.
    /// </summary>
    /// <param name="claims">The claims from the OIDC token</param>
    /// <returns>The synchronized user</returns>
    Task<User> SyncUserFromClaimsAsync(IEnumerable<Claim> claims);

    /// <summary>
    /// Checks if a user is active/enabled in the system.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>True if the user is active, false otherwise</returns>
    Task<bool> IsUserActiveAsync(string userId);
}

/// <summary>
/// Implementation of user profile synchronization service.
/// </summary>
public sealed class UserProfileSyncService : IUserProfileSyncService
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<UserProfileSyncService> _logger;
    private readonly string _nameClaimType;
    private readonly string _emailClaimType;
    private readonly string _displayNameClaimType;

    public UserProfileSyncService(
        InMemoryReportStore store,
        ILogger<UserProfileSyncService> logger,
        IConfiguration configuration)
    {
        _store = store;
        _logger = logger;

        // Get claim type mappings from configuration
        var oidcSettings = configuration.GetSection("Authentication:Oidc");
        _nameClaimType = oidcSettings["NameClaimType"] ?? "preferred_username";
        _emailClaimType = oidcSettings["EmailClaimType"] ?? "email";
        _displayNameClaimType = oidcSettings["DisplayNameClaimType"] ?? "name";
    }

    public Task<User> SyncUserFromClaimsAsync(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        
        // Extract user information from claims
        var userId = GetClaimValue(claimsList, _nameClaimType, ClaimTypes.NameIdentifier);
        var email = GetClaimValue(claimsList, _emailClaimType, ClaimTypes.Email);
        var displayName = GetClaimValue(claimsList, _displayNameClaimType, ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("Cannot sync user: no user identifier found in claims");
            throw new InvalidOperationException("No user identifier found in OIDC claims");
        }

        _logger.LogInformation("Syncing user profile for {UserId} from OIDC claims", userId);

        // Check if user exists
        var existingUser = _store.GetUser(userId);
        
        if (existingUser != null)
        {
            // Update existing user if information has changed
            if (existingUser.Email != email || existingUser.Name != displayName)
            {
                _logger.LogInformation("Updating user profile for {UserId}", userId);
                // In a real implementation, we'd update the user in the database
                // For now, we'll work with the in-memory store's limitations
            }
            return Task.FromResult(existingUser);
        }

        // Create new user
        _logger.LogInformation("Creating new user profile for {UserId}", userId);
        var newUser = new User
        {
            Id = userId,
            Email = email ?? $"{userId}@unknown.com",
            Name = displayName ?? userId,
            RoleIds = new List<string>(), // No roles assigned by default
            IsActive = true
        };

        // In a real implementation, we'd save to database here
        // For this demo, the in-memory store is pre-populated with users
        
        return Task.FromResult(newUser);
    }

    public Task<bool> IsUserActiveAsync(string userId)
    {
        var user = _store.GetUser(userId);
        
        // If user doesn't exist or is not active, deny access
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found in system", userId);
            return Task.FromResult(false);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("User {UserId} is disabled", userId);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private static string GetClaimValue(List<Claim> claims, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var claim = claims.FirstOrDefault(c => c.Type == claimType);
            if (claim != null && !string.IsNullOrEmpty(claim.Value))
            {
                return claim.Value;
            }
        }
        return string.Empty;
    }
}
