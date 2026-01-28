using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/reminders")]
public sealed class RemindersController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public RemindersController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get reminder configuration for a reporting period.
    /// </summary>
    [HttpGet("config/{periodId}")]
    public ActionResult<ReminderConfiguration> GetConfiguration(string periodId)
    {
        var config = _store.GetReminderConfiguration(periodId);
        if (config == null)
        {
            // Return default configuration if none exists
            return Ok(new ReminderConfiguration
            {
                PeriodId = periodId,
                Enabled = true,
                DaysBeforeDeadline = new List<int> { 7, 3, 1 },
                CheckFrequencyHours = 24
            });
        }

        return Ok(config);
    }

    /// <summary>
    /// Create or update reminder configuration for a reporting period.
    /// </summary>
    [HttpPost("config/{periodId}")]
    public ActionResult<ReminderConfiguration> UpdateConfiguration(
        string periodId, 
        [FromBody] ReminderConfiguration config)
    {
        config.PeriodId = periodId;
        var result = _store.CreateOrUpdateReminderConfiguration(periodId, config);
        return Ok(result);
    }

    /// <summary>
    /// Get reminder history for a specific data point or user.
    /// </summary>
    [HttpGet("history")]
    public ActionResult<IReadOnlyList<ReminderHistory>> GetHistory(
        [FromQuery] string? dataPointId = null,
        [FromQuery] string? userId = null)
    {
        var history = _store.GetReminderHistory(dataPointId, userId);
        return Ok(history);
    }
}
