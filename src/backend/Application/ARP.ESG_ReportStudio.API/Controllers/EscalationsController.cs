using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/escalations")]
public sealed class EscalationsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public EscalationsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get escalation configuration for a reporting period.
    /// </summary>
    [HttpGet("config/{periodId}")]
    public ActionResult<EscalationConfiguration> GetConfiguration(string periodId)
    {
        var config = _store.GetEscalationConfiguration(periodId);
        if (config == null)
        {
            // Return default configuration if none exists
            return Ok(new EscalationConfiguration
            {
                PeriodId = periodId,
                Enabled = true,
                DaysAfterDeadline = new List<int> { 3, 7 }
            });
        }

        return Ok(config);
    }

    /// <summary>
    /// Create or update escalation configuration for a reporting period.
    /// </summary>
    [HttpPost("config/{periodId}")]
    public ActionResult<EscalationConfiguration> UpdateConfiguration(
        string periodId,
        [FromBody] EscalationConfiguration config)
    {
        // Validate configuration
        if (config.DaysAfterDeadline == null || !config.DaysAfterDeadline.Any())
        {
            return BadRequest(new { error = "DaysAfterDeadline must contain at least one value." });
        }

        if (config.DaysAfterDeadline.Any(d => d <= 0))
        {
            return BadRequest(new { error = "DaysAfterDeadline values must be positive (days after deadline)." });
        }

        config.PeriodId = periodId;
        var result = _store.CreateOrUpdateEscalationConfiguration(periodId, config);
        return Ok(result);
    }

    /// <summary>
    /// Get escalation history for a specific data point or user.
    /// </summary>
    [HttpGet("history")]
    public ActionResult<IReadOnlyList<EscalationHistory>> GetHistory(
        [FromQuery] string? dataPointId = null,
        [FromQuery] string? userId = null)
    {
        var history = _store.GetEscalationHistory(dataPointId, userId);
        return Ok(history);
    }
}
