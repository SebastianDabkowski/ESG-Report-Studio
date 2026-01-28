using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/simplifications")]
public sealed class SimplificationsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public SimplificationsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all simplifications, optionally filtered by section.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<Simplification>> GetSimplifications([FromQuery] string? sectionId)
    {
        return Ok(_store.GetSimplifications(sectionId));
    }

    /// <summary>
    /// Get a specific simplification by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Simplification> GetSimplificationById(string id)
    {
        var simplification = _store.GetSimplificationById(id);
        if (simplification == null)
        {
            return NotFound(new { error = $"Simplification with ID '{id}' not found." });
        }

        return Ok(simplification);
    }

    /// <summary>
    /// Create a new simplification.
    /// </summary>
    [HttpPost]
    public ActionResult<Simplification> CreateSimplification([FromBody] CreateSimplificationRequest request)
    {
        var (isValid, errorMessage, simplification) = _store.CreateSimplification(
            request.SectionId,
            request.Title,
            request.Description,
            request.AffectedEntities,
            request.AffectedSites,
            request.AffectedProcesses,
            request.ImpactLevel,
            request.ImpactNotes,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetSimplificationById), new { id = simplification!.Id }, simplification);
    }

    /// <summary>
    /// Update an existing simplification.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<Simplification> UpdateSimplification(string id, [FromBody] UpdateSimplificationRequest request)
    {
        var (isValid, errorMessage, simplification) = _store.UpdateSimplification(
            id,
            request.Title,
            request.Description,
            request.AffectedEntities,
            request.AffectedSites,
            request.AffectedProcesses,
            request.ImpactLevel,
            request.ImpactNotes,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(simplification);
    }

    /// <summary>
    /// Delete a simplification. Records the deletion in the audit log.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteSimplification(string id)
    {
        var (isValid, errorMessage) = _store.DeleteSimplification(id, User?.Identity?.Name ?? "anonymous");
        if (!isValid)
        {
            return NotFound(new { error = errorMessage });
        }

        return NoContent();
    }
}
