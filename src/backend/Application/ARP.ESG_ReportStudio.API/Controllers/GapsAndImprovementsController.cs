using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/gaps-and-improvements")]
public sealed class GapsAndImprovementsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public GapsAndImprovementsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get the Gaps and Improvements report for a reporting period.
    /// Compiles gaps, assumptions, simplifications, and remediation plans into a report-ready format.
    /// </summary>
    /// <param name="periodId">Optional reporting period ID to filter by</param>
    /// <param name="sectionId">Optional section ID to filter by</param>
    /// <returns>A comprehensive gaps and improvements report</returns>
    [HttpGet("report")]
    public ActionResult<GapsAndImprovementsReport> GetReport([FromQuery] string? periodId, [FromQuery] string? sectionId)
    {
        // In a real app, get current user from authentication context
        var currentUserId = "user-1"; // Placeholder
        
        var report = _store.GetGapsAndImprovementsReport(periodId, sectionId, currentUserId);
        return Ok(report);
    }

    /// <summary>
    /// Update the manual narrative for the gaps and improvements report.
    /// Allows report editors to override auto-generated content with custom text.
    /// </summary>
    /// <param name="request">Request containing the manual narrative text</param>
    /// <returns>Success response</returns>
    [HttpPost("narrative")]
    public ActionResult UpdateNarrative([FromBody] UpdateGapsNarrativeRequest request)
    {
        // In a real implementation, this would store the manual narrative in persistent storage
        // associated with the specific period/section combination.
        // For now, we return success to demonstrate the API contract.
        
        return Ok(new { message = "Manual narrative updated successfully. Set to null to revert to auto-generated content." });
    }

    /// <summary>
    /// Get gaps dashboard with filtering and sorting capabilities.
    /// Supports filtering by status, section, owner, and due period.
    /// Supports sorting by risk level and due date.
    /// </summary>
    /// <param name="periodId">Optional reporting period ID to filter by</param>
    /// <param name="status">Optional status filter: open, resolved, all (default: all)</param>
    /// <param name="sectionId">Optional section ID to filter by</param>
    /// <param name="ownerId">Optional owner ID to filter by</param>
    /// <param name="duePeriod">Optional due period to filter by (matches targetDate or remediation plan targetPeriod)</param>
    /// <param name="sortBy">Sort field: risk, dueDate, section (default: risk)</param>
    /// <param name="sortOrder">Sort order: asc, desc (default: desc for risk, asc for dueDate)</param>
    /// <returns>Gap dashboard with filtered and sorted results</returns>
    [HttpGet("dashboard")]
    public ActionResult<GapDashboardResponse> GetGapsDashboard(
        [FromQuery] string? periodId,
        [FromQuery] string? status = "all",
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? duePeriod = null,
        [FromQuery] string sortBy = "risk",
        [FromQuery] string sortOrder = "desc")
    {
        // Validate sortBy parameter
        var validSortBy = new[] { "risk", "dueDate", "section", "impact", "duePeriod" };
        if (!validSortBy.Contains(sortBy.ToLowerInvariant()))
        {
            sortBy = "risk";
        }
        
        // Validate sortOrder parameter
        var validSortOrder = new[] { "asc", "desc" };
        if (!validSortOrder.Contains(sortOrder.ToLowerInvariant()))
        {
            sortOrder = "desc";
        }
        
        var currentUserId = "user-1"; // Placeholder for authentication
        var dashboard = _store.GetGapsDashboard(periodId, status, sectionId, ownerId, duePeriod, sortBy, sortOrder, currentUserId);
        return Ok(dashboard);
    }
}
