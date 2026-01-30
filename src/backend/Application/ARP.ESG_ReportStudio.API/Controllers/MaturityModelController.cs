using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/maturity-models")]
public sealed class MaturityModelController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public MaturityModelController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets all maturity models.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive/historical versions.</param>
    [HttpGet]
    public ActionResult<IReadOnlyList<MaturityModel>> GetMaturityModels([FromQuery] bool includeInactive = false)
    {
        var models = _store.GetMaturityModels(includeInactive);
        return Ok(models);
    }

    /// <summary>
    /// Gets the active maturity model.
    /// </summary>
    [HttpGet("active")]
    public ActionResult<MaturityModel> GetActiveMaturityModel()
    {
        var model = _store.GetActiveMaturityModel();
        if (model == null)
        {
            return NotFound(new { error = "No active maturity model found." });
        }

        return Ok(model);
    }

    /// <summary>
    /// Gets a specific maturity model by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<MaturityModel> GetMaturityModel(string id)
    {
        var model = _store.GetMaturityModel(id);
        if (model == null)
        {
            return NotFound(new { error = "Maturity model not found." });
        }

        return Ok(model);
    }

    /// <summary>
    /// Gets version history for a maturity model.
    /// </summary>
    [HttpGet("{id}/versions")]
    public ActionResult<IReadOnlyList<MaturityModel>> GetMaturityModelVersionHistory(string id)
    {
        var versions = _store.GetMaturityModelVersionHistory(id);
        if (versions.Count == 0)
        {
            return NotFound(new { error = "Maturity model not found." });
        }

        return Ok(versions);
    }

    /// <summary>
    /// Creates a new maturity model.
    /// </summary>
    [HttpPost]
    public ActionResult<MaturityModel> CreateMaturityModel([FromBody] CreateMaturityModelRequest request)
    {
        var (isValid, errorMessage, model) = _store.CreateMaturityModel(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetMaturityModel), new { id = model!.Id }, model);
    }

    /// <summary>
    /// Updates a maturity model by creating a new version.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<MaturityModel> UpdateMaturityModel(string id, [FromBody] UpdateMaturityModelRequest request)
    {
        var (isValid, errorMessage, model) = _store.UpdateMaturityModel(id, request);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(model);
    }

    /// <summary>
    /// Deletes a maturity model.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteMaturityModel(string id)
    {
        var (isValid, errorMessage) = _store.DeleteMaturityModel(id);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Maturity model deleted successfully." });
    }
}
