using Microsoft.AspNetCore.Mvc;
using SD.ProjectName.Modules.Integrations.Application;

namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

[ApiController]
[Route("api/v1/finance")]
public class FinanceSyncController : ControllerBase
{
    private readonly FinanceSyncService _financeSyncService;

    public FinanceSyncController(FinanceSyncService financeSyncService)
    {
        _financeSyncService = financeSyncService;
    }

    /// <summary>
    /// Test connection to a Finance connector and validate authentication and required permissions
    /// </summary>
    [HttpPost("connectors/{connectorId}/test-connection")]
    public async Task<ActionResult<TestConnectionResponse>> TestConnection(int connectorId)
    {
        var currentUser = User.Identity?.Name ?? "system";
        var result = await _financeSyncService.TestConnectionAsync(connectorId, currentUser);
        
        var response = new TestConnectionResponse
        {
            Success = result.Success,
            Message = result.Message,
            CorrelationId = result.CorrelationId,
            DurationMs = result.DurationMs,
            ErrorDetails = result.ErrorDetails
        };

        if (result.Success)
        {
            return Ok(response);
        }
        else
        {
            return BadRequest(response);
        }
    }

    /// <summary>
    /// Manually trigger a Finance data sync.
    /// Imports financial data into staging area with provenance metadata.
    /// Implements conflict resolution that preserves manual values unless admin approves override.
    /// </summary>
    [HttpPost("sync/{connectorId}")]
    public async Task<ActionResult<FinanceSyncResponse>> TriggerSync(
        int connectorId,
        [FromQuery] string? approvedOverrideBy = null)
    {
        var currentUser = User.Identity?.Name ?? "system";
        
        try
        {
            var result = await _financeSyncService.ExecuteSyncAsync(
                connectorId, 
                currentUser, 
                isScheduled: false,
                approvedOverrideBy: approvedOverrideBy);
            
            var response = new FinanceSyncResponse
            {
                ConnectorId = result.ConnectorId,
                CorrelationId = result.CorrelationId,
                ImportJobId = result.ImportJobId,
                Success = result.Success,
                Message = result.Message,
                ImportedCount = result.ImportedCount,
                UpdatedCount = result.UpdatedCount,
                ConflictsPreservedCount = result.ConflictsPreservedCount,
                RejectedCount = result.RejectedCount,
                FailedCount = result.FailedCount,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get sync history for a connector
    /// </summary>
    [HttpGet("sync-history/{connectorId}")]
    public async Task<ActionResult<List<FinanceSyncRecordResponse>>> GetSyncHistory(
        int connectorId,
        [FromQuery] int limit = 100)
    {
        var records = await _financeSyncService.GetSyncHistoryAsync(connectorId, limit);
        var response = records.Select(r => new FinanceSyncRecordResponse
        {
            Id = r.Id,
            ConnectorId = r.ConnectorId,
            CorrelationId = r.CorrelationId,
            Status = r.Status.ToString(),
            ExternalId = r.ExternalId,
            RejectionReason = r.RejectionReason,
            ConflictDetected = r.ConflictDetected,
            ConflictResolution = r.ConflictResolution,
            OverwroteApprovedData = r.OverwroteApprovedData,
            ApprovedOverrideBy = r.ApprovedOverrideBy,
            SyncedAt = r.SyncedAt,
            InitiatedBy = r.InitiatedBy,
            FinanceEntityId = r.FinanceEntityId
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get rejected records for a connector
    /// </summary>
    [HttpGet("rejected-records/{connectorId}")]
    public async Task<ActionResult<List<FinanceSyncRecordResponse>>> GetRejectedRecords(
        int connectorId,
        [FromQuery] int limit = 100)
    {
        var records = await _financeSyncService.GetRejectedRecordsAsync(connectorId, limit);
        var response = records.Select(r => new FinanceSyncRecordResponse
        {
            Id = r.Id,
            ConnectorId = r.ConnectorId,
            CorrelationId = r.CorrelationId,
            Status = r.Status.ToString(),
            ExternalId = r.ExternalId,
            RawData = r.RawData,
            RejectionReason = r.RejectionReason,
            ConflictDetected = r.ConflictDetected,
            ConflictResolution = r.ConflictResolution,
            OverwroteApprovedData = r.OverwroteApprovedData,
            SyncedAt = r.SyncedAt,
            InitiatedBy = r.InitiatedBy
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get conflict records for a connector (where manual data was preserved)
    /// </summary>
    [HttpGet("conflicts/{connectorId}")]
    public async Task<ActionResult<List<FinanceSyncRecordResponse>>> GetConflicts(
        int connectorId,
        [FromQuery] int limit = 100)
    {
        var records = await _financeSyncService.GetConflictsAsync(connectorId, limit);
        var response = records.Select(r => new FinanceSyncRecordResponse
        {
            Id = r.Id,
            ConnectorId = r.ConnectorId,
            CorrelationId = r.CorrelationId,
            Status = r.Status.ToString(),
            ExternalId = r.ExternalId,
            RawData = r.RawData,
            RejectionReason = r.RejectionReason,
            ConflictDetected = r.ConflictDetected,
            ConflictResolution = r.ConflictResolution,
            OverwroteApprovedData = r.OverwroteApprovedData,
            ApprovedOverrideBy = r.ApprovedOverrideBy,
            SyncedAt = r.SyncedAt,
            InitiatedBy = r.InitiatedBy,
            FinanceEntityId = r.FinanceEntityId
        }).ToList();

        return Ok(response);
    }
}

/// <summary>
/// Response DTO for Finance sync operations
/// </summary>
public class FinanceSyncResponse
{
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ImportJobId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ConflictsPreservedCount { get; set; }
    public int RejectedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Response DTO for Finance sync records
/// </summary>
public class FinanceSyncRecordResponse
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? RawData { get; set; }
    public string? RejectionReason { get; set; }
    public bool ConflictDetected { get; set; }
    public string? ConflictResolution { get; set; }
    public bool OverwroteApprovedData { get; set; }
    public string? ApprovedOverrideBy { get; set; }
    public DateTime SyncedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public int? FinanceEntityId { get; set; }
}
