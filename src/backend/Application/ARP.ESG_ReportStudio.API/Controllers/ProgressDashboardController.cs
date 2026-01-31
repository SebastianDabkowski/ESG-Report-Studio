using ARP.ESG_ReportStudio.API.Reporting;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// API endpoints for progress dashboard showing trends across periods.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/progress-dashboard")]
public class ProgressDashboardController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<ProgressDashboardController> _logger;

    public ProgressDashboardController(InMemoryReportStore store, ILogger<ProgressDashboardController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Gets progress trends (completeness and maturity) across multiple periods.
    /// </summary>
    /// <param name="periodIds">Optional list of period IDs to include. If not specified, all periods are included.</param>
    /// <param name="category">Optional filter by category (environmental, social, governance).</param>
    /// <param name="organizationalUnitId">Optional filter by organizational unit ID.</param>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="ownerId">Optional filter by owner ID.</param>
    /// <returns>Progress trends response with period data and summary.</returns>
    [HttpGet("trends")]
    public ActionResult<ProgressTrendsResponse> GetProgressTrends(
        [FromQuery] List<string>? periodIds = null,
        [FromQuery] string? category = null,
        [FromQuery] string? organizationalUnitId = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null)
    {
        _logger.LogInformation("Getting progress trends with filters: periodIds={PeriodIds}, category={Category}, orgUnit={OrgUnit}, section={Section}, owner={Owner}",
            periodIds != null ? string.Join(",", periodIds) : "all",
            category ?? "all",
            organizationalUnitId ?? "all",
            sectionId ?? "all",
            ownerId ?? "all");

        var trends = _store.GetProgressTrends(periodIds, category, organizationalUnitId, sectionId, ownerId);

        _logger.LogInformation("Retrieved trends for {Count} periods", trends.Periods.Count);

        return Ok(trends);
    }

    /// <summary>
    /// Gets outstanding actions across periods.
    /// </summary>
    /// <param name="periodIds">Optional list of period IDs to include. If not specified, all periods are included.</param>
    /// <param name="category">Optional filter by category (environmental, social, governance).</param>
    /// <param name="organizationalUnitId">Optional filter by organizational unit ID.</param>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="ownerId">Optional filter by owner ID.</param>
    /// <param name="priority">Optional filter by priority (high, medium, low).</param>
    /// <returns>Outstanding actions response with actions list and summary.</returns>
    [HttpGet("outstanding-actions")]
    public ActionResult<OutstandingActionsResponse> GetOutstandingActions(
        [FromQuery] List<string>? periodIds = null,
        [FromQuery] string? category = null,
        [FromQuery] string? organizationalUnitId = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? priority = null)
    {
        _logger.LogInformation("Getting outstanding actions with filters: periodIds={PeriodIds}, category={Category}, orgUnit={OrgUnit}, section={Section}, owner={Owner}, priority={Priority}",
            periodIds != null ? string.Join(",", periodIds) : "all",
            category ?? "all",
            organizationalUnitId ?? "all",
            sectionId ?? "all",
            ownerId ?? "all",
            priority ?? "all");

        var actions = _store.GetOutstandingActions(periodIds, category, organizationalUnitId, sectionId, ownerId, priority);

        _logger.LogInformation("Retrieved {Count} outstanding actions", actions.Actions.Count);

        return Ok(actions);
    }

    /// <summary>
    /// Exports the progress dashboard data.
    /// </summary>
    /// <param name="format">Export format: "csv" or "pdf"</param>
    /// <param name="periodIds">Optional list of period IDs to include.</param>
    /// <param name="category">Optional filter by category.</param>
    /// <param name="organizationalUnitId">Optional filter by organizational unit ID.</param>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="ownerId">Optional filter by owner ID.</param>
    /// <returns>File download with the exported data.</returns>
    [HttpGet("export")]
    public IActionResult ExportProgressDashboard(
        [FromQuery] string format = "csv",
        [FromQuery] List<string>? periodIds = null,
        [FromQuery] string? category = null,
        [FromQuery] string? organizationalUnitId = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null)
    {
        _logger.LogInformation("Exporting progress dashboard in format: {Format}", format);

        var trends = _store.GetProgressTrends(periodIds, category, organizationalUnitId, sectionId, ownerId);
        var actions = _store.GetOutstandingActions(periodIds, category, organizationalUnitId, sectionId, ownerId, null);

        if (format.ToLower() == "csv")
        {
            var csv = GenerateCsvExport(trends, actions);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "progress-dashboard.csv");
        }
        else if (format.ToLower() == "pdf")
        {
            // For now, return a simple text representation with .txt extension
            // TODO: In a real implementation, use a PDF library like QuestPDF or PdfSharp
            var content = GeneratePdfContent(trends, actions);
            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/plain", "progress-dashboard.txt");
        }

        return BadRequest(new { error = "Invalid format. Use 'csv' or 'pdf'." });
    }

    private string GenerateCsvExport(ProgressTrendsResponse trends, OutstandingActionsResponse actions)
    {
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("Progress Dashboard Export");
        csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        csv.AppendLine();
        
        // Summary
        csv.AppendLine("Summary");
        csv.AppendLine($"Total Periods,{trends.Summary.TotalPeriods}");
        csv.AppendLine($"Locked Periods,{trends.Summary.LockedPeriods}");
        csv.AppendLine($"Latest Completeness %,{trends.Summary.LatestCompletenessPercentage?.ToString("F2") ?? "N/A"}");
        csv.AppendLine($"Latest Maturity Score,{trends.Summary.LatestMaturityScore?.ToString("F2") ?? "N/A"}");
        csv.AppendLine($"Completeness Change,{trends.Summary.CompletenessChange?.ToString("F2") ?? "N/A"}");
        csv.AppendLine($"Maturity Change,{trends.Summary.MaturityChange?.ToString("F2") ?? "N/A"}");
        csv.AppendLine();
        
        // Period Trends
        csv.AppendLine("Period Trends");
        csv.AppendLine("Period Name,Start Date,End Date,Status,Locked,Completeness %,Complete Data Points,Total Data Points,Maturity Score,Maturity Level,Open Gaps,High Risk Gaps,Blocked Data Points");
        
        foreach (var period in trends.Periods)
        {
            csv.AppendLine($"{period.PeriodName},{period.StartDate},{period.EndDate},{period.Status},{period.IsLocked},{period.CompletenessPercentage:F2},{period.CompleteDataPoints},{period.TotalDataPoints},{period.MaturityScore?.ToString("F2") ?? "N/A"},{period.MaturityLevel ?? "N/A"},{period.OpenGaps},{period.HighRiskGaps},{period.BlockedDataPoints}");
        }
        
        csv.AppendLine();
        
        // Outstanding Actions
        csv.AppendLine("Outstanding Actions Summary");
        csv.AppendLine($"Total Actions,{actions.Summary.TotalActions}");
        csv.AppendLine($"High Priority,{actions.Summary.HighPriority}");
        csv.AppendLine($"Medium Priority,{actions.Summary.MediumPriority}");
        csv.AppendLine($"Low Priority,{actions.Summary.LowPriority}");
        csv.AppendLine($"Open Gaps,{actions.Summary.OpenGaps}");
        csv.AppendLine($"Blocked Data Points,{actions.Summary.BlockedDataPoints}");
        csv.AppendLine($"Pending Approvals,{actions.Summary.PendingApprovals}");
        csv.AppendLine();
        
        // Actions Detail
        csv.AppendLine("Outstanding Actions Detail");
        csv.AppendLine("Action Type,Title,Period,Section,Category,Owner,Priority,Due Date");
        
        foreach (var action in actions.Actions)
        {
            csv.AppendLine($"{action.ActionType},{action.Title},{action.PeriodName},{action.SectionTitle},{action.Category},{action.OwnerName},{action.Priority},{action.DueDate}");
        }
        
        return csv.ToString();
    }

    private string GeneratePdfContent(ProgressTrendsResponse trends, OutstandingActionsResponse actions)
    {
        // This is a placeholder. In a real implementation, use a PDF library like QuestPDF or PdfSharp
        var content = new System.Text.StringBuilder();
        
        content.AppendLine("PROGRESS DASHBOARD REPORT");
        content.AppendLine("========================");
        content.AppendLine();
        content.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        content.AppendLine();
        
        content.AppendLine("SUMMARY");
        content.AppendLine("-------");
        content.AppendLine($"Total Periods: {trends.Summary.TotalPeriods}");
        content.AppendLine($"Locked Periods: {trends.Summary.LockedPeriods}");
        content.AppendLine($"Latest Completeness: {trends.Summary.LatestCompletenessPercentage?.ToString("F2") ?? "N/A"}%");
        content.AppendLine($"Latest Maturity Score: {trends.Summary.LatestMaturityScore?.ToString("F2") ?? "N/A"}");
        content.AppendLine($"Completeness Change: {trends.Summary.CompletenessChange?.ToString("F2") ?? "N/A"}%");
        content.AppendLine($"Maturity Change: {trends.Summary.MaturityChange?.ToString("F2") ?? "N/A"}");
        content.AppendLine();
        
        content.AppendLine("PERIOD TRENDS");
        content.AppendLine("-------------");
        foreach (var period in trends.Periods)
        {
            content.AppendLine($"{period.PeriodName} ({period.StartDate} - {period.EndDate})");
            content.AppendLine($"  Status: {period.Status} {(period.IsLocked ? "[LOCKED]" : "")}");
            content.AppendLine($"  Completeness: {period.CompletenessPercentage:F2}% ({period.CompleteDataPoints}/{period.TotalDataPoints})");
            content.AppendLine($"  Maturity: {period.MaturityScore?.ToString("F2") ?? "N/A"} - {period.MaturityLevel ?? "N/A"}");
            content.AppendLine($"  Issues: {period.OpenGaps} gaps ({period.HighRiskGaps} high-risk), {period.BlockedDataPoints} blocked");
            content.AppendLine();
        }
        
        content.AppendLine("OUTSTANDING ACTIONS");
        content.AppendLine("------------------");
        content.AppendLine($"Total: {actions.Summary.TotalActions}");
        content.AppendLine($"High Priority: {actions.Summary.HighPriority}");
        content.AppendLine($"Medium Priority: {actions.Summary.MediumPriority}");
        content.AppendLine($"Low Priority: {actions.Summary.LowPriority}");
        content.AppendLine();
        
        return content.ToString();
    }
}
