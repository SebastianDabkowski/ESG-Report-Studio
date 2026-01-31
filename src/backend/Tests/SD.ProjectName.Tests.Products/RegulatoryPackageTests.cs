using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products;

public class RegulatoryPackageTests
{
    private static InMemoryReportStore CreateStore()
    {
        return new InMemoryReportStore(new TextDiffService());
    }

    [Fact]
    public void CreateRegulatoryPackage_WithValidData_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var request = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD/ESRS 2024",
            Description = "Corporate Sustainability Reporting Directive with European Sustainability Reporting Standards",
            Version = "1.0",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User",
            RequiredSections = new List<string> { "ENV-001", "SOC-001" },
            ValidationRuleIds = new List<string> { "vr-001", "vr-002" }
        };

        // Act
        var (isValid, errorMessage, package) = store.CreateRegulatoryPackage(request);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(package);
        Assert.Equal("CSRD/ESRS 2024", package!.Name);
        Assert.Equal("1.0", package.Version);
        Assert.Equal("draft", package.Status); // New packages start as draft
        Assert.Equal(2, package.RequiredSections.Count);
        Assert.Equal(2, package.ValidationRuleIds.Count);
    }

    [Fact]
    public void CreateRegulatoryPackage_WithoutName_ShouldFail()
    {
        // Arrange
        var store = CreateStore();
        var request = new CreateRegulatoryPackageRequest
        {
            Name = "",
            Version = "1.0",
            CreatedBy = "admin-001"
        };

        // Act
        var (isValid, errorMessage, package) = store.CreateRegulatoryPackage(request);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("name is required", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(package);
    }

    [Fact]
    public void UpdateRegulatoryPackage_ToActiveStatus_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "GRI Standards",
            Version = "2021",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);

        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = "GRI Universal Standards",
            Version = "2021.1",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User",
            RequiredSections = new List<string> { "ENV-001" },
            ValidationRuleIds = new List<string> { "vr-003" }
        };

        // Act
        var (isValid, errorMessage, package) = store.UpdateRegulatoryPackage(createdPackage!.Id, updateRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(package);
        Assert.Equal("active", package!.Status);
        Assert.Equal("GRI Universal Standards", package.Name);
    }

    [Fact]
    public void EnablePackageForTenant_WithActivePackage_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        
        // Create and activate a package
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "TCFD",
            Version = "2023",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);
        
        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = "TCFD",
            Version = "2023",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };
        store.UpdateRegulatoryPackage(createdPackage!.Id, updateRequest);

        // Create organization
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        
        var org = store.GetOrganization();

        // Act
        var enableRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        var (isValid, errorMessage, config) = store.EnablePackageForTenant(enableRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(config);
        Assert.Equal(org!.Id, config!.OrganizationId);
        Assert.Equal(createdPackage.Id, config.PackageId);
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public void EnablePackageForTenant_WithDraftPackage_ShouldFail()
    {
        // Arrange
        var store = CreateStore();
        
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "Draft Package",
            Version = "0.1",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);

        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();

        // Act
        var enableRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = createdPackage!.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        var (isValid, errorMessage, config) = store.EnablePackageForTenant(enableRequest);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("must be active", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(config);
    }

    [Fact]
    public void EnablePackageForPeriod_WhenEnabledForTenant_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        
        // Create and activate package
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);
        
        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };
        store.UpdateRegulatoryPackage(createdPackage!.Id, updateRequest);

        // Create organization and period
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default unit for testing",
            CreatedBy = "admin-001"
        });
        
        var (_, _, periodSnapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest 
        { 
            OrganizationId = org!.Id,
            Name = "2024", 
            StartDate = "2024-01-01", 
            EndDate = "2024-12-31", 
            ReportingMode = "simplified", 
            ReportScope = "single-company", 
            OwnerId = "user-001", 
            OwnerName = "User One" 
        });
        var period = periodSnapshot!.Periods[0];

        // Enable for tenant first
        var tenantRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        store.EnablePackageForTenant(tenantRequest);

        // Act - Enable for period
        var periodRequest = new EnablePackageForPeriodRequest
        {
            PeriodId = period.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "user-001",
            EnabledByName = "User One"
        };
        var (isValid, errorMessage, config) = store.EnablePackageForPeriod(periodRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(config);
        Assert.Equal(period.Id, config!.PeriodId);
        Assert.Equal(createdPackage.Id, config.PackageId);
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public void EnablePackageForPeriod_WhenNotEnabledForTenant_ShouldFail()
    {
        // Arrange
        var store = CreateStore();
        
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);

        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default unit for testing",
            CreatedBy = "admin-001"
        });
        
        var (_, _, periodSnapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest 
        { 
            OrganizationId = org!.Id,
            Name = "2024", 
            StartDate = "2024-01-01", 
            EndDate = "2024-12-31", 
            ReportingMode = "simplified", 
            ReportScope = "single-company", 
            OwnerId = "user-001", 
            OwnerName = "User One" 
        });
        var period = periodSnapshot!.Periods[0];

        // Act - Try to enable for period without tenant enablement
        var periodRequest = new EnablePackageForPeriodRequest
        {
            PeriodId = period.Id,
            PackageId = createdPackage!.Id,
            EnabledBy = "user-001",
            EnabledByName = "User One"
        };
        var (isValid, errorMessage, config) = store.EnablePackageForPeriod(periodRequest);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("must be enabled for tenant", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(config);
    }

    [Fact]
    public void DisablePackageForPeriod_ShouldPreserveValidationSnapshot()
    {
        // Arrange
        var store = CreateStore();
        
        // Create and enable package
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);
        
        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };
        store.UpdateRegulatoryPackage(createdPackage!.Id, updateRequest);

        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default unit for testing",
            CreatedBy = "admin-001"
        });
        
        var (_, _, periodSnapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest 
        { 
            OrganizationId = org!.Id,
            Name = "2024", 
            StartDate = "2024-01-01", 
            EndDate = "2024-12-31", 
            ReportingMode = "simplified", 
            ReportScope = "single-company", 
            OwnerId = "user-001", 
            OwnerName = "User One" 
        });
        var period = periodSnapshot!.Periods[0];

        var tenantRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        store.EnablePackageForTenant(tenantRequest);

        var periodRequest = new EnablePackageForPeriodRequest
        {
            PeriodId = period.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "user-001",
            EnabledByName = "User One"
        };
        store.EnablePackageForPeriod(periodRequest);

        // Act - Disable with validation snapshot
        var snapshot = "{\"validationResults\": [\"passed\"], \"timestamp\": \"2024-12-31T23:59:59Z\"}";
        var disabled = store.DisablePackageForPeriod(
            period.Id, 
            createdPackage.Id, 
            "admin-001", 
            "Admin User", 
            snapshot);

        // Assert
        Assert.True(disabled);
        var configs = store.GetPeriodRegulatoryConfigs(period.Id);
        var config = configs.FirstOrDefault(c => c.PackageId == createdPackage.Id);
        Assert.NotNull(config);
        Assert.False(config!.IsEnabled);
        Assert.NotNull(config.DisabledAt);
        Assert.Equal(snapshot, config.ValidationSnapshot);
    }

    [Fact]
    public void GetValidationRulesForPeriod_ShouldReturnOnlyEnabledPackageRules()
    {
        // Arrange
        var store = CreateStore();
        
        // Create organization and period first
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default unit for testing",
            CreatedBy = "admin-001"
        });
        
        var (_, _, periodSnapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest 
        { 
            OrganizationId = org!.Id,
            Name = "2024", 
            StartDate = "2024-01-01", 
            EndDate = "2024-12-31", 
            ReportingMode = "simplified", 
            ReportScope = "single-company", 
            OwnerId = "user-001", 
            OwnerName = "User One" 
        });
        var period = periodSnapshot!.Periods[0];
        
        // Get sections created by the period
        var sections = store.GetSections(period.Id);
        Assert.NotEmpty(sections);
        var section1 = sections[0];
        var section2 = sections.Count > 1 ? sections[1] : sections[0];
        
        // Create validation rules using actual section IDs
        var rule1Request = new CreateValidationRuleRequest
        {
            SectionId = section1.Id,
            RuleType = "non-negative",
            ErrorMessage = "Value must be non-negative",
            CreatedBy = "admin-001"
        };
        var (rule1Valid, rule1Error, rule1) = store.CreateValidationRule(rule1Request);
        Assert.True(rule1Valid, $"Failed to create rule1: {rule1Error}");
        Assert.NotNull(rule1);
        
        var rule2Request = new CreateValidationRuleRequest
        {
            SectionId = section2.Id,
            RuleType = "required-unit",
            ErrorMessage = "Unit is required",
            CreatedBy = "admin-001"
        };
        var (_, _, rule2) = store.CreateValidationRule(rule2Request);

        // Create packages
        var package1Request = new CreateRegulatoryPackageRequest
        {
            Name = "Package 1",
            Version = "1.0",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User",
            ValidationRuleIds = new List<string> { rule1!.Id }
        };
        var (_, _, package1) = store.CreateRegulatoryPackage(package1Request);
        
        var package2Request = new CreateRegulatoryPackageRequest
        {
            Name = "Package 2",
            Version = "1.0",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User",
            ValidationRuleIds = new List<string> { rule2!.Id }
        };
        var (_, _, package2) = store.CreateRegulatoryPackage(package2Request);

        // Activate packages
        var activate1 = new UpdateRegulatoryPackageRequest
        {
            Name = "Package 1",
            Version = "1.0",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User",
            ValidationRuleIds = new List<string> { rule1!.Id }
        };
        store.UpdateRegulatoryPackage(package1!.Id, activate1);
        
        var activate2 = new UpdateRegulatoryPackageRequest
        {
            Name = "Package 2",
            Version = "1.0",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User",
            ValidationRuleIds = new List<string> { rule2!.Id }
        };
        store.UpdateRegulatoryPackage(package2!.Id, activate2);

        // Enable only package1 for tenant and period
        var tenantRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = package1.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        store.EnablePackageForTenant(tenantRequest);

        var periodRequest = new EnablePackageForPeriodRequest
        {
            PeriodId = period.Id,
            PackageId = package1.Id,
            EnabledBy = "user-001",
            EnabledByName = "User One"
        };
        store.EnablePackageForPeriod(periodRequest);

        // Act
        var rules = store.GetValidationRulesForPeriod(period.Id);

        // Assert
        Assert.Single(rules);
        Assert.Equal(rule1!.Id, rules[0].Id);
        Assert.DoesNotContain(rules, r => r.Id == rule2!.Id);
    }

    [Fact]
    public void DeleteRegulatoryPackage_WhenEnabledForTenant_ShouldFail()
    {
        // Arrange
        var store = CreateStore();
        
        var createRequest = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            CreatedBy = "admin-001",
            CreatedByName = "Admin User"
        };
        var (_, _, createdPackage) = store.CreateRegulatoryPackage(createRequest);
        
        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = "CSRD",
            Version = "2024",
            Status = "active",
            UpdatedBy = "admin-001",
            UpdatedByName = "Admin User"
        };
        store.UpdateRegulatoryPackage(createdPackage!.Id, updateRequest);

        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corp",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "123456"
        });
        var org = store.GetOrganization();

        var tenantRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org!.Id,
            PackageId = createdPackage.Id,
            EnabledBy = "admin-001",
            EnabledByName = "Admin User"
        };
        store.EnablePackageForTenant(tenantRequest);

        // Act
        var deleted = store.DeleteRegulatoryPackage(createdPackage.Id);

        // Assert
        Assert.False(deleted);
    }
}
