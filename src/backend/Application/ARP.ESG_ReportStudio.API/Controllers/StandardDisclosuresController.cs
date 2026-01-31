using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing standard disclosures (individual requirements within reporting standards).
/// </summary>
[ApiController]
[Route("api/standard-disclosures")]
public sealed class StandardDisclosuresController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public StandardDisclosuresController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets all standard disclosures with optional filtering.
    /// </summary>
    /// <param name="standardId">Optional filter by standard ID.</param>
    /// <param name="category">Optional filter by category (environmental, social, governance).</param>
    /// <param name="topic">Optional filter by topic.</param>
    /// <param name="mandatoryOnly">Optional filter to show only mandatory disclosures.</param>
    /// <returns>List of standard disclosures.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<StandardDisclosure>> GetStandardDisclosures(
        [FromQuery] string? standardId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? topic = null,
        [FromQuery] bool? mandatoryOnly = null)
    {
        return Ok(_store.GetStandardDisclosures(standardId, category, topic, mandatoryOnly));
    }

    /// <summary>
    /// Gets a specific standard disclosure by ID.
    /// </summary>
    /// <param name="id">The disclosure ID.</param>
    /// <returns>The disclosure details.</returns>
    [HttpGet("{id}")]
    public ActionResult<StandardDisclosure> GetStandardDisclosure(string id)
    {
        var disclosure = _store.GetStandardDisclosure(id);
        if (disclosure == null)
        {
            return NotFound(new { error = "Standard disclosure not found." });
        }

        return Ok(disclosure);
    }

    /// <summary>
    /// Creates a new standard disclosure.
    /// </summary>
    /// <param name="request">The disclosure creation request.</param>
    /// <returns>The created disclosure.</returns>
    [HttpPost]
    public ActionResult<StandardDisclosure> CreateStandardDisclosure([FromBody] CreateStandardDisclosureRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, disclosure) = _store.CreateStandardDisclosure(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetStandardDisclosure), new { id = disclosure!.Id }, disclosure);
    }

    /// <summary>
    /// Updates an existing standard disclosure.
    /// </summary>
    /// <param name="id">The disclosure ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated disclosure.</returns>
    [HttpPut("{id}")]
    public ActionResult<StandardDisclosure> UpdateStandardDisclosure(string id, [FromBody] UpdateStandardDisclosureRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, disclosure) = _store.UpdateStandardDisclosure(id, request, userId);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(disclosure);
    }

    /// <summary>
    /// Deletes a standard disclosure.
    /// Also deletes all mappings that reference this disclosure.
    /// </summary>
    /// <param name="id">The disclosure ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    public IActionResult DeleteStandardDisclosure(string id)
    {
        var success = _store.DeleteStandardDisclosure(id);
        
        if (!success)
        {
            return NotFound(new { error = "Standard disclosure not found." });
        }

        return NoContent();
    }
}
