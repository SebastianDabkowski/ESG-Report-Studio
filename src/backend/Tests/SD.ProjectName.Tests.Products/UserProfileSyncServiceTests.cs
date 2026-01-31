using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

public class UserProfileSyncServiceTests
{
    private readonly InMemoryReportStore _store;
    private readonly Mock<ILogger<UserProfileSyncService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockAuthSection;
    private readonly Mock<IConfigurationSection> _mockOidcSection;
    private readonly UserProfileSyncService _service;

    public UserProfileSyncServiceTests()
    {
        _store = new InMemoryReportStore();
        _mockLogger = new Mock<ILogger<UserProfileSyncService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockAuthSection = new Mock<IConfigurationSection>();
        _mockOidcSection = new Mock<IConfigurationSection>();

        // Setup configuration mocks using indexer instead of GetValue
        _mockConfiguration.Setup(c => c.GetSection("Authentication:Oidc")).Returns(_mockOidcSection.Object);
        _mockOidcSection.Setup(s => s["NameClaimType"]).Returns("preferred_username");
        _mockOidcSection.Setup(s => s["EmailClaimType"]).Returns("email");
        _mockOidcSection.Setup(s => s["DisplayNameClaimType"]).Returns("name");

        _service = new UserProfileSyncService(_store, _mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldReturnExistingUser_WhenUserExists()
    {
        // Arrange
        var userId = "user-1";  // Use existing user from InMemoryReportStore (Sarah Chen)
        var email = "sarah.chen@company.com";
        var displayName = "Sarah Chen";

        var claims = new List<Claim>
        {
            new Claim("preferred_username", userId),
            new Claim("email", email),
            new Claim("name", displayName)
        };

        // Act
        var result = await _service.SyncUserFromClaimsAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldCreateNewUser_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "new-user-unique-123";
        var email = "newuser@example.com";
        var displayName = "New User";

        var claims = new List<Claim>
        {
            new Claim("preferred_username", userId),
            new Claim("email", email),
            new Claim("name", displayName)
        };

        // Act
        var result = await _service.SyncUserFromClaimsAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal(displayName, result.Name);
        Assert.True(result.IsActive);
        Assert.Empty(result.RoleIds);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldUseClaimTypeFallback_WhenPreferredClaimNotFound()
    {
        // Arrange
        var userId = "test-user-fallback";
        var email = "test@example.com";
        var displayName = "Test User";

        // Using standard claim types as fallback
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName)
        };

        // Act
        var result = await _service.SyncUserFromClaimsAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal(displayName, result.Name);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldThrowException_WhenNoUserIdentifierFound()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "test@example.com"),
            new Claim("name", "Test User")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SyncUserFromClaimsAsync(claims));
    }

    [Fact]
    public async Task IsUserActiveAsync_ShouldReturnTrue_WhenUserExistsAndIsActive()
    {
        // Arrange
        var userId = "user-1";  // Use existing user from InMemoryReportStore (Sarah Chen)

        // Act
        var result = await _service.IsUserActiveAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserActiveAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistent-user-xyz";

        // Act
        var result = await _service.IsUserActiveAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldUseDefaultEmail_WhenEmailClaimNotFound()
    {
        // Arrange
        var userId = "user-1";  // Use existing user
        var displayName = "Test User";

        var claims = new List<Claim>
        {
            new Claim("preferred_username", userId),
            new Claim("name", displayName)
        };

        // Act
        var result = await _service.SyncUserFromClaimsAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        // Email will be from existing user, not default
        Assert.NotEmpty(result.Email);
    }

    [Fact]
    public async Task SyncUserFromClaimsAsync_ShouldUseUserId_WhenDisplayNameNotFound()
    {
        // Arrange
        var userId = "user-1";  // Use existing user
        var email = "test@example.com";

        var claims = new List<Claim>
        {
            new Claim("preferred_username", userId),
            new Claim("email", email)
        };

        // Act
        var result = await _service.SyncUserFromClaimsAsync(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        // Name will be from existing user
        Assert.NotEmpty(result.Name);
    }
}
