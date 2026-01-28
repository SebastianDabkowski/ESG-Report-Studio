using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for dashboard analytics and statistics.
/// </summary>
[ApiController]
[Route("api/dashboard")]
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
}
