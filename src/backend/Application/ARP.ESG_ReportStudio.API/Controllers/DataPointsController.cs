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

    /// <summary>
    /// Get data points accessible to a specific user.
    /// Filters by sections the user owns or has explicit access to.
    /// </summary>
    /// <param name="userId">User ID to filter data points for</param>
    /// <param name="sectionId">Optional section ID to filter by</param>
    /// <response code="200">Returns accessible data points</response>
    /// <response code="400">Invalid user ID</response>
    /// <response code="403">User does not have access to the specified section</response>
    [HttpGet("accessible")]
    [ProducesResponseType(typeof(IReadOnlyList<DataPoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<DataPoint>> GetAccessibleDataPoints(
        [FromQuery] string userId, 
        [FromQuery] string? sectionId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required." });
        }

        // If a specific section is requested, check access first
        if (!string.IsNullOrWhiteSpace(sectionId))
        {
            if (!_store.HasSectionAccess(userId, sectionId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new 
                { 
                    error = "Access denied. You do not have permission to view this section.",
                    sectionId 
                });
            }
            
            return Ok(_store.GetDataPoints(sectionId, null));
        }

        // Get all accessible sections for the user
        var accessibleSections = _store.GetAccessibleSections(userId, null);
        var accessibleSectionIds = accessibleSections.Select(s => s.Id).ToHashSet();

        // Get all data points and filter to accessible sections
        var allDataPoints = _store.GetDataPoints(null, null);
        var accessibleDataPoints = allDataPoints
            .Where(dp => accessibleSectionIds.Contains(dp.SectionId))
            .ToList();

        return Ok(accessibleDataPoints);
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

    /// <summary>
    /// Retrieves cross-period lineage for a data point showing its history across reporting periods.
    /// This includes previous period values, rollover information, and audit trail within the current period.
    /// </summary>
    /// <param name="id">Data point ID</param>
    /// <param name="maxHistoryDepth">Maximum number of previous periods to include (default: 10, max: 50)</param>
    /// <returns>Cross-period lineage response with historical snapshots</returns>
    [HttpGet("{id}/cross-period-lineage")]
    public ActionResult<CrossPeriodLineageResponse> GetCrossPeriodLineage(string id, [FromQuery] int maxHistoryDepth = 10)
    {
        // Validate max history depth
        if (maxHistoryDepth < 1)
        {
            return BadRequest(new { error = "maxHistoryDepth must be at least 1." });
        }
        
        if (maxHistoryDepth > 50)
        {
            return BadRequest(new { error = "maxHistoryDepth cannot exceed 50." });
        }
        
        var lineage = _store.GetCrossPeriodLineage(id, maxHistoryDepth);
        
        if (lineage == null)
        {
            return NotFound(new { error = $"DataPoint with ID '{id}' not found or has no accessible lineage." });
        }
        
        return Ok(lineage);
    }

    /// <summary>
    /// Compares a numeric metric across reporting periods for year-over-year analysis.
    /// Shows current value, prior value, and percentage change.
    /// </summary>
    /// <param name="id">Data point ID</param>
    /// <param name="priorPeriodId">Optional ID of the prior period to compare against. If not provided, uses the most recent prior period.</param>
    /// <returns>Metric comparison response with values and percentage change</returns>
    [HttpGet("{id}/compare-periods")]
    public ActionResult<MetricComparisonResponse> CompareMetrics(string id, [FromQuery] string? priorPeriodId = null)
    {
        var comparison = _store.CompareMetrics(id, priorPeriodId);
        
        if (comparison == null)
        {
            return NotFound(new { error = $"DataPoint with ID '{id}' not found." });
        }
        
        return Ok(comparison);
    }

    /// <summary>
    /// Compares narrative text disclosures between reporting periods to show year-over-year changes.
    /// Supports word-level and sentence-level diffs with draft copy detection.
    /// </summary>
    /// <param name="id">Current data point ID</param>
    /// <param name="previousPeriodId">Optional ID of the previous period to compare against. If not provided, uses rollover lineage.</param>
    /// <param name="granularity">Diff granularity: "word" or "sentence". Defaults to "word".</param>
    /// <returns>Text disclosure comparison response with highlighted changes</returns>
    [HttpGet("{id}/compare-text")]
    public ActionResult<TextDisclosureComparisonResponse> CompareTextDisclosures(
        string id, 
        [FromQuery] string? previousPeriodId = null,
        [FromQuery] string granularity = "word")
    {
        if (granularity != "word" && granularity != "sentence")
        {
            return BadRequest(new { error = "Granularity must be 'word' or 'sentence'." });
        }

        var (success, errorMessage, response) = _store.CompareTextDisclosures(id, previousPeriodId, granularity);
        
        if (!success)
        {
            return NotFound(new { error = errorMessage });
        }
        
        return Ok(response);
    }
}
