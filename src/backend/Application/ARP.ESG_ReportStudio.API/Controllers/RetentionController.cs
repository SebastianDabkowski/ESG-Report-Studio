using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing retention policies and data cleanup operations.
/// Requires admin role for all operations.
/// </summary>
[ApiController]
[Route("api/retention")]
public sealed class RetentionController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public RetentionController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Creates a new retention policy.
    /// Requires admin role.
    /// </summary>
    [HttpPost("policies")]
    public ActionResult<RetentionPolicy> CreateRetentionPolicy(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromBody] CreateRetentionPolicyRequest request)
    {
        // Check authorization
        var authResult = CheckAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        request.CreatedBy = userId;
        var (success, errorMessage, policy) = _store.CreateRetentionPolicy(request);

        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Created($"/api/retention/policies/{policy!.Id}", policy);
    }

    /// <summary>
    /// Gets all retention policies.
    /// Requires admin or auditor role.
    /// </summary>
    [HttpGet("policies")]
    public ActionResult<IReadOnlyList<RetentionPolicy>> GetRetentionPolicies(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromQuery] string? tenantId = null,
        [FromQuery] bool activeOnly = true)
    {
        // Check authorization
        var authResult = CheckAuditorOrAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        var policies = _store.GetRetentionPolicies(tenantId, activeOnly);
        return Ok(policies);
    }

    /// <summary>
    /// Gets the applicable retention policy for a specific context.
    /// Requires admin or auditor role.
    /// </summary>
    [HttpGet("policies/applicable")]
    public ActionResult<RetentionPolicy> GetApplicableRetentionPolicy(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromQuery] string dataCategory,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? reportType = null)
    {
        // Check authorization
        var authResult = CheckAuditorOrAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        var policy = _store.GetApplicableRetentionPolicy(dataCategory, tenantId, reportType);
        if (policy == null)
        {
            return NotFound(new { error = "No applicable retention policy found." });
        }

        return Ok(policy);
    }

    /// <summary>
    /// Updates a retention policy.
    /// Requires admin role.
    /// </summary>
    [HttpPatch("policies/{policyId}")]
    public IActionResult UpdateRetentionPolicy(
        [FromHeader(Name = "X-User-Id")] string userId,
        string policyId,
        [FromBody] UpdateRetentionPolicyRequest request)
    {
        // Check authorization
        var authResult = CheckAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        var (success, errorMessage) = _store.UpdateRetentionPolicy(
            policyId, 
            request.RetentionDays, 
            request.AllowDeletion, 
            userId);

        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Deactivates a retention policy.
    /// Requires admin role.
    /// </summary>
    [HttpDelete("policies/{policyId}")]
    public IActionResult DeactivateRetentionPolicy(
        [FromHeader(Name = "X-User-Id")] string userId,
        string policyId)
    {
        // Check authorization
        var authResult = CheckAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        var (success, errorMessage) = _store.DeactivateRetentionPolicy(policyId, userId);

        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Runs cleanup based on retention policies.
    /// Requires admin role.
    /// </summary>
    [HttpPost("cleanup")]
    public ActionResult<CleanupResult> RunCleanup(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromBody] RunCleanupRequest request)
    {
        // Check authorization
        var authResult = CheckAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        request.InitiatedBy = userId;
        var result = _store.RunCleanup(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all deletion reports.
    /// Requires admin or auditor role.
    /// </summary>
    [HttpGet("deletion-reports")]
    public ActionResult<IReadOnlyList<DeletionReport>> GetDeletionReports(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromQuery] string? tenantId = null)
    {
        // Check authorization
        var authResult = CheckAuditorOrAdminAccess(userId);
        if (!authResult.HasAccess)
        {
            return StatusCode(403, new { error = authResult.ErrorMessage });
        }

        var reports = _store.GetDeletionReports(tenantId);
        return Ok(reports);
    }

    /// <summary>
    /// Checks if a user has admin access.
    /// </summary>
    private (bool HasAccess, string? ErrorMessage) CheckAdminAccess(string userId)
    {
        var user = _store.GetUser(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        if (user.Role != "admin")
        {
            return (false, "Admin role required for this operation.");
        }

        return (true, null);
    }

    /// <summary>
    /// Checks if a user has admin or auditor access.
    /// </summary>
    private (bool HasAccess, string? ErrorMessage) CheckAuditorOrAdminAccess(string userId)
    {
        var user = _store.GetUser(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        if (user.Role != "admin" && user.Role != "auditor")
        {
            return (false, "Admin or auditor role required for this operation.");
        }

        return (true, null);
    }
}

/// <summary>
/// Request to update a retention policy.
/// </summary>
public sealed class UpdateRetentionPolicyRequest
{
    public int RetentionDays { get; set; }
    public bool AllowDeletion { get; set; }
}
