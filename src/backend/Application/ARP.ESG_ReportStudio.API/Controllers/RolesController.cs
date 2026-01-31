using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing system roles and permissions.
/// Provides endpoints for viewing, creating, updating, and deleting roles.
/// Predefined roles are protected from deletion to ensure consistent access control.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public RolesController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all system roles with descriptions and permissions.
    /// </summary>
    /// <response code="200">Returns list of all roles</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SystemRole>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<SystemRole>> GetRoles()
    {
        var roles = _store.GetRoles();
        return Ok(roles);
    }

    /// <summary>
    /// Get a specific role by ID.
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <response code="200">Returns the role</response>
    /// <response code="404">Role not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SystemRole), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SystemRole> GetRole(string id)
    {
        var role = _store.GetRole(id);
        if (role == null)
        {
            return NotFound(new { error = $"Role with ID '{id}' not found." });
        }

        return Ok(role);
    }

    /// <summary>
    /// Create a new custom role.
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <response code="201">Role created successfully</response>
    /// <response code="400">Invalid request or duplicate name</response>
    [HttpPost]
    [ProducesResponseType(typeof(SystemRole), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SystemRole> CreateRole([FromBody] CreateRoleRequest request)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userId = "admin-user";
        var userName = "Admin User";

        var (success, errorMessage, role) = _store.CreateRole(request, userId, userName);
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetRole), new { id = role!.Id }, role);
    }

    /// <summary>
    /// Update a role's description. Version is incremented and change is audited.
    /// Permissions cannot be updated to maintain consistency.
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Update request</param>
    /// <response code="200">Role updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Role not found</response>
    [HttpPatch("{id}/description")]
    [ProducesResponseType(typeof(SystemRole), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SystemRole> UpdateRoleDescription(string id, [FromBody] UpdateRoleRequest request)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userId = "admin-user";
        var userName = "Admin User";

        var (success, errorMessage) = _store.UpdateRoleDescription(id, request, userId, userName);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        var role = _store.GetRole(id);
        return Ok(role);
    }

    /// <summary>
    /// Delete a custom role. Predefined roles cannot be deleted.
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <response code="204">Role deleted successfully</response>
    /// <response code="400">Cannot delete predefined role</response>
    /// <response code="404">Role not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteRole(string id)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userId = "admin-user";
        var userName = "Admin User";

        var (success, errorMessage) = _store.DeleteRole(id, userId, userName);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }
}
