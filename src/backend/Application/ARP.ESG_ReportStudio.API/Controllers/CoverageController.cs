using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for analyzing standards coverage across reporting periods.
/// Provides insights into which standard disclosures are covered, partially covered, or missing.
/// </summary>
[ApiController]
[Route("api/coverage")]
public sealed class CoverageController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public CoverageController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets a coverage analysis for a specific standard and reporting period.
    /// Shows which disclosures are covered, partially covered, or missing.
    /// </summary>
    /// <param name="standardId">The standard ID to analyze.</param>
    /// <param name="periodId">The reporting period ID to analyze.</param>
    /// <param name="category">Optional filter by category (environmental, social, governance).</param>
    /// <param name="topic">Optional filter by topic.</param>
    /// <returns>Coverage analysis result.</returns>
    [HttpGet]
    public ActionResult<StandardCoverageAnalysis> GetStandardCoverageAnalysis(
        [FromQuery] string standardId,
        [FromQuery] string periodId,
        [FromQuery] string? category = null,
        [FromQuery] string? topic = null)
    {
        if (string.IsNullOrEmpty(standardId))
        {
            return BadRequest(new { error = "Standard ID is required." });
        }

        if (string.IsNullOrEmpty(periodId))
        {
            return BadRequest(new { error = "Period ID is required." });
        }

        try
        {
            var analysis = _store.GetStandardCoverageAnalysis(standardId, periodId, category, topic);
            return Ok(analysis);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
