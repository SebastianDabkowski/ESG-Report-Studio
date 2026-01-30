using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ReportingController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly INotificationService _notificationService;

    public ReportingController(InMemoryReportStore store, INotificationService notificationService)
    {
        _store = store;
        _notificationService = notificationService;
    }

    [HttpGet("periods")]
    public ActionResult<IReadOnlyList<ReportingPeriod>> GetPeriods()
    {
        return Ok(_store.GetPeriods());
    }

    [HttpPost("periods")]
    public ActionResult<ReportingDataSnapshot> CreatePeriod([FromBody] CreateReportingPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.StartDate)
            || string.IsNullOrWhiteSpace(request.EndDate)
            || string.IsNullOrWhiteSpace(request.OwnerId)
            || string.IsNullOrWhiteSpace(request.OwnerName))
        {
            return BadRequest("Name, dates, and owner info are required.");
        }

        var (isValid, errorMessage, snapshot) = _store.ValidateAndCreatePeriod(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(snapshot);
    }

    [HttpPut("periods/{id}")]
    public ActionResult<ReportingPeriod> UpdatePeriod(string id, [FromBody] UpdateReportingPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.StartDate)
            || string.IsNullOrWhiteSpace(request.EndDate))
        {
            return BadRequest("Name and dates are required.");
        }

        var (isValid, errorMessage, period) = _store.ValidateAndUpdatePeriod(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(period);
    }

    [HttpGet("periods/{id}/has-started")]
    public ActionResult<bool> HasReportingStarted(string id)
    {
        return Ok(_store.HasReportingStarted(id));
    }

    [HttpGet("sections")]
    public ActionResult<IReadOnlyList<ReportSection>> GetSections([FromQuery] string? periodId)
    {
        return Ok(_store.GetSections(periodId));
    }

    [HttpGet("section-summaries")]
    public ActionResult<IReadOnlyList<SectionSummary>> GetSectionSummaries([FromQuery] string? periodId)
    {
        return Ok(_store.GetSectionSummaries(periodId));
    }

    [HttpGet("reporting-data")]
    public ActionResult<ReportingDataSnapshot> GetReportingData()
    {
        return Ok(_store.GetSnapshot());
    }

    [HttpPut("sections/{id}/owner")]
    public async Task<ActionResult<ReportSection>> UpdateSectionOwner(string id, [FromBody] UpdateSectionOwnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "UpdatedBy is required." });
        }

        var (isValid, errorMessage, result) = _store.UpdateSectionOwner(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Send notifications after successful update
        if (result != null && result.Section != null && result.ChangedBy != null)
        {
            // Send removal notification to old owner if they exist and are different from new owner
            if (result.OldOwner != null && result.NewOwner?.Id != result.OldOwner.Id)
            {
                await _notificationService.SendSectionRemovedNotificationAsync(
                    result.Section, result.OldOwner, result.ChangedBy, request.ChangeNote);
            }
            
            // Send assignment notification to new owner if they exist and are different from old owner
            if (result.NewOwner != null && result.OldOwner?.Id != result.NewOwner.Id)
            {
                await _notificationService.SendSectionAssignedNotificationAsync(
                    result.Section, result.NewOwner, result.ChangedBy, request.ChangeNote);
            }
        }

        return Ok(result?.Section);
    }

    [HttpPost("sections/bulk-owner")]
    public async Task<ActionResult<BulkUpdateSectionOwnerResult>> UpdateSectionOwnersBulk([FromBody] BulkUpdateSectionOwnerRequest request)
    {
        if (request.SectionIds == null || request.SectionIds.Count == 0)
        {
            return BadRequest(new { error = "SectionIds are required." });
        }

        if (string.IsNullOrWhiteSpace(request.OwnerId) || string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "OwnerId and UpdatedBy are required." });
        }

        var result = _store.UpdateSectionOwnersBulk(request);
        
        // Send notifications for all successful updates concurrently
        if (result.OwnerUpdates.Count > 0)
        {
            var changedBy = _store.GetUser(request.UpdatedBy);
            if (changedBy != null)
            {
                var notificationTasks = new List<Task>();
                
                foreach (var update in result.OwnerUpdates)
                {
                    // Send removal notification to old owner if they exist and are different from new owner
                    if (update.OldOwner != null && update.OldOwner.Id != update.NewOwner?.Id)
                    {
                        notificationTasks.Add(_notificationService.SendSectionRemovedNotificationAsync(
                            update.Section, update.OldOwner, changedBy, request.ChangeNote));
                    }
                    
                    // Send assignment notification to new owner if they exist and are different from old owner
                    if (update.NewOwner != null && update.OldOwner?.Id != update.NewOwner.Id)
                    {
                        notificationTasks.Add(_notificationService.SendSectionAssignedNotificationAsync(
                            update.Section, update.NewOwner, changedBy, request.ChangeNote));
                    }
                }
                
                // Send all notifications concurrently
                await Task.WhenAll(notificationTasks);
            }
        }
        
        return Ok(result);
    }

    [HttpGet("responsibility-matrix")]
    public ActionResult<ResponsibilityMatrix> GetResponsibilityMatrix([FromQuery] string? periodId, [FromQuery] string? ownerFilter)
    {
        var matrix = _store.GetResponsibilityMatrix(periodId, ownerFilter);
        return Ok(matrix);
    }
    
    [HttpPost("periods/rollover")]
    public ActionResult<RolloverResult> RolloverPeriod([FromBody] RolloverRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.SourcePeriodId))
        {
            return BadRequest(new { error = "SourcePeriodId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodName))
        {
            return BadRequest(new { error = "TargetPeriodName is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodStartDate))
        {
            return BadRequest(new { error = "TargetPeriodStartDate is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodEndDate))
        {
            return BadRequest(new { error = "TargetPeriodEndDate is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.PerformedBy))
        {
            return BadRequest(new { error = "PerformedBy is required." });
        }
        
        // Perform rollover
        var (success, errorMessage, result) = _store.RolloverPeriod(request);
        
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(result);
    }
    
    [HttpGet("periods/{periodId}/rollover-audit")]
    public ActionResult<IReadOnlyList<RolloverAuditLog>> GetRolloverAuditLogs(string periodId)
    {
        var logs = _store.GetRolloverAuditLogs(periodId);
        return Ok(logs);
    }
}
