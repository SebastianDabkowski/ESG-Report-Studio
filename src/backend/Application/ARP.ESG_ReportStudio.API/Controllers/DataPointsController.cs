using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/data-points")]
public sealed class DataPointsController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly INotificationService _notificationService;

    public DataPointsController(InMemoryReportStore store, INotificationService notificationService)
    {
        _store = store;
        _notificationService = notificationService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<DataPoint>> GetDataPoints([FromQuery] string? sectionId, [FromQuery] string? assignedUserId)
    {
        return Ok(_store.GetDataPoints(sectionId, assignedUserId));
    }

    [HttpGet("{id}")]
    public ActionResult<DataPoint> GetDataPoint(string id)
    {
        var dataPoint = _store.GetDataPoint(id);
        if (dataPoint == null)
        {
            return NotFound(new { error = $"DataPoint with ID '{id}' not found." });
        }

        return Ok(dataPoint);
    }

    [HttpPost]
    public ActionResult<DataPoint> CreateDataPoint([FromBody] CreateDataPointRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.CreateDataPoint(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Note: Notifications are sent when ownership changes (via UpdateDataPoint),
        // not when a data point is initially created with an owner.

        return CreatedAtAction(nameof(GetDataPoint), new { id = dataPoint!.Id }, dataPoint);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DataPoint>> UpdateDataPoint(string id, [FromBody] UpdateDataPointRequest request)
    {
        // Get the old data point to track owner changes
        var oldDataPoint = _store.GetDataPoint(id);
        var oldOwnerId = oldDataPoint?.OwnerId;
        
        var (isValid, errorMessage, dataPoint) = _store.UpdateDataPoint(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Send notifications only if owner changed (both old and new owners must exist)
        if (dataPoint != null && 
            !string.IsNullOrWhiteSpace(request.OwnerId) && 
            !string.IsNullOrWhiteSpace(oldOwnerId) &&
            oldOwnerId != request.OwnerId)
        {
            var updatedBy = _store.GetUser(request.UpdatedBy ?? string.Empty);
            
            // Only send notifications if we can identify who made the change
            if (updatedBy != null)
            {
                // Send removal notification to old owner
                var oldOwner = _store.GetUser(oldOwnerId);
                if (oldOwner != null)
                {
                    await _notificationService.SendDataPointRemovedNotificationAsync(
                        dataPoint, oldOwner, updatedBy);
                }
                
                // Send assignment notification to new owner
                var newOwner = _store.GetUser(request.OwnerId);
                if (newOwner != null)
                {
                    await _notificationService.SendDataPointAssignedNotificationAsync(
                        dataPoint, newOwner, updatedBy);
                }
            }
        }

        return Ok(dataPoint);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteDataPoint(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "deletedBy query parameter is required." });
        }

        var deleted = _store.DeleteDataPoint(id, deletedBy);
        if (!deleted)
        {
            return NotFound(new { error = $"DataPoint with ID '{id}' not found." });
        }

        return NoContent();
    }

    [HttpPost("{id}/approve")]
    public ActionResult<DataPoint> ApproveDataPoint(string id, [FromBody] ApproveDataPointRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.ApproveDataPoint(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpPost("{id}/request-changes")]
    public ActionResult<DataPoint> RequestChanges(string id, [FromBody] RequestChangesRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.RequestChanges(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpPost("{id}/status")]
    public IActionResult UpdateDataPointStatus(string id, [FromBody] UpdateDataPointStatusRequest request)
    {
        var (isValid, validationError, dataPoint) = _store.UpdateDataPointStatus(id, request);
        
        if (!isValid)
        {
            // Wrap validation error for consistency with other endpoints
            return BadRequest(new { error = validationError });
        }

        return Ok(dataPoint);
    }

    [HttpPost("{id}/notes")]
    public ActionResult<DataPointNote> CreateNote(string id, [FromBody] CreateDataPointNoteRequest request)
    {
        // Validate request content
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Note content cannot be empty." });
        }

        try
        {
            var note = _store.CreateDataPointNote(id, request);
            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/notes")]
    public ActionResult<List<DataPointNote>> GetNotes(string id)
    {
        // Validate that the data point exists
        var dataPoint = _store.GetDataPoint(id);
        if (dataPoint == null)
        {
            return NotFound(new { error = $"Data point with ID '{id}' not found." });
        }

        var notes = _store.GetDataPointNotes(id);
        return Ok(notes);
    }

    [HttpPost("{id}/flag-missing")]
    public ActionResult<DataPoint> FlagMissingData(string id, [FromBody] FlagMissingDataRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.FlagMissingData(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpPost("{id}/unflag-missing")]
    public ActionResult<DataPoint> UnflagMissingData(string id, [FromBody] UnflagMissingDataRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.UnflagMissingData(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpPost("{id}/transition-gap-status")]
    public ActionResult<DataPoint> TransitionGapStatus(string id, [FromBody] TransitionGapStatusRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.TransitionGapStatus(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpGet("{id}/lineage")]
    public ActionResult<CalculationLineageResponse> GetCalculationLineage(string id)
    {
        var lineage = _store.GetCalculationLineage(id);
        
        if (lineage == null)
        {
            return NotFound(new { error = $"DataPoint with ID '{id}' not found or is not a calculated value." });
        }

        return Ok(lineage);
    }

    /// <summary>
    /// Recalculates a derived data point by updating its lineage metadata.
    /// Note: This endpoint updates calculation metadata (version, snapshot) but does not
    /// perform the actual calculation. The calculation logic must be implemented by the caller
    /// and the resulting value should be passed separately.
    /// </summary>
    [HttpPost("{id}/recalculate")]
    public ActionResult<DataPoint> RecalculateDataPoint(string id, [FromBody] RecalculateDataPointRequest request)
    {
        // TODO: Implement actual calculation logic based on formula
        // For now, this endpoint only updates the lineage metadata
        
        var (isValid, errorMessage, dataPoint) = _store.RecalculateDataPoint(id, request, null, null);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }
}
