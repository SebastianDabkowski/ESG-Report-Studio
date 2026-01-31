using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing the standards catalogue (e.g., CSRD/ESRS, SME model).
/// Standards are data-driven configurations that define reporting frameworks.
/// </summary>
[ApiController]
[Route("api/standards-catalog")]
public sealed class StandardsCatalogController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public StandardsCatalogController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets all standards from the catalogue.
    /// </summary>
    /// <param name="includeDeprecated">If true, includes deprecated standards in the results. Default is false.</param>
    /// <returns>List of standards.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<StandardsCatalogItem>> GetStandardsCatalog([FromQuery] bool includeDeprecated = false)
    {
        return Ok(_store.GetStandardsCatalog(includeDeprecated));
    }

    /// <summary>
    /// Gets a specific standard by ID.
    /// </summary>
    /// <param name="id">The standard ID.</param>
    /// <returns>The standard details.</returns>
    [HttpGet("{id}")]
    public ActionResult<StandardsCatalogItem> GetStandard(string id)
    {
        var item = _store.GetStandard(id);
        if (item == null)
        {
            return NotFound(new { error = "Standard not found." });
        }

        return Ok(item);
    }

    /// <summary>
    /// Creates a new reporting standard in the catalogue.
    /// </summary>
    /// <param name="request">The standard creation request.</param>
    /// <returns>The created standard.</returns>
    [HttpPost]
    public ActionResult<StandardsCatalogItem> CreateStandard([FromBody] CreateStandardRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, item) = _store.CreateStandard(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetStandard), new { id = item!.Id }, item);
    }

    /// <summary>
    /// Updates an existing standard.
    /// </summary>
    /// <param name="id">The standard ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated standard.</returns>
    [HttpPut("{id}")]
    public ActionResult<StandardsCatalogItem> UpdateStandard(string id, [FromBody] UpdateStandardRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, item) = _store.UpdateStandard(id, request, userId);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(item);
    }

    /// <summary>
    /// Marks a standard as deprecated.
    /// Deprecated standards are not selectable by default for new reports.
    /// </summary>
    /// <param name="id">The standard ID.</param>
    /// <returns>Success message.</returns>
    [HttpPost("{id}/deprecate")]
    public ActionResult DeprecateStandard(string id)
    {
        var (isValid, errorMessage) = _store.DeprecateStandard(id);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Standard has been deprecated successfully." });
    }

    /// <summary>
    /// Gets all section mappings for a specific standard.
    /// Mappings define how standard references (e.g., "ESRS E1") map to platform sections.
    /// </summary>
    /// <param name="id">The standard ID.</param>
    /// <returns>List of mappings.</returns>
    [HttpGet("{id}/mappings")]
    public ActionResult<IReadOnlyList<StandardSectionMapping>> GetStandardMappings(string id)
    {
        // Verify the standard exists
        var standard = _store.GetStandard(id);
        if (standard == null)
        {
            return NotFound(new { error = "Standard not found." });
        }

        return Ok(_store.GetStandardMappings(id));
    }

    /// <summary>
    /// Creates a new mapping between a standard reference and a section.
    /// </summary>
    /// <param name="request">The mapping creation request.</param>
    /// <returns>The created mapping.</returns>
    [HttpPost("mappings")]
    public ActionResult<StandardSectionMapping> CreateStandardMapping([FromBody] CreateStandardMappingRequest request)
    {
        // In a real application, this would come from authenticated user context
        var userId = "system"; // TODO: Get from authenticated user context
        
        var (isValid, errorMessage, mapping) = _store.CreateStandardMapping(request, userId);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetStandardMappings), new { id = mapping!.StandardId }, mapping);
    }

    /// <summary>
    /// Deletes a standard-to-section mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>Success message.</returns>
    [HttpDelete("mappings/{id}")]
    public ActionResult DeleteStandardMapping(string id)
    {
        var success = _store.DeleteStandardMapping(id);
        
        if (!success)
        {
            return NotFound(new { error = "Mapping not found." });
        }

        return Ok(new { message = "Mapping has been deleted successfully." });
    }
}
