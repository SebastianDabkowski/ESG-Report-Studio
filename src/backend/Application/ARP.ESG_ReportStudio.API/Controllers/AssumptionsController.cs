using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/assumptions")]
public sealed class AssumptionsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public AssumptionsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all assumptions, optionally filtered by section.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<Assumption>> GetAssumptions([FromQuery] string? sectionId)
    {
        return Ok(_store.GetAssumptions(sectionId));
    }

    /// <summary>
    /// Get a specific assumption by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Assumption> GetAssumptionById(string id)
    {
        var assumption = _store.GetAssumptionById(id);
        if (assumption == null)
        {
            return NotFound(new { error = $"Assumption with ID '{id}' not found." });
        }

        return Ok(assumption);
    }

    /// <summary>
    /// Create a new assumption.
    /// </summary>
    [HttpPost]
    public ActionResult<Assumption> CreateAssumption([FromBody] CreateAssumptionRequest request)
    {
        var (isValid, errorMessage, assumption) = _store.CreateAssumption(
            request.SectionId,
            request.Title,
            request.Description,
            request.Scope,
            request.ValidityStartDate,
            request.ValidityEndDate,
            request.Methodology,
            request.Limitations,
            request.LinkedDataPointIds,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetAssumptionById), new { id = assumption!.Id }, assumption);
    }

    /// <summary>
    /// Update an existing assumption. All linked data points will reflect the updated version.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<Assumption> UpdateAssumption(string id, [FromBody] UpdateAssumptionRequest request)
    {
        var (isValid, errorMessage, assumption) = _store.UpdateAssumption(
            id,
            request.Title,
            request.Description,
            request.Scope,
            request.ValidityStartDate,
            request.ValidityEndDate,
            request.Methodology,
            request.Limitations,
            request.LinkedDataPointIds,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(assumption);
    }

    /// <summary>
    /// Deprecate an assumption. Requires either a replacement assumption or justification.
    /// </summary>
    [HttpPost("{id}/deprecate")]
    public ActionResult DeprecateAssumption(string id, [FromBody] DeprecateAssumptionRequest request)
    {
        var (isValid, errorMessage) = _store.DeprecateAssumption(
            id,
            request.ReplacementAssumptionId,
            request.Justification
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Link an assumption to a data point.
    /// </summary>
    [HttpPost("{assumptionId}/link")]
    public ActionResult LinkAssumptionToDataPoint(string assumptionId, [FromBody] LinkAssumptionRequest request)
    {
        var (isValid, errorMessage) = _store.LinkAssumptionToDataPoint(assumptionId, request.DataPointId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Unlink an assumption from a data point.
    /// </summary>
    [HttpPost("{assumptionId}/unlink")]
    public ActionResult UnlinkAssumptionFromDataPoint(string assumptionId, [FromBody] LinkAssumptionRequest request)
    {
        var (isValid, errorMessage) = _store.UnlinkAssumptionFromDataPoint(assumptionId, request.DataPointId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete an assumption. Cannot delete if used as replacement for other assumptions.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteAssumption(string id)
    {
        var (isValid, errorMessage) = _store.DeleteAssumption(id);
        if (!isValid)
        {
            return NotFound(new { error = errorMessage });
        }

        return NoContent();
    }
}

/// <summary>
/// Request to link/unlink an assumption to a data point.
/// </summary>
public sealed class LinkAssumptionRequest
{
    public string DataPointId { get; set; } = string.Empty;
}
