using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Service for monitoring and auditing integration activities
/// </summary>
public class IntegrationMonitoringService
{
    private readonly IIntegrationJobMetadataRepository _jobMetadataRepository;
    private readonly IIntegrationLogRepository _logRepository;
    private readonly IHRSyncRecordRepository _hrSyncRecordRepository;
    private readonly IFinanceSyncRecordRepository _financeSyncRecordRepository;

    public IntegrationMonitoringService(
        IIntegrationJobMetadataRepository jobMetadataRepository,
        IIntegrationLogRepository logRepository,
        IHRSyncRecordRepository hrSyncRecordRepository,
        IFinanceSyncRecordRepository financeSyncRecordRepository)
    {
        _jobMetadataRepository = jobMetadataRepository;
        _logRepository = logRepository;
        _hrSyncRecordRepository = hrSyncRecordRepository;
        _financeSyncRecordRepository = financeSyncRecordRepository;
    }

    /// <summary>
    /// Search integration jobs with filtering and pagination
    /// </summary>
    public async Task<(List<IntegrationJobMetadata> jobs, int totalCount)> SearchJobsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationJobStatus? status = null,
        int? connectorId = null,
        string? jobType = null,
        string? initiatedBy = null,
        int page = 1,
        int pageSize = 50)
    {
        var skip = (page - 1) * pageSize;
        var jobs = await _jobMetadataRepository.SearchJobsAsync(
            startDate, endDate, status, connectorId, jobType, initiatedBy, skip, pageSize);
        var totalCount = await _jobMetadataRepository.GetJobCountAsync(
            startDate, endDate, status, connectorId, jobType, initiatedBy);
        
        return (jobs, totalCount);
    }

    /// <summary>
    /// Get detailed information about a specific job including related logs
    /// </summary>
    public async Task<IntegrationJobDetails?> GetJobDetailsAsync(string jobId)
    {
        var job = await _jobMetadataRepository.GetByJobIdAsync(jobId);
        if (job == null)
            return null;

        var logs = await _logRepository.GetByCorrelationIdAsync(job.CorrelationId);
        
        return new IntegrationJobDetails
        {
            Job = job,
            Logs = logs
        };
    }

    /// <summary>
    /// Get approval history for integration overrides
    /// </summary>
    public async Task<List<ApprovalHistoryEntry>> GetApprovalHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? connectorId = null,
        string? approvedBy = null)
    {
        var history = new List<ApprovalHistoryEntry>();

        // Get finance sync records with approvals
        var financeSyncRecords = await _financeSyncRecordRepository.SearchRecordsAsync(
            startDate, endDate, null, connectorId, null, null, 0, 1000);
        
        foreach (var record in financeSyncRecords.Where(r => !string.IsNullOrEmpty(r.ApprovedOverrideBy)))
        {
            if (approvedBy != null && record.ApprovedOverrideBy != approvedBy)
                continue;

            history.Add(new ApprovalHistoryEntry
            {
                Timestamp = record.SyncedAt,
                ApprovedBy = record.ApprovedOverrideBy!,
                Action = "Override Approved",
                EntityType = "FinanceEntity",
                EntityId = record.FinanceEntityId?.ToString(),
                ConnectorId = record.ConnectorId,
                CorrelationId = record.CorrelationId,
                ConflictResolution = record.ConflictResolution,
                Details = $"Override approved for external ID: {record.ExternalId}"
            });
        }

        return history.OrderByDescending(h => h.Timestamp).ToList();
    }

    /// <summary>
    /// Get integration statistics for a date range
    /// </summary>
    public async Task<IntegrationStatistics> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        int? connectorId = null)
    {
        var jobs = await _jobMetadataRepository.SearchJobsAsync(
            startDate, endDate, null, connectorId, null, null, 0, int.MaxValue);

        var logs = await _logRepository.SearchLogsAsync(
            startDate, endDate, null, connectorId, null, null, 0, int.MaxValue);

        return new IntegrationStatistics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalJobs = jobs.Count,
            CompletedJobs = jobs.Count(j => j.Status == IntegrationJobStatus.Completed),
            FailedJobs = jobs.Count(j => j.Status == IntegrationJobStatus.Failed),
            JobsWithErrors = jobs.Count(j => j.Status == IntegrationJobStatus.CompletedWithErrors),
            TotalRecordsProcessed = jobs.Sum(j => j.TotalRecords),
            TotalRecordsSucceeded = jobs.Sum(j => j.SuccessCount),
            TotalRecordsFailed = jobs.Sum(j => j.FailureCount),
            TotalApiCalls = logs.Count,
            SuccessfulApiCalls = logs.Count(l => l.Status == IntegrationStatus.Success),
            FailedApiCalls = logs.Count(l => l.Status == IntegrationStatus.Failed),
            AverageJobDurationMs = jobs.Where(j => j.DurationMs.HasValue).Select(j => j.DurationMs!.Value).DefaultIfEmpty(0).Average()
        };
    }

    /// <summary>
    /// Create a new integration job metadata entry
    /// </summary>
    public async Task<IntegrationJobMetadata> CreateJobAsync(IntegrationJobMetadata job)
    {
        return await _jobMetadataRepository.CreateAsync(job);
    }

    /// <summary>
    /// Update an existing integration job metadata entry
    /// </summary>
    public async Task<IntegrationJobMetadata> UpdateJobAsync(IntegrationJobMetadata job)
    {
        return await _jobMetadataRepository.UpdateAsync(job);
    }
}

/// <summary>
/// Detailed information about an integration job
/// </summary>
public class IntegrationJobDetails
{
    public IntegrationJobMetadata Job { get; set; } = null!;
    public List<IntegrationLog> Logs { get; set; } = new();
}

/// <summary>
/// Entry in the approval history showing who approved what and when
/// </summary>
public class ApprovalHistoryEntry
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

/// <summary>
/// Aggregated statistics for integration activities
/// </summary>
public class IntegrationStatistics
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
