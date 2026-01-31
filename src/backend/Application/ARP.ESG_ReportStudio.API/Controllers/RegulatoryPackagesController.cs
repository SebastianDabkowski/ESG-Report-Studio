using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing regulatory compliance packages.
/// Provides controlled mechanism to add new regulatory requirements (disclosures, validations, workflows).
/// </summary>
[ApiController]
[Route("api/regulatory-packages")]
public sealed class RegulatoryPackagesController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public RegulatoryPackagesController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all regulatory packages.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<RegulatoryPackage>> GetRegulatoryPackages()
    {
        return Ok(_store.GetRegulatoryPackages());
    }

    /// <summary>
    /// Get a specific regulatory package by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<RegulatoryPackage> GetRegulatoryPackage(string id)
    {
        var package = _store.GetRegulatoryPackage(id);
        if (package == null)
        {
            return NotFound(new { error = $"Regulatory package with ID '{id}' not found." });
        }

        return Ok(package);
    }

    /// <summary>
    /// Create a new regulatory package.
    /// </summary>
    [HttpPost]
    public ActionResult<RegulatoryPackage> CreateRegulatoryPackage([FromBody] CreateRegulatoryPackageRequest request)
    {
        var (isValid, errorMessage, package) = _store.CreateRegulatoryPackage(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetRegulatoryPackage), new { id = package!.Id }, package);
    }

    /// <summary>
    /// Update an existing regulatory package.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<RegulatoryPackage> UpdateRegulatoryPackage(string id, [FromBody] UpdateRegulatoryPackageRequest request)
    {
        var (isValid, errorMessage, package) = _store.UpdateRegulatoryPackage(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(package);
    }

    /// <summary>
    /// Delete a regulatory package.
    /// Package cannot be deleted if it is currently enabled for any tenant or period.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteRegulatoryPackage(string id)
    {
        var deleted = _store.DeleteRegulatoryPackage(id);
        if (!deleted)
        {
            return BadRequest(new { error = $"Cannot delete package '{id}'. It may not exist or is currently enabled for a tenant/period." });
        }

        return NoContent();
    }

    /// <summary>
    /// Enable a regulatory package for a tenant (organization).
    /// Package must be in 'active' status to be enabled.
    /// </summary>
    [HttpPost("tenant-config")]
    public ActionResult<TenantRegulatoryConfig> EnablePackageForTenant([FromBody] EnablePackageForTenantRequest request)
    {
        var (isValid, errorMessage, config) = _store.EnablePackageForTenant(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(config);
    }

    /// <summary>
    /// Disable a regulatory package for a tenant.
    /// </summary>
    [HttpDelete("tenant-config")]
    public ActionResult DisablePackageForTenant(
        [FromQuery] string organizationId, 
        [FromQuery] string packageId,
        [FromQuery] string disabledBy,
        [FromQuery] string disabledByName)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return BadRequest(new { error = "organizationId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return BadRequest(new { error = "packageId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(disabledBy))
        {
            return BadRequest(new { error = "disabledBy is required." });
        }

        var disabled = _store.DisablePackageForTenant(organizationId, packageId, disabledBy, disabledByName);
        if (!disabled)
        {
            return NotFound(new { error = $"Package '{packageId}' is not enabled for organization '{organizationId}'." });
        }

        return NoContent();
    }

    /// <summary>
    /// Get regulatory packages configured for a tenant.
    /// </summary>
    [HttpGet("tenant-config/{organizationId}")]
    public ActionResult<IReadOnlyList<TenantRegulatoryConfig>> GetTenantRegulatoryConfigs(string organizationId)
    {
        return Ok(_store.GetTenantRegulatoryConfigs(organizationId));
    }

    /// <summary>
    /// Enable a regulatory package for a reporting period.
    /// Package must be enabled for the tenant before enabling for a period.
    /// </summary>
    [HttpPost("period-config")]
    public ActionResult<PeriodRegulatoryConfig> EnablePackageForPeriod([FromBody] EnablePackageForPeriodRequest request)
    {
        var (isValid, errorMessage, config) = _store.EnablePackageForPeriod(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(config);
    }

    /// <summary>
    /// Disable a regulatory package for a period.
    /// Historical compliance results remain available via the validation snapshot.
    /// </summary>
    [HttpDelete("period-config")]
    public ActionResult DisablePackageForPeriod(
        [FromQuery] string periodId, 
        [FromQuery] string packageId,
        [FromQuery] string disabledBy,
        [FromQuery] string disabledByName,
        [FromQuery] string? validationSnapshot)
    {
        if (string.IsNullOrWhiteSpace(periodId))
        {
            return BadRequest(new { error = "periodId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return BadRequest(new { error = "packageId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(disabledBy))
        {
            return BadRequest(new { error = "disabledBy is required." });
        }

        var disabled = _store.DisablePackageForPeriod(periodId, packageId, disabledBy, disabledByName, validationSnapshot);
        if (!disabled)
        {
            return NotFound(new { error = $"Package '{packageId}' is not enabled for period '{periodId}'." });
        }

        return NoContent();
    }

    /// <summary>
    /// Get regulatory packages configured for a reporting period.
    /// </summary>
    [HttpGet("period-config/{periodId}")]
    public ActionResult<IReadOnlyList<PeriodRegulatoryConfig>> GetPeriodRegulatoryConfigs(string periodId)
    {
        return Ok(_store.GetPeriodRegulatoryConfigs(periodId));
    }

    /// <summary>
    /// Get validation rules applicable to a period based on enabled regulatory packages.
    /// This endpoint demonstrates data-driven validation rule application.
    /// </summary>
    [HttpGet("period-config/{periodId}/validation-rules")]
    public ActionResult<IReadOnlyList<ValidationRule>> GetValidationRulesForPeriod(string periodId)
    {
        return Ok(_store.GetValidationRulesForPeriod(periodId));
    }
}
