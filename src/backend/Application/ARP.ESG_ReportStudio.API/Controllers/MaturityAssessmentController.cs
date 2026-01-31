using ARP.ESG_ReportStudio.API.Reporting;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// API endpoints for maturity assessment calculation and tracking.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/maturity-assessments")]
public class MaturityAssessmentController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<MaturityAssessmentController> _logger;

    public MaturityAssessmentController(InMemoryReportStore store, ILogger<MaturityAssessmentController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Calculate a maturity assessment for a reporting period.
    /// </summary>
    /// <param name="request">Assessment calculation request</param>
    /// <returns>The calculated assessment</returns>
    [HttpPost]
    public IActionResult CalculateAssessment([FromBody] CalculateMaturityAssessmentRequest request)
    {
        _logger.LogInformation("Calculating maturity assessment for period {PeriodId}", request.PeriodId);

        var (isValid, errorMessage, assessment) = _store.CalculateMaturityAssessment(request);

        if (!isValid)
        {
            _logger.LogWarning("Failed to calculate assessment: {Error}", errorMessage);
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation("Successfully calculated assessment {AssessmentId} with score {Score}", 
            assessment!.Id, assessment.OverallScore);

        return Ok(assessment);
    }

    /// <summary>
    /// Get the current (latest) maturity assessment for a period.
    /// </summary>
    /// <param name="periodId">Reporting period ID</param>
    /// <returns>Current assessment or null if none exists</returns>
    [HttpGet("period/{periodId}/current")]
    public IActionResult GetCurrentAssessment(string periodId)
    {
        _logger.LogInformation("Getting current assessment for period {PeriodId}", periodId);

        var assessment = _store.GetCurrentMaturityAssessment(periodId);

        if (assessment == null)
        {
            _logger.LogInformation("No current assessment found for period {PeriodId}", periodId);
            return NotFound(new { error = "No assessment found for this period" });
        }

        return Ok(assessment);
    }

    /// <summary>
    /// Get assessment history for a period.
    /// </summary>
    /// <param name="periodId">Reporting period ID</param>
    /// <returns>List of all assessments for the period, ordered by calculation date (newest first)</returns>
    [HttpGet("period/{periodId}/history")]
    public IActionResult GetAssessmentHistory(string periodId)
    {
        _logger.LogInformation("Getting assessment history for period {PeriodId}", periodId);

        var assessments = _store.GetMaturityAssessmentHistory(periodId);

        return Ok(assessments);
    }

    /// <summary>
    /// Get a specific maturity assessment by ID.
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <returns>The assessment</returns>
    [HttpGet("{id}")]
    public IActionResult GetAssessment(string id)
    {
        _logger.LogInformation("Getting assessment {AssessmentId}", id);

        var assessment = _store.GetMaturityAssessment(id);

        if (assessment == null)
        {
            _logger.LogWarning("Assessment {AssessmentId} not found", id);
            return NotFound(new { error = "Assessment not found" });
        }

        return Ok(assessment);
    }
}
