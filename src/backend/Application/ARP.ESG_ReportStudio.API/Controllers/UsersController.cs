using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing users and role assignments.
/// Provides endpoints for viewing users, assigning roles, and calculating effective permissions.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public UsersController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all users in the system.
    /// </summary>
    /// <response code="200">Returns list of all users</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<User>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<User>> GetUsers()
    {
        return Ok(_store.GetUsers());
    }

    /// <summary>
    /// Get a specific user by ID.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <response code="200">Returns the user</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> GetUser(string id)
    {
        var user = _store.GetUser(id);
        if (user == null)
        {
            return NotFound(new { error = $"User with ID '{id}' not found." });
        }

        return Ok(user);
    }

    /// <summary>
    /// Assign roles to a user. Replaces existing role assignments.
    /// Change is recorded with audit trail including who made the change and when.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Role assignment request</param>
    /// <response code="200">Roles assigned successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> AssignUserRoles(string id, [FromBody] AssignUserRolesRequest request)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userId = "admin-user";
        var userName = "Admin User";

        var (success, errorMessage) = _store.AssignUserRoles(id, request, userId, userName);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        var user = _store.GetUser(id);
        return Ok(user);
    }

    /// <summary>
    /// Remove a specific role from a user.
    /// Change is recorded with audit trail including who made the change and when.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="roleId">Role ID to remove</param>
    /// <response code="200">Role removed successfully</response>
    /// <response code="400">Invalid request or role not assigned</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id}/roles/{roleId}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> RemoveUserRole(string id, string roleId)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userId = "admin-user";
        var userName = "Admin User";

        var (success, errorMessage) = _store.RemoveUserRole(id, roleId, userId, userName);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        var user = _store.GetUser(id);
        return Ok(user);
    }

    /// <summary>
    /// Invite an external advisor with limited access and optional expiry.
    /// Assigns advisor role and grants access to specified sections.
    /// </summary>
    /// <param name="request">External advisor invitation request</param>
    /// <response code="200">Advisor invited successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("invite-external-advisor")]
    [ProducesResponseType(typeof(InviteExternalAdvisorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<InviteExternalAdvisorResponse> InviteExternalAdvisor([FromBody] InviteExternalAdvisorRequest request)
    {
        // For demo purposes, using a mock user. In production, get from auth context
        var userName = "Report Manager";

        var response = _store.InviteExternalAdvisor(request, userName);
        
        if (!response.Success)
        {
            return BadRequest(new { error = response.ErrorMessage });
        }

        return Ok(response);
    }

    /// <summary>
    /// Get effective permissions for a user based on assigned roles.
    /// Uses union strategy: user has a permission if ANY assigned role grants it.
    /// Shows which roles contribute which permissions for transparency.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <response code="200">Returns effective permissions</response>
    [HttpGet("{id}/effective-permissions")]
    [ProducesResponseType(typeof(EffectivePermissionsResponse), StatusCodes.Status200OK)]
    public ActionResult<EffectivePermissionsResponse> GetEffectivePermissions(string id)
    {
        var permissions = _store.GetEffectivePermissions(id);
        return Ok(permissions);
    }
}
