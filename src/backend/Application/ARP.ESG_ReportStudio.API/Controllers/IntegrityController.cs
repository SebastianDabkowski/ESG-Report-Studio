using ARP.ESG_ReportStudio.API.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/integrity")]
public class IntegrityController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<IntegrityController> _logger;

    public IntegrityController(InMemoryReportStore store, ILogger<IntegrityController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the integrity of a reporting period.
    /// </summary>
    [HttpPost("reporting-periods/{periodId}/verify")]
    public IActionResult VerifyReportingPeriodIntegrity(string periodId)
    {
        _logger.LogInformation("Verifying integrity for reporting period {PeriodId}", periodId);
        
        var isValid = _store.VerifyReportingPeriodIntegrity(periodId);
        
        return Ok(new
        {
            periodId,
            integrityValid = isValid,
            message = isValid 
                ? "Integrity check passed." 
                : "Integrity check failed. The reporting period data has been modified since creation."
        });
    }

    /// <summary>
    /// Verifies the integrity of a decision.
    /// </summary>
    [HttpPost("decisions/{decisionId}/verify")]
    public IActionResult VerifyDecisionIntegrity(string decisionId)
    {
        _logger.LogInformation("Verifying integrity for decision {DecisionId}", decisionId);
        
        var isValid = _store.VerifyDecisionIntegrity(decisionId);
        
        return Ok(new
        {
            decisionId,
            integrityValid = isValid,
            integrityStatus = isValid ? "valid" : "failed",
            message = isValid 
                ? "Integrity check passed." 
                : "Integrity check failed. The decision has been modified since creation."
        });
    }

    /// <summary>
    /// Gets the comprehensive integrity status for a reporting period.
    /// Includes period integrity and all related decisions.
    /// </summary>
    [HttpGet("reporting-periods/{periodId}/status")]
    public IActionResult GetIntegrityStatus(string periodId)
    {
        _logger.LogInformation("Getting integrity status for reporting period {PeriodId}", periodId);
        
        var status = _store.GetIntegrityStatus(periodId);
        
        if (status.ErrorMessage != null)
        {
            return NotFound(new { error = status.ErrorMessage });
        }
        
        return Ok(status);
    }

    /// <summary>
    /// Overrides an integrity warning to allow publication. Requires admin privileges.
    /// </summary>
    [HttpPost("reporting-periods/{periodId}/override-warning")]
    public IActionResult OverrideIntegrityWarning(
        string periodId,
        [FromBody] OverrideIntegrityWarningRequest request,
        [FromHeader(Name = "X-User-Id")] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required in X-User-Id header." });
        }

        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            return BadRequest(new { error = "Justification is required." });
        }

        _logger.LogInformation(
            "User {UserId} attempting to override integrity warning for period {PeriodId}", 
            userId, 
            periodId);
        
        var (success, errorMessage) = _store.OverrideIntegrityWarning(periodId, userId, request.Justification);
        
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(new
        {
            message = "Integrity warning overridden successfully.",
            periodId,
            overriddenBy = userId,
            justification = request.Justification
        });
    }

    /// <summary>
    /// Checks if a reporting period can be published based on integrity status.
    /// </summary>
    [HttpGet("reporting-periods/{periodId}/can-publish")]
    public IActionResult CanPublishPeriod(string periodId)
    {
        _logger.LogInformation("Checking if period {PeriodId} can be published", periodId);
        
        var canPublish = _store.CanPublishPeriod(periodId);
        var status = _store.GetIntegrityStatus(periodId);
        
        return Ok(new
        {
            periodId,
            canPublish,
            integrityValid = status.PeriodIntegrityValid,
            hasWarning = status.PeriodIntegrityWarning,
            failedDecisionCount = status.FailedDecisions.Count,
            message = canPublish 
                ? "Period can be published." 
                : "Publication blocked due to integrity warning. Admin override required."
        });
    }
}
