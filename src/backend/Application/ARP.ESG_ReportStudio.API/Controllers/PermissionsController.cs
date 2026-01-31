using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing permissions and viewing the permission matrix.
/// Provides endpoints for viewing role-action mappings, permission history, and checking permissions.
/// </summary>
[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public PermissionsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get the complete permission matrix showing all roles and their permissions across resources.
    /// </summary>
    /// <response code="200">Returns the permission matrix</response>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(PermissionMatrix), StatusCodes.Status200OK)]
    public ActionResult<PermissionMatrix> GetPermissionMatrix()
    {
        var matrix = _store.GetPermissionMatrix();
        return Ok(matrix);
    }

    /// <summary>
    /// Get permission change history from the audit log.
    /// Shows who changed permissions, what changed, and when.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return (default: all)</param>
    /// <response code="200">Returns permission change history</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogEntry>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AuditLogEntry>> GetPermissionHistory([FromQuery] int? limit = null)
    {
        var history = _store.GetPermissionChangeHistory(limit);
        return Ok(history);
    }

    /// <summary>
    /// Check if a user has permission to perform an action on a resource.
    /// Logs the check result to the audit log (both allowed and denied attempts).
    /// </summary>
    /// <param name="request">Permission check request</param>
    /// <response code="200">Returns permission check result</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PermissionCheckResult> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { error = "User ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return BadRequest(new { error = "Resource type is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest(new { error = "Action is required." });
        }

        // For demo purposes, using a mock user. In production, get from auth context
        var userName = "System User";

        var result = _store.CheckPermission(request, userName);
        return Ok(result);
    }
}
