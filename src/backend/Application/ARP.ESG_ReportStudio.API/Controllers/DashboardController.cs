using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for dashboard analytics and statistics.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public DashboardController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets completeness statistics broken down by category and organizational unit.
    /// </summary>
    /// <param name="periodId">Optional filter by reporting period ID.</param>
    /// <param name="category">Optional filter by category (environmental, social, governance).</param>
    /// <param name="organizationalUnitId">Optional filter by organizational unit ID.</param>
    /// <returns>Completeness statistics with breakdowns.</returns>
    [HttpGet("completeness-stats")]
    public ActionResult<CompletenessStats> GetCompletenessStats(
        [FromQuery] string? periodId = null, 
        [FromQuery] string? category = null, 
        [FromQuery] string? organizationalUnitId = null)
    {
        var stats = _store.GetCompletenessStats(periodId, category, organizationalUnitId);
        return Ok(stats);
    }

    /// <summary>
    /// Compares completeness statistics between two reporting periods.
    /// </summary>
    /// <param name="currentPeriodId">Current reporting period ID.</param>
    /// <param name="priorPeriodId">Prior reporting period ID for comparison.</param>
    /// <returns>Completeness comparison with breakdowns, regressions, and improvements.</returns>
    [HttpGet("completeness-comparison")]
    public ActionResult<CompletenessComparison> CompareCompletenessStats(
        [FromQuery] string currentPeriodId,
        [FromQuery] string priorPeriodId)
    {
        if (string.IsNullOrWhiteSpace(currentPeriodId))
        {
            return BadRequest("currentPeriodId is required.");
        }

        if (string.IsNullOrWhiteSpace(priorPeriodId))
        {
            return BadRequest("priorPeriodId is required.");
        }

        if (currentPeriodId == priorPeriodId)
        {
            return BadRequest("currentPeriodId and priorPeriodId must be different.");
        }

        try
        {
            var comparison = _store.CompareCompletenessStats(currentPeriodId, priorPeriodId);
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
