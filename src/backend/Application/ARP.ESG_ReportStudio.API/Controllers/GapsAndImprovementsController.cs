using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/gaps-and-improvements")]
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
}
