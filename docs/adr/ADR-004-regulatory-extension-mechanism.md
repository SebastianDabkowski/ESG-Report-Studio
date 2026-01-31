# ADR-004: Regulatory Extension Mechanism

Status: Accepted  
Date: 2026-01-31  

## Context

ESG Report Studio needs the ability to add new regulatory requirements (disclosures, validations, workflows) in a controlled manner without disrupting existing customers. As new regulations emerge (e.g., CSRD/ESRS updates, GRI Standards revisions, TCFD recommendations), the platform must support their adoption on a tenant-by-tenant and period-by-period basis.

Key requirements:
- New regulatory packages should be configurable and self-contained
- Packages must be enabled/disabled per tenant (organization) and per reporting period
- Validation rules should apply based on configuration at runtime (data-driven)
- Historical compliance results must be preserved when packages are disabled
- The system should prevent breaking changes to existing tenants

## Decision

We will implement a three-tier regulatory package system:

### 1. Regulatory Packages

A `RegulatoryPackage` represents a collection of regulatory requirements:
- **Metadata**: Name, version, description
- **Status**: draft, active, deprecated, superseded (only active packages can be enabled)
- **Required sections**: Catalog codes that must be present
- **Validation rules**: Associated validation rule IDs
- **Metadata**: JSON-encoded package-specific configuration

### 2. Tenant-Level Configuration

A `TenantRegulatoryConfig` controls package availability for an organization:
- Links a package to an organization
- Tracks enablement/disablement history with full audit trail
- Package must be enabled for tenant before it can be used in periods
- Prevents accidental enablement of draft packages (only active packages allowed)

### 3. Period-Level Configuration

A `PeriodRegulatoryConfig` controls package application to specific reporting periods:
- Requires package to be enabled for tenant first (enforces hierarchy)
- Tracks enablement/disablement with audit trail
- **Preserves validation snapshots** when disabled for historical compliance tracking
- Allows different periods to have different regulatory requirements

### 4. Data-Driven Validation

Validation rules are applied dynamically based on period configuration:
- `GetValidationRulesForPeriod(periodId)` returns only rules from enabled packages
- Validation logic remains decoupled from specific regulations
- New rules can be added without code changes
- Rules can be shared across multiple packages

### 5. API Endpoints

New controller `RegulatoryPackagesController` provides:
- CRUD operations for regulatory packages
- Tenant configuration (enable/disable packages for organizations)
- Period configuration (enable/disable packages for periods)
- Query validation rules by period (demonstrates data-driven approach)

## Implementation Details

### Package Lifecycle

1. **Draft**: Package is created but cannot be enabled for tenants
2. **Active**: Package can be enabled for tenants and periods
3. **Deprecated**: Package is no longer recommended but existing enablements remain valid
4. **Superseded**: Package has been replaced by a newer version

### Enablement Hierarchy

```
Organization (Tenant)
  ├─ TenantRegulatoryConfig (enables package for tenant)
  └─ ReportingPeriod
       └─ PeriodRegulatoryConfig (enables package for period)
```

A package **must** be enabled for a tenant before it can be enabled for any period belonging to that tenant.

### Historical Preservation

When a package is disabled for a period:
- The `ValidationSnapshot` field preserves the last validation results
- Historical reports can still show compliance status from when the package was enabled
- Audit trail (EnabledAt, DisabledAt, EnabledBy, DisabledBy) provides full history

### Data-Driven Validation

Instead of hard-coding validation rules in application code:
```csharp
// Get validation rules for a period based on enabled packages
var rules = store.GetValidationRulesForPeriod(periodId);

// Apply rules dynamically
foreach (var rule in rules)
{
    // Execute rule against data point
    ApplyRule(dataPoint, rule);
}
```

This approach allows:
- Adding new regulations without code deployment
- Tenant-specific validation requirements
- Period-specific compliance rules

## Consequences

### Positive

- **Controlled rollout**: New regulations can be tested with pilot tenants before general availability
- **Tenant isolation**: Enabling a package for one tenant doesn't affect others
- **Historical accuracy**: Compliance results are preserved even after package is disabled
- **Flexibility**: Different periods can have different regulatory requirements
- **Scalability**: New regulations are data, not code
- **Auditability**: Full trail of who enabled/disabled packages and when

### Negative

- **Complexity**: Three-tier configuration (package, tenant, period) requires careful management
- **Data volume**: Each tenant-period combination can have multiple package configurations
- **Testing burden**: Validation logic must work correctly with any combination of enabled packages

### Migration Path

Existing validation rules can be:
1. Left as-is (not associated with any package)
2. Grouped into a "baseline" package and automatically enabled for all existing tenants
3. Migrated gradually as regulations are formalized

## Alternatives Considered

### 1. Feature Flags (e.g., LaunchDarkly)

**Rejected** because:
- Doesn't provide tenant-level or period-level granularity
- No built-in historical preservation
- Requires external service dependency
- Harder to audit which tenant had which flags at what time

### 2. Code-Based Conditionals

```csharp
if (tenant.HasFeature("CSRD"))
{
    ApplyCSRDValidation();
}
```

**Rejected** because:
- Requires code deployment for new regulations
- Harder to test all combinations
- Doesn't scale to many regulations
- No historical snapshot capability

### 3. Separate Databases Per Tenant

**Rejected** because:
- Massive operational overhead
- Harder to share reference data
- Complicates reporting and analytics
- Doesn't solve the period-level requirement

## Related Decisions

- ADR-001: Architecture style and layering (regulatory packages follow N-Layer pattern)
- ADR-003: Integration connector framework (similar configuration-driven approach)

## Examples

### Creating and Enabling a Package

```csharp
// 1. Create package (draft status)
var package = store.CreateRegulatoryPackage(new CreateRegulatoryPackageRequest
{
    Name = "CSRD/ESRS 2024",
    Version = "1.0",
    RequiredSections = new[] { "ENV-001", "SOC-001", "GOV-001" },
    ValidationRuleIds = new[] { "vr-csrd-001", "vr-csrd-002" }
});

// 2. Activate package
store.UpdateRegulatoryPackage(package.Id, new UpdateRegulatoryPackageRequest
{
    Status = "active",
    // ... other fields
});

// 3. Enable for tenant
store.EnablePackageForTenant(new EnablePackageForTenantRequest
{
    OrganizationId = "acme-corp",
    PackageId = package.Id,
    EnabledBy = "admin-001"
});

// 4. Enable for specific period
store.EnablePackageForPeriod(new EnablePackageForPeriodRequest
{
    PeriodId = "period-2024",
    PackageId = package.Id,
    EnabledBy = "period-owner"
});

// 5. Validate report
var rules = store.GetValidationRulesForPeriod("period-2024");
// Apply rules...
```

## Future Enhancements

1. **Package Dependencies**: One package can require another (e.g., CSRD base + CSRD amendments)
2. **Version Migrations**: Automatic upgrade paths from one package version to another
3. **Rule Engine**: External rules engine (e.g., Drools) for complex validation logic
4. **Workflow Templates**: Packages can include approval workflow definitions
5. **UI Templates**: Packages can include section/disclosure UI configurations
