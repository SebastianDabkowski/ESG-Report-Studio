using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/data-points")]
public sealed class DataPointsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public DataPointsController(InMemoryReportStore store)
    {
        _store = store;
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

        return CreatedAtAction(nameof(GetDataPoint), new { id = dataPoint!.Id }, dataPoint);
    }

    [HttpPut("{id}")]
    public ActionResult<DataPoint> UpdateDataPoint(string id, [FromBody] UpdateDataPointRequest request)
    {
        var (isValid, errorMessage, dataPoint) = _store.UpdateDataPoint(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(dataPoint);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteDataPoint(string id)
    {
        var deleted = _store.DeleteDataPoint(id);
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
}
