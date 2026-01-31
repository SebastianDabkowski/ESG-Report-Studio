using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing mappings between report sections/data points and standard disclosures.
/// Supports many-to-many relationships for standards compliance tracking.
/// </summary>
[ApiController]
[Route("api/mappings")]
public sealed class MappingsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public MappingsController(InMemoryReportStore store)
    {
        _store = store;
    }

    #region Section Disclosure Mappings

    /// <summary>
    /// Gets all section-to-disclosure mappings with optional filtering.
    /// </summary>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="disclosureId">Optional filter by disclosure ID.</param>
    /// <returns>List of section disclosure mappings.</returns>
    [HttpGet("section-disclosures")]
    public ActionResult<IReadOnlyList<SectionDisclosureMapping>> GetSectionDisclosureMappings(
        [FromQuery] string? sectionId = null,
        [FromQuery] string? disclosureId = null)
    {
        return Ok(_store.GetSectionDisclosureMappings(sectionId, disclosureId));
    }

    /// <summary>
    /// Gets a specific section disclosure mapping by ID.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>The mapping details.</returns>
    [HttpGet("section-disclosures/{id}")]
    public ActionResult<SectionDisclosureMapping> GetSectionDisclosureMapping(string id)
    {
        var mapping = _store.GetSectionDisclosureMapping(id);
        if (mapping == null)
        {
            return NotFound(new { error = "Section disclosure mapping not found." });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Creates a new section-to-disclosure mapping.
    /// </summary>
    /// <param name="request">The mapping creation request.</param>
    /// <returns>The created mapping.</returns>
    [HttpPost("section-disclosures")]
    public ActionResult<SectionDisclosureMapping> CreateSectionDisclosureMapping([FromBody] CreateSectionDisclosureMappingRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, mapping) = _store.CreateSectionDisclosureMapping(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetSectionDisclosureMapping), new { id = mapping!.Id }, mapping);
    }

    /// <summary>
    /// Updates an existing section-to-disclosure mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated mapping.</returns>
    [HttpPut("section-disclosures/{id}")]
    public ActionResult<SectionDisclosureMapping> UpdateSectionDisclosureMapping(string id, [FromBody] UpdateSectionDisclosureMappingRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, mapping) = _store.UpdateSectionDisclosureMapping(id, request, userId);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Deletes a section disclosure mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("section-disclosures/{id}")]
    public IActionResult DeleteSectionDisclosureMapping(string id)
    {
        var success = _store.DeleteSectionDisclosureMapping(id);
        
        if (!success)
        {
            return NotFound(new { error = "Section disclosure mapping not found." });
        }

        return NoContent();
    }

    #endregion

    #region DataPoint Disclosure Mappings

    /// <summary>
    /// Gets all data-point-to-disclosure mappings with optional filtering.
    /// </summary>
    /// <param name="dataPointId">Optional filter by data point ID.</param>
    /// <param name="disclosureId">Optional filter by disclosure ID.</param>
    /// <returns>List of data point disclosure mappings.</returns>
    [HttpGet("datapoint-disclosures")]
    public ActionResult<IReadOnlyList<DataPointDisclosureMapping>> GetDataPointDisclosureMappings(
        [FromQuery] string? dataPointId = null,
        [FromQuery] string? disclosureId = null)
    {
        return Ok(_store.GetDataPointDisclosureMappings(dataPointId, disclosureId));
    }

    /// <summary>
    /// Gets a specific data point disclosure mapping by ID.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>The mapping details.</returns>
    [HttpGet("datapoint-disclosures/{id}")]
    public ActionResult<DataPointDisclosureMapping> GetDataPointDisclosureMapping(string id)
    {
        var mapping = _store.GetDataPointDisclosureMapping(id);
        if (mapping == null)
        {
            return NotFound(new { error = "Data point disclosure mapping not found." });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Creates a new data-point-to-disclosure mapping.
    /// </summary>
    /// <param name="request">The mapping creation request.</param>
    /// <returns>The created mapping.</returns>
    [HttpPost("datapoint-disclosures")]
    public ActionResult<DataPointDisclosureMapping> CreateDataPointDisclosureMapping([FromBody] CreateDataPointDisclosureMappingRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, mapping) = _store.CreateDataPointDisclosureMapping(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetDataPointDisclosureMapping), new { id = mapping!.Id }, mapping);
    }

    /// <summary>
    /// Updates an existing data-point-to-disclosure mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated mapping.</returns>
    [HttpPut("datapoint-disclosures/{id}")]
    public ActionResult<DataPointDisclosureMapping> UpdateDataPointDisclosureMapping(string id, [FromBody] UpdateDataPointDisclosureMappingRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, mapping) = _store.UpdateDataPointDisclosureMapping(id, request, userId);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(mapping);
    }

    /// <summary>
    /// Deletes a data point disclosure mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("datapoint-disclosures/{id}")]
    public IActionResult DeleteDataPointDisclosureMapping(string id)
    {
        var success = _store.DeleteDataPointDisclosureMapping(id);
        
        if (!success)
        {
            return NotFound(new { error = "Data point disclosure mapping not found." });
        }

        return NoContent();
    }

    #endregion

    #region Mapping Versions

    /// <summary>
    /// Gets all mapping versions for a reporting period.
    /// </summary>
    /// <param name="periodId">The reporting period ID.</param>
    /// <returns>List of mapping versions.</returns>
    [HttpGet("versions")]
    public ActionResult<IReadOnlyList<MappingVersion>> GetMappingVersions([FromQuery] string periodId)
    {
        if (string.IsNullOrEmpty(periodId))
        {
            return BadRequest(new { error = "Period ID is required." });
        }

        return Ok(_store.GetMappingVersions(periodId));
    }

    /// <summary>
    /// Gets a specific mapping version by ID.
    /// </summary>
    /// <param name="id">The version ID.</param>
    /// <returns>The version details.</returns>
    [HttpGet("versions/{id}")]
    public ActionResult<MappingVersion> GetMappingVersion(string id)
    {
        var version = _store.GetMappingVersion(id);
        if (version == null)
        {
            return NotFound(new { error = "Mapping version not found." });
        }

        return Ok(version);
    }

    /// <summary>
    /// Creates a new mapping version snapshot for a reporting period.
    /// Captures the current state of all mappings for export consistency.
    /// </summary>
    /// <param name="request">The version creation request.</param>
    /// <returns>The created version.</returns>
    [HttpPost("versions")]
    public ActionResult<MappingVersion> CreateMappingVersion([FromBody] CreateMappingVersionRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, version) = _store.CreateMappingVersion(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetMappingVersion), new { id = version!.Id }, version);
    }

    #endregion
}
