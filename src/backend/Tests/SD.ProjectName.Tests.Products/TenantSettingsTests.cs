using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products;

public class TenantSettingsTests
{
    private static InMemoryReportStore CreateStoreWithOrg()
    {
        var store = new InMemoryReportStore(new TextDiffService());
        
        // Create organization
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "GmbH",
            Country = "DE",
            Identifier = "org-001",
            CoverageType = "full",
            CoverageJustification = "Testing",
            CreatedBy = "test-user"
        });
        
        // Create organizational unit
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Organization Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });
        
        // Create reporting period
        var snapshot = store.GetSnapshot();
        store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Reporting Period",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-001",
            OwnerName = "Test User",
            OrganizationId = snapshot.Organization!.Id
        });
        
        return store;
    }

    [Fact]
    public void GetTenantSettings_WithNoExistingSettings_ShouldCreateDefaults()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;

        // Act
        var settings = store.GetTenantSettings(orgId);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(orgId, settings.OrganizationId);
        Assert.Empty(settings.EnabledIntegrations);
        Assert.Empty(settings.EnabledStandards);
        Assert.Equal(1, settings.Version);
        Assert.Equal("system", settings.CreatedBy);
    }

    [Fact]
    public void UpdateTenantSettings_WithValidData_ShouldSucceed()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR", "Finance" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User",
            ChangeReason = "Initial configuration"
        };

        // Act
        var (success, errorMessage, settings) = store.UpdateTenantSettings(orgId, request);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.NotNull(settings);
        Assert.Equal(2, settings!.EnabledIntegrations.Count);
        Assert.Contains("HR", settings.EnabledIntegrations);
        Assert.Contains("Finance", settings.EnabledIntegrations);
        Assert.Equal("admin-001", settings.UpdatedBy);
        Assert.Equal("Admin User", settings.UpdatedByName);
    }

    [Fact]
    public void UpdateTenantSettings_WithStandards_ShouldSucceed()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        // Get a valid standard ID from the catalog
        var standards = store.GetStandardsCatalog(includeDeprecated: false);
        var standardId = standards.First().Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR" },
            EnabledStandards = new List<string> { standardId },
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (success, errorMessage, settings) = store.UpdateTenantSettings(orgId, request);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.NotNull(settings);
        Assert.Single(settings!.EnabledStandards);
        Assert.Contains(standardId, settings.EnabledStandards);
    }

    [Fact]
    public void UpdateTenantSettings_WithInvalidStandard_ShouldFail()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string>(),
            EnabledStandards = new List<string> { "invalid-standard-id" },
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (success, errorMessage, settings) = store.UpdateTenantSettings(orgId, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("not found", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(settings);
    }

    [Fact]
    public void UpdateTenantSettings_WithInvalidIntegration_ShouldFail()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "InvalidIntegration" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (success, errorMessage, settings) = store.UpdateTenantSettings(orgId, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("Invalid integration type", errorMessage);
        Assert.Null(settings);
    }

    [Fact]
    public void UpdateTenantSettings_ShouldIncrementVersion()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request1 = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        var request2 = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR", "Finance" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (_, _, settings1) = store.UpdateTenantSettings(orgId, request1);
        var initialVersion = settings1!.Version;  // Capture whatever version we start with
        
        var (_, _, settings2) = store.UpdateTenantSettings(orgId, request2);

        // Assert
        // Version should increment by 1 from first update to second
        Assert.Equal(initialVersion + 1, settings2!.Version);
    }

    [Fact]
    public void UpdateTenantSettings_ShouldCreateHistoryEntry()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request1 = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User",
            ChangeReason = "Initial setup"
        };

        var request2 = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR", "Finance" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-002",
            UpdatedByName = "Another Admin",
            ChangeReason = "Added Finance integration"
        };

        // Act
        store.UpdateTenantSettings(orgId, request1);
        store.UpdateTenantSettings(orgId, request2);
        var history = store.GetTenantSettingsHistory(orgId);

        // Assert
        Assert.Single(history); // Only one history entry (for first update before second)
        var historyEntry = history.First();
        Assert.Equal(1, historyEntry.Version);
        Assert.Single(historyEntry.EnabledIntegrations);
        Assert.Contains("HR", historyEntry.EnabledIntegrations);
        Assert.Equal("admin-002", historyEntry.ChangedBy);
        Assert.Equal("Added Finance integration", historyEntry.ChangeReason);
    }

    [Fact]
    public void UpdateTenantSettings_WithApplyImmediately_ShouldSetEffectiveDateToNow()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        var beforeUpdate = DateTime.UtcNow;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (_, _, settings) = store.UpdateTenantSettings(orgId, request);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(settings);
        var effectiveDate = DateTime.Parse(settings!.EffectiveDate);
        Assert.True(effectiveDate >= beforeUpdate && effectiveDate <= afterUpdate);
    }

    [Fact]
    public void IsIntegrationEnabled_WithEnabledIntegration_ShouldReturnTrue()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR", "Finance" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        store.UpdateTenantSettings(orgId, request);

        // Act
        var isHREnabled = store.IsIntegrationEnabled(orgId, "HR");
        var isFinanceEnabled = store.IsIntegrationEnabled(orgId, "Finance");
        var isUtilitiesEnabled = store.IsIntegrationEnabled(orgId, "Utilities");

        // Assert
        Assert.True(isHREnabled);
        Assert.True(isFinanceEnabled);
        Assert.False(isUtilitiesEnabled);
    }

    [Fact]
    public void IsStandardEnabled_WithEnabledStandard_ShouldReturnTrue()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var snapshot = store.GetSnapshot();
        var orgId = snapshot.Organization!.Id;
        
        var standards = store.GetStandardsCatalog(includeDeprecated: false);
        var standardId = standards.First().Id;
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string>(),
            EnabledStandards = new List<string> { standardId },
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        store.UpdateTenantSettings(orgId, request);

        // Act
        var isEnabled = store.IsStandardEnabled(orgId, standardId);
        var isOtherEnabled = store.IsStandardEnabled(orgId, "non-existent-id");

        // Assert
        Assert.True(isEnabled);
        Assert.False(isOtherEnabled);
    }

    [Fact]
    public void UpdateTenantSettings_WithInvalidOrganization_ShouldFail()
    {
        // Arrange
        var store = CreateStoreWithOrg();
        var invalidOrgId = "invalid-org-id";
        
        var request = new UpdateTenantSettingsRequest
        {
            EnabledIntegrations = new List<string> { "HR" },
            EnabledStandards = new List<string>(),
            ApplyImmediately = true,
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };

        // Act
        var (success, errorMessage, settings) = store.UpdateTenantSettings(invalidOrgId, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("Organization not found", errorMessage);
        Assert.Null(settings);
    }
}
