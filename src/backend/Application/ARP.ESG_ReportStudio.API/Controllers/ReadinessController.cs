using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for coverage and readiness reporting.
/// Provides metrics on ownership completeness and data completion.
/// </summary>
[ApiController]
[Route("api/readiness")]
public sealed class ReadinessController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ReadinessController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets a readiness report showing ownership and completion metrics.
    /// </summary>
    /// <param name="periodId">Optional filter by reporting period ID.</param>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="ownerId">Optional filter by owner ID.</param>
    /// <param name="category">Optional filter by ESG category (environmental, social, governance).</param>
    /// <returns>Readiness report with metrics and filtered item list.</returns>
    [HttpGet("report")]
    public ActionResult<ReadinessReport> GetReadinessReport(
        [FromQuery] string? periodId = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? category = null)
    {
        var report = _store.GetReadinessReport(periodId, sectionId, ownerId, category);
        return Ok(report);
    }
}
