using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing tenant-level configuration settings.
/// Controls which integrations and reporting standards are enabled for each organization/tenant.
/// All changes are audited and versioned with effective dates.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/tenant-settings")]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public TenantSettingsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get tenant settings for an organization.
    /// Creates default settings if none exist.
    /// </summary>
    /// <remarks>
    /// Permission required: view-tenant-config or admin
    /// </remarks>
    [HttpGet("{organizationId}")]
    public ActionResult<TenantSettings> GetTenantSettings(string organizationId)
    {
        // TODO: Add permission check for view-tenant-config or admin
        // For now, we'll implement basic functionality without auth middleware
        
        var settings = _store.GetTenantSettings(organizationId);
        return Ok(settings);
    }

    /// <summary>
    /// Update tenant settings.
    /// Creates new settings if none exist for the organization.
    /// </summary>
    /// <remarks>
    /// Permission required: edit-tenant-config or admin
    /// Changes are audited and logged automatically.
    /// Effective date can be immediate or set to next reporting period start.
    /// </remarks>
    [HttpPut("{organizationId}")]
    public ActionResult<TenantSettings> UpdateTenantSettings(
        string organizationId, 
        [FromBody] UpdateTenantSettingsRequest request)
    {
        // TODO: Add permission check for edit-tenant-config or admin
        // For now, we'll implement basic functionality without auth middleware
        
        // Validate request
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "UpdatedBy is required" });
        }
        
        if (string.IsNullOrWhiteSpace(request.UpdatedByName))
        {
            return BadRequest(new { error = "UpdatedByName is required" });
        }
        
        var (success, errorMessage, settings) = _store.UpdateTenantSettings(organizationId, request);
        
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(settings);
    }

    /// <summary>
    /// Get historical changes to tenant settings for audit purposes.
    /// </summary>
    /// <remarks>
    /// Permission required: view-tenant-config or admin or auditor
    /// Returns all historical versions ordered by most recent first.
    /// </remarks>
    [HttpGet("{organizationId}/history")]
    public ActionResult<IReadOnlyList<TenantSettingsHistory>> GetTenantSettingsHistory(string organizationId)
    {
        // TODO: Add permission check for view-tenant-config or admin or auditor
        
        var history = _store.GetTenantSettingsHistory(organizationId);
        return Ok(history);
    }

    /// <summary>
    /// Check if a specific integration type is enabled for a tenant.
    /// Takes effective date into account - future changes are not considered active yet.
    /// </summary>
    /// <remarks>
    /// Permission required: view-tenant-config or admin
    /// </remarks>
    [HttpGet("{organizationId}/integrations/{integrationType}/enabled")]
    public ActionResult<bool> IsIntegrationEnabled(string organizationId, string integrationType)
    {
        // TODO: Add permission check for view-tenant-config or admin
        
        var isEnabled = _store.IsIntegrationEnabled(organizationId, integrationType);
        return Ok(new { enabled = isEnabled });
    }

    /// <summary>
    /// Check if a specific reporting standard is enabled for a tenant.
    /// Takes effective date into account - future changes are not considered active yet.
    /// </summary>
    /// <remarks>
    /// Permission required: view-tenant-config or admin
    /// </remarks>
    [HttpGet("{organizationId}/standards/{standardId}/enabled")]
    public ActionResult<bool> IsStandardEnabled(string organizationId, string standardId)
    {
        // TODO: Add permission check for view-tenant-config or admin
        
        var isEnabled = _store.IsStandardEnabled(organizationId, standardId);
        return Ok(new { enabled = isEnabled });
    }
}
