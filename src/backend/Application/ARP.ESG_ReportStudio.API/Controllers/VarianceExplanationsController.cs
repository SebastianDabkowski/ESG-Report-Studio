using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/variance-explanations")]
public sealed class VarianceExplanationsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public VarianceExplanationsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Creates a variance threshold configuration for a reporting period.
    /// </summary>
    [HttpPost("threshold-config/{periodId}")]
    public ActionResult<VarianceThresholdConfig> CreateThresholdConfig(
        string periodId,
        [FromBody] CreateVarianceThresholdConfigRequest request)
    {
        var (isValid, errorMessage, config) = _store.CreateVarianceThresholdConfig(periodId, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(config);
    }

    /// <summary>
    /// Creates a variance explanation for a significant metric change.
    /// </summary>
    [HttpPost]
    public ActionResult<VarianceExplanation> CreateExplanation([FromBody] CreateVarianceExplanationRequest request)
    {
        var (isValid, errorMessage, explanation) = _store.CreateVarianceExplanation(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetExplanation), new { id = explanation!.Id }, explanation);
    }

    /// <summary>
    /// Gets all variance explanations, optionally filtered by data point or period.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<VarianceExplanation>> GetExplanations(
        [FromQuery] string? dataPointId = null,
        [FromQuery] string? periodId = null)
    {
        var explanations = _store.GetVarianceExplanations(dataPointId, periodId);
        return Ok(explanations);
    }

    /// <summary>
    /// Gets a single variance explanation by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<VarianceExplanation> GetExplanation(string id)
    {
        var explanation = _store.GetVarianceExplanation(id);
        if (explanation == null)
        {
            return NotFound(new { error = $"Variance explanation with ID '{id}' not found." });
        }

        return Ok(explanation);
    }

    /// <summary>
    /// Updates a variance explanation.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<VarianceExplanation> UpdateExplanation(
        string id,
        [FromBody] UpdateVarianceExplanationRequest request)
    {
        var (isValid, errorMessage, explanation) = _store.UpdateVarianceExplanation(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(explanation);
    }

    /// <summary>
    /// Submits a variance explanation for review (or marks as complete if review not required).
    /// </summary>
    [HttpPost("{id}/submit")]
    public ActionResult<VarianceExplanation> SubmitExplanation(
        string id,
        [FromBody] SubmitVarianceExplanationRequest request)
    {
        var (isValid, errorMessage, explanation) = _store.SubmitVarianceExplanation(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(explanation);
    }

    /// <summary>
    /// Reviews a variance explanation (approve, reject, or request revision).
    /// </summary>
    [HttpPost("{id}/review")]
    public ActionResult<VarianceExplanation> ReviewExplanation(
        string id,
        [FromBody] ReviewVarianceExplanationRequest request)
    {
        var (isValid, errorMessage, explanation) = _store.ReviewVarianceExplanation(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(explanation);
    }

    /// <summary>
    /// Deletes a variance explanation.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteExplanation(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "deletedBy query parameter is required." });
        }

        var deleted = _store.DeleteVarianceExplanation(id, deletedBy);
        if (!deleted)
        {
            return NotFound(new { error = $"Variance explanation with ID '{id}' not found." });
        }

        return NoContent();
    }
}
