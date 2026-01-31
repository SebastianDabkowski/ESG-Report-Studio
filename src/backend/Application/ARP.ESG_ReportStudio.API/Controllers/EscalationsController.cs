using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/escalations")]
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
            // Note: Empty Id/CreatedAt/UpdatedAt indicate this is a non-persisted default
            return Ok(new EscalationConfiguration
            {
                Id = string.Empty,
                PeriodId = periodId,
                Enabled = true,
                DaysAfterDeadline = new List<int> { 3, 7 },
                CreatedAt = string.Empty,
                UpdatedAt = string.Empty
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

        // Create new config object to avoid mutating the parameter
        var configToSave = new EscalationConfiguration
        {
            PeriodId = periodId,
            Enabled = config.Enabled,
            DaysAfterDeadline = config.DaysAfterDeadline
        };

        var result = _store.CreateOrUpdateEscalationConfiguration(periodId, configToSave);
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
