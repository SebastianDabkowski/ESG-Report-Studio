using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using SD.ProjectName.Modules.Integrations.Application;

namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/hr")]
public class HRSyncController : ControllerBase
{
    private readonly HRSyncService _hrSyncService;

    public HRSyncController(HRSyncService hrSyncService)
    {
        _hrSyncService = hrSyncService;
    }

    /// <summary>
    /// Test connection to an HR connector
    /// </summary>
    [HttpPost("connectors/{connectorId}/test-connection")]
    public async Task<ActionResult<TestConnectionResponse>> TestConnection(int connectorId)
    {
        var currentUser = User.Identity?.Name ?? "system";
        var result = await _hrSyncService.TestConnectionAsync(connectorId, currentUser);
        
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
    /// Manually trigger an HR data sync
    /// </summary>
    [HttpPost("sync/{connectorId}")]
    public async Task<ActionResult<HRSyncResponse>> TriggerSync(int connectorId)
    {
        var currentUser = User.Identity?.Name ?? "system";
        
        try
        {
            var result = await _hrSyncService.ExecuteSyncAsync(connectorId, currentUser, isScheduled: false);
            
            var response = new HRSyncResponse
            {
                ConnectorId = result.ConnectorId,
                CorrelationId = result.CorrelationId,
                Success = result.Success,
                Message = result.Message,
                ImportedCount = result.ImportedCount,
                UpdatedCount = result.UpdatedCount,
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
    public async Task<ActionResult<List<HRSyncRecordResponse>>> GetSyncHistory(
        int connectorId,
        [FromQuery] int limit = 100)
    {
        var records = await _hrSyncService.GetSyncHistoryAsync(connectorId, limit);
        var response = records.Select(r => new HRSyncRecordResponse
        {
            Id = r.Id,
            ConnectorId = r.ConnectorId,
            CorrelationId = r.CorrelationId,
            Status = r.Status.ToString(),
            ExternalId = r.ExternalId,
            RejectionReason = r.RejectionReason,
            OverwroteApprovedData = r.OverwroteApprovedData,
            SyncedAt = r.SyncedAt,
            InitiatedBy = r.InitiatedBy,
            HREntityId = r.HREntityId
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get rejected records for a connector
    /// </summary>
    [HttpGet("rejected-records/{connectorId}")]
    public async Task<ActionResult<List<HRSyncRecordResponse>>> GetRejectedRecords(
        int connectorId,
        [FromQuery] int limit = 100)
    {
        var records = await _hrSyncService.GetRejectedRecordsAsync(connectorId, limit);
        var response = records.Select(r => new HRSyncRecordResponse
        {
            Id = r.Id,
            ConnectorId = r.ConnectorId,
            CorrelationId = r.CorrelationId,
            Status = r.Status.ToString(),
            ExternalId = r.ExternalId,
            RawData = r.RawData,
            RejectionReason = r.RejectionReason,
            OverwroteApprovedData = r.OverwroteApprovedData,
            SyncedAt = r.SyncedAt,
            InitiatedBy = r.InitiatedBy,
            HREntityId = r.HREntityId
        }).ToList();

        return Ok(response);
    }
}

/// <summary>
/// Response DTO for test connection
/// </summary>
public class TestConnectionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Response DTO for HR sync operation
/// </summary>
public class HRSyncResponse
{
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int RejectedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Response DTO for HR sync record
/// </summary>
public class HRSyncRecordResponse
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? RawData { get; set; }
    public string? RejectionReason { get; set; }
    public bool OverwroteApprovedData { get; set; }
    public DateTime SyncedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public int? HREntityId { get; set; }
}
