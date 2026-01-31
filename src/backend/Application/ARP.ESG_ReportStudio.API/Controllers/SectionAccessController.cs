using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing granular section-level access control.
/// Enables Report Managers to grant and revoke access to specific sections.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/section-access")]
public sealed class SectionAccessController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public SectionAccessController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Grant access to a section for one or more users.
    /// </summary>
    /// <param name="request">Grant request containing section ID, user IDs, and reason</param>
    /// <response code="200">Access granted successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("grant")]
    [ProducesResponseType(typeof(GrantSectionAccessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<GrantSectionAccessResult> GrantSectionAccess([FromBody] GrantSectionAccessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SectionId))
        {
            return BadRequest(new { error = "Section ID is required." });
        }

        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return BadRequest(new { error = "At least one user ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.GrantedBy))
        {
            return BadRequest(new { error = "GrantedBy user ID is required." });
        }

        var result = _store.GrantSectionAccess(request);
        
        // If all operations failed, return bad request
        if (result.GrantedAccess.Count == 0 && result.Failures.Count > 0)
        {
            return BadRequest(new 
            { 
                error = "Failed to grant access to any users.", 
                failures = result.Failures 
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Revoke access to a section from one or more users.
    /// </summary>
    /// <param name="request">Revoke request containing section ID, user IDs, and reason</param>
    /// <response code="200">Access revoked successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(RevokeSectionAccessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<RevokeSectionAccessResult> RevokeSectionAccess([FromBody] RevokeSectionAccessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SectionId))
        {
            return BadRequest(new { error = "Section ID is required." });
        }

        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return BadRequest(new { error = "At least one user ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.RevokedBy))
        {
            return BadRequest(new { error = "RevokedBy user ID is required." });
        }

        var result = _store.RevokeSectionAccess(request);
        
        // If all operations failed, return bad request
        if (result.RevokedUserIds.Count == 0 && result.Failures.Count > 0)
        {
            return BadRequest(new 
            { 
                error = "Failed to revoke access from any users.", 
                failures = result.Failures 
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all sections a user has explicit access to.
    /// Does not include sections the user owns.
    /// </summary>
    /// <param name="userId">User ID to get section access for</param>
    /// <response code="200">Returns list of section access grants</response>
    /// <response code="400">Invalid user ID</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<SectionAccessGrant>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<SectionAccessGrant>> GetUserSectionAccess(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required." });
        }

        var grants = _store.GetUserSectionAccess(userId);
        return Ok(grants);
    }

    /// <summary>
    /// Get all users who have explicit access to a section.
    /// Includes the section owner and all users with explicit grants.
    /// </summary>
    /// <param name="sectionId">Section ID to get access summary for</param>
    /// <response code="200">Returns section access summary</response>
    /// <response code="400">Invalid section ID</response>
    [HttpGet("section/{sectionId}")]
    [ProducesResponseType(typeof(SectionAccessSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SectionAccessSummary> GetSectionAccessSummary(string sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            return BadRequest(new { error = "Section ID is required." });
        }

        var summary = _store.GetSectionAccessSummary(sectionId);
        return Ok(summary);
    }

    /// <summary>
    /// Check if a user has access to a specific section.
    /// Returns true if user is admin, section owner, or has explicit grant.
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="sectionId">Section ID to check access for</param>
    /// <response code="200">Returns access check result</response>
    /// <response code="400">Invalid parameters</response>
    [HttpGet("check")]
    [ProducesResponseType(typeof(SectionAccessCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SectionAccessCheckResult> CheckSectionAccess(
        [FromQuery] string userId, 
        [FromQuery] string sectionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required." });
        }

        if (string.IsNullOrWhiteSpace(sectionId))
        {
            return BadRequest(new { error = "Section ID is required." });
        }

        var hasAccess = _store.HasSectionAccess(userId, sectionId);
        
        return Ok(new SectionAccessCheckResult
        {
            UserId = userId,
            SectionId = sectionId,
            HasAccess = hasAccess
        });
    }
}

/// <summary>
/// Result of a section access check.
/// </summary>
public sealed class SectionAccessCheckResult
{
    /// <summary>
    /// User ID that was checked.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Section ID that was checked.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the user has access to the section.
    /// </summary>
    public bool HasAccess { get; set; }
}
