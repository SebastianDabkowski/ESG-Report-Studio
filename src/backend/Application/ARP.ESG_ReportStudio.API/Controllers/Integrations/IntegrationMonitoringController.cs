using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using System.Text;

namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

/// <summary>
/// Controller for integration monitoring and audit trail
/// Provides searchable job metadata, logs, and approval history
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/integration-monitoring")]
public class IntegrationMonitoringController : ControllerBase
{
    private readonly IntegrationMonitoringService _monitoringService;

    public IntegrationMonitoringController(IntegrationMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    /// <summary>
    /// Search integration jobs with filtering and pagination
    /// </summary>
    /// <param name="startDate">Filter by jobs started after this date (ISO 8601 format)</param>
    /// <param name="endDate">Filter by jobs started before this date (ISO 8601 format)</param>
    /// <param name="status">Filter by job status (Queued, Running, Completed, CompletedWithErrors, Failed, Cancelled)</param>
    /// <param name="connectorId">Filter by connector ID</param>
    /// <param name="jobType">Filter by job type (e.g., HRSync, FinanceSync)</param>
    /// <param name="initiatedBy">Filter by user or service that initiated the job</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Results per page (default: 50, max: 100)</param>
    [HttpGet("jobs")]
    public async Task<ActionResult<JobSearchResponse>> SearchJobs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] IntegrationJobStatus? status = null,
        [FromQuery] int? connectorId = null,
        [FromQuery] string? jobType = null,
        [FromQuery] string? initiatedBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1)
            return BadRequest(new { error = "Page must be >= 1" });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "PageSize must be between 1 and 100" });

        var (jobs, totalCount) = await _monitoringService.SearchJobsAsync(
            startDate, endDate, status, connectorId, jobType, initiatedBy, page, pageSize);

        var response = new JobSearchResponse
        {
            Jobs = jobs.Select(MapToJobResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get detailed information about a specific integration job
    /// </summary>
    /// <param name="jobId">Unique job identifier</param>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<JobDetailsResponse>> GetJobDetails(string jobId)
    {
        var details = await _monitoringService.GetJobDetailsAsync(jobId);
        if (details == null)
            return NotFound(new { error = $"Job with ID {jobId} not found" });

        var response = new JobDetailsResponse
        {
            Job = MapToJobResponse(details.Job),
            Logs = details.Logs.Select(MapToLogResponse).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get approval history for integration overrides
    /// </summary>
    /// <param name="startDate">Filter by approvals after this date</param>
    /// <param name="endDate">Filter by approvals before this date</param>
    /// <param name="connectorId">Filter by connector ID</param>
    /// <param name="approvedBy">Filter by user who approved the override</param>
    [HttpGet("approvals")]
    public async Task<ActionResult<List<ApprovalHistoryResponse>>> GetApprovalHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? connectorId = null,
        [FromQuery] string? approvedBy = null)
    {
        var history = await _monitoringService.GetApprovalHistoryAsync(
            startDate, endDate, connectorId, approvedBy);

        var response = history.Select(h => new ApprovalHistoryResponse
        {
            Timestamp = h.Timestamp,
            ApprovedBy = h.ApprovedBy,
            Action = h.Action,
            EntityType = h.EntityType,
            EntityId = h.EntityId,
            ConnectorId = h.ConnectorId,
            CorrelationId = h.CorrelationId,
            ConflictResolution = h.ConflictResolution,
            Details = h.Details
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get integration statistics for a date range
    /// </summary>
    /// <param name="startDate">Start date for statistics (required)</param>
    /// <param name="endDate">End date for statistics (required)</param>
    /// <param name="connectorId">Optional connector ID to filter by</param>
    [HttpGet("statistics")]
    public async Task<ActionResult<IntegrationStatisticsResponse>> GetStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? connectorId = null)
    {
        if (startDate == default || endDate == default)
            return BadRequest(new { error = "Both startDate and endDate are required" });

        if (startDate > endDate)
            return BadRequest(new { error = "startDate must be before endDate" });

        var stats = await _monitoringService.GetStatisticsAsync(startDate, endDate, connectorId);

        var response = new IntegrationStatisticsResponse
        {
            StartDate = stats.StartDate,
            EndDate = stats.EndDate,
            TotalJobs = stats.TotalJobs,
            CompletedJobs = stats.CompletedJobs,
            FailedJobs = stats.FailedJobs,
            JobsWithErrors = stats.JobsWithErrors,
            TotalRecordsProcessed = stats.TotalRecordsProcessed,
            TotalRecordsSucceeded = stats.TotalRecordsSucceeded,
            TotalRecordsFailed = stats.TotalRecordsFailed,
            TotalApiCalls = stats.TotalApiCalls,
            SuccessfulApiCalls = stats.SuccessfulApiCalls,
            FailedApiCalls = stats.FailedApiCalls,
            AverageJobDurationMs = stats.AverageJobDurationMs
        };

        return Ok(response);
    }

    /// <summary>
    /// Export audit data as CSV for compliance purposes
    /// </summary>
    /// <param name="startDate">Start date for export</param>
    /// <param name="endDate">End date for export</param>
    /// <param name="connectorId">Optional connector ID filter</param>
    [HttpGet("export/audit-csv")]
    public async Task<IActionResult> ExportAuditCsv(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? connectorId = null)
    {
        var (jobs, _) = await _monitoringService.SearchJobsAsync(
            startDate, endDate, null, connectorId, null, null, 1, int.MaxValue);

        var csv = new StringBuilder();
        csv.AppendLine("JobId,JobType,ConnectorId,Status,StartedAt,CompletedAt,DurationMs,TotalRecords,SuccessCount,FailureCount,SkippedCount,InitiatedBy,ErrorSummary");

        foreach (var job in jobs)
        {
            csv.AppendLine($"\"{job.JobId}\",\"{job.JobType}\",{job.ConnectorId},\"{job.Status}\",\"{job.StartedAt:O}\",\"{job.CompletedAt:O}\",{job.DurationMs},{job.TotalRecords},{job.SuccessCount},{job.FailureCount},{job.SkippedCount},\"{job.InitiatedBy}\",\"{EscapeCsv(job.ErrorSummary)}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"integration-audit-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        return value.Replace("\"", "\"\"");
    }

    private static JobResponse MapToJobResponse(IntegrationJobMetadata job)
    {
        return new JobResponse
        {
            Id = job.Id,
            JobId = job.JobId,
            ConnectorId = job.ConnectorId,
            ConnectorName = job.Connector?.Name,
            CorrelationId = job.CorrelationId,
            JobType = job.JobType,
            Status = job.Status.ToString(),
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            DurationMs = job.DurationMs,
            TotalRecords = job.TotalRecords,
            SuccessCount = job.SuccessCount,
            FailureCount = job.FailureCount,
            SkippedCount = job.SkippedCount,
            ErrorSummary = job.ErrorSummary,
            InitiatedBy = job.InitiatedBy,
            Notes = job.Notes
        };
    }

    private static LogResponse MapToLogResponse(IntegrationLog log)
    {
        return new LogResponse
        {
            Id = log.Id,
            ConnectorId = log.ConnectorId,
            CorrelationId = log.CorrelationId,
            OperationType = log.OperationType,
            Status = log.Status.ToString(),
            HttpMethod = log.HttpMethod,
            Endpoint = log.Endpoint,
            HttpStatusCode = log.HttpStatusCode,
            RetryAttempts = log.RetryAttempts,
            ErrorMessage = log.ErrorMessage,
            DurationMs = log.DurationMs,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            InitiatedBy = log.InitiatedBy
        };
    }
}

// DTOs for responses
public class JobSearchResponse
{
    public List<JobResponse> Jobs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class JobResponse
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public int ConnectorId { get; set; }
    public string? ConnectorName { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorSummary { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class JobDetailsResponse
{
    public JobResponse Job { get; set; } = null!;
    public List<LogResponse> Logs { get; set; } = new();
}

public class LogResponse
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? HttpStatusCode { get; set; }
    public int RetryAttempts { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
}

public class ApprovalHistoryResponse
{
    public DateTime Timestamp { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ConflictResolution { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class IntegrationStatisticsResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int JobsWithErrors { get; set; }
    public int TotalRecordsProcessed { get; set; }
    public int TotalRecordsSucceeded { get; set; }
    public int TotalRecordsFailed { get; set; }
    public int TotalApiCalls { get; set; }
    public int SuccessfulApiCalls { get; set; }
    public int FailedApiCalls { get; set; }
    public double AverageJobDurationMs { get; set; }
}
