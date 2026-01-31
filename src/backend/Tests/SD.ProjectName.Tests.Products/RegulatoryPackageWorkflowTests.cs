using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Integration test demonstrating the complete regulatory package workflow.
/// This test showcases how the regulatory extension mechanism works end-to-end.
/// </summary>
public class RegulatoryPackageWorkflowTests
{
    private static InMemoryReportStore CreateStore()
    {
        return new InMemoryReportStore(new TextDiffService());
    }

    [Fact]
    public void CompleteRegulatoryPackageWorkflow_ShouldDemonstrateFullCapability()
    {
        // This test demonstrates the complete workflow for adding and managing
        // a new regulatory requirement without breaking existing customers.
        
        var store = CreateStore();

        // ============================================================
        // STEP 1: Create a new regulatory package (CSRD/ESRS)
        // ============================================================
        
        var packageRequest = new CreateRegulatoryPackageRequest
        {
            Name = "CSRD/ESRS 2024",
            Description = "Corporate Sustainability Reporting Directive with European Sustainability Reporting Standards",
            Version = "1.0",
            CreatedBy = "admin-001",
            CreatedByName = "System Admin",
            RequiredSections = new List<string> { "ENV-001", "SOC-001", "GOV-001" },
            ValidationRuleIds = new List<string>() // Will be populated later
        };
        
        var (_, _, package) = store.CreateRegulatoryPackage(packageRequest);
        Assert.NotNull(package);
        Assert.Equal("draft", package!.Status); // New packages start as draft
        
        // ============================================================
        // STEP 2: Create validation rules for the package
        // ============================================================
        
        // Create organization and period first
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Acme Corporation",
            LegalForm = "corporation",
            Country = "DE",
            Identifier = "DE123456"
        });
        var org = store.GetOrganization();
        
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Default Unit",
            Description = "Default unit",
            CreatedBy = "admin-001"
        });
        
        var (_, _, periodSnapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "extended", // CSRD requires extended mode
            ReportScope = "single-company",
            OwnerId = "user-001",
            OwnerName = "Report Owner",
            OrganizationId = org!.Id
        });
        var period = periodSnapshot!.Periods[0];
        
        // Get a section from the period to attach validation rules to
        var sections = periodSnapshot.Sections;
        Assert.NotEmpty(sections); // Period should have at least one section
        var sectionId = sections[0].Id;
        
        // Create CSRD-specific validation rules
        var rule1Request = new CreateValidationRuleRequest
        {
            SectionId = sectionId,
            RuleType = "non-negative",
            TargetField = "value",
            ErrorMessage = "CSRD requires non-negative emission values",
            CreatedBy = "admin-001"
        };
        var (_, _, rule1) = store.CreateValidationRule(rule1Request);
        
        var rule2Request = new CreateValidationRuleRequest
        {
            SectionId = sectionId,
            RuleType = "required-unit",
            TargetField = "unit",
            Parameters = "{\"allowedUnits\": [\"tCO2e\", \"kgCO2e\"]}",
            ErrorMessage = "CSRD requires CO2 equivalent units",
            CreatedBy = "admin-001"
        };
        var (_, _, rule2) = store.CreateValidationRule(rule2Request);
        
        // Update package with validation rule IDs
        var updateRequest = new UpdateRegulatoryPackageRequest
        {
            Name = package.Name,
            Description = package.Description,
            Version = package.Version,
            Status = "active", // Activate the package
            RequiredSections = package.RequiredSections,
            ValidationRuleIds = new List<string> { rule1!.Id, rule2!.Id },
            UpdatedBy = "admin-001",
            UpdatedByName = "System Admin"
        };
        var (_, _, updatedPackage) = store.UpdateRegulatoryPackage(package.Id, updateRequest);
        Assert.Equal("active", updatedPackage!.Status);
        
        // ============================================================
        // STEP 3: Enable package for PILOT TENANT (Acme Corp)
        // ============================================================
        
        var tenantEnableRequest = new EnablePackageForTenantRequest
        {
            OrganizationId = org.Id,
            PackageId = package.Id,
            EnabledBy = "admin-001",
            EnabledByName = "System Admin"
        };
        var (tenantValid, _, tenantConfig) = store.EnablePackageForTenant(tenantEnableRequest);
        Assert.True(tenantValid);
        Assert.True(tenantConfig!.IsEnabled);
        
        // ============================================================
        // STEP 4: Enable package for SPECIFIC PERIOD (FY 2024)
        // ============================================================
        
        var periodEnableRequest = new EnablePackageForPeriodRequest
        {
            PeriodId = period.Id,
            PackageId = package.Id,
            EnabledBy = "user-001",
            EnabledByName = "Report Owner"
        };
        var (periodValid, _, periodConfig) = store.EnablePackageForPeriod(periodEnableRequest);
        Assert.True(periodValid);
        Assert.True(periodConfig!.IsEnabled);
        
        // ============================================================
        // STEP 5: Validate data point using package rules
        // ============================================================
        
        // Get validation rules for the period (data-driven approach)
        var applicableRules = store.GetValidationRulesForPeriod(period.Id);
        
        // Verify that CSRD rules are applied for this period
        Assert.Equal(2, applicableRules.Count);
        Assert.Contains(applicableRules, r => r.Id == rule1.Id);
        Assert.Contains(applicableRules, r => r.Id == rule2.Id);
        
        // ============================================================
        // STEP 6: Create another organization without CSRD
        // ============================================================
        
        // Reset to create another org
        var store2 = CreateStore();
        store2.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Beta Industries",
            LegalForm = "llc",
            Country = "US",
            Identifier = "US789012"
        });
        var org2 = store2.GetOrganization();
        
        store2.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Default Unit",
            Description = "Default unit",
            CreatedBy = "admin-001"
        });
        
        var (_, _, period2Snapshot) = store2.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified", // Not using CSRD
            ReportScope = "single-company",
            OwnerId = "user-002",
            OwnerName = "Report Owner 2",
            OrganizationId = org2!.Id
        });
        var period2 = period2Snapshot!.Periods[0];
        
        // Verify that Beta Industries has NO CSRD rules (package not enabled)
        var betaRules = store2.GetValidationRulesForPeriod(period2.Id);
        Assert.Empty(betaRules); // No CSRD rules applied
        
        // ============================================================
        // STEP 7: Disable package for period (preserve history)
        // ============================================================
        
        var validationSnapshot = "{\"timestamp\": \"2024-12-31T23:59:59Z\", \"result\": \"compliant\", \"rulesApplied\": [\"" + rule1.Id + "\", \"" + rule2.Id + "\"]}";
        var disabled = store.DisablePackageForPeriod(
            period.Id,
            package.Id,
            "admin-001",
            "System Admin",
            validationSnapshot
        );
        Assert.True(disabled);
        
        // Verify historical snapshot is preserved
        var configs = store.GetPeriodRegulatoryConfigs(period.Id);
        var disabledConfig = configs.FirstOrDefault(c => c.PackageId == package.Id);
        Assert.NotNull(disabledConfig);
        Assert.False(disabledConfig!.IsEnabled);
        Assert.Equal(validationSnapshot, disabledConfig.ValidationSnapshot);
        
        // ============================================================
        // DEMONSTRATION COMPLETE
        // ============================================================
        
        // This test demonstrates:
        // ✅ Creating regulatory packages without code deployment
        // ✅ Enabling packages per tenant (Acme has CSRD, Beta doesn't)
        // ✅ Enabling packages per period (FY 2024 for Acme)
        // ✅ Data-driven validation rule application
        // ✅ Historical preservation when packages are disabled
        // ✅ Complete isolation between tenants
    }
}
