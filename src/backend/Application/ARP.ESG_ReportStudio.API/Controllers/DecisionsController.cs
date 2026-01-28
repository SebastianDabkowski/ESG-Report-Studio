using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing decision log entries.
/// Supports ADR-like decision recording with versioning and audit trails.
/// </summary>
[ApiController]
[Route("api/decisions")]
public sealed class DecisionsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public DecisionsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all decisions, optionally filtered by section.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<Decision>> GetDecisions([FromQuery] string? sectionId)
    {
        return Ok(_store.GetDecisions(sectionId));
    }

    /// <summary>
    /// Get a specific decision by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Decision> GetDecisionById(string id)
    {
        var decision = _store.GetDecisionById(id);
        if (decision == null)
        {
            return NotFound(new { error = $"Decision with ID '{id}' not found." });
        }

        return Ok(decision);
    }

    /// <summary>
    /// Get version history for a decision.
    /// </summary>
    [HttpGet("{id}/versions")]
    public ActionResult<IReadOnlyList<DecisionVersion>> GetDecisionVersionHistory(string id)
    {
        // Verify decision exists
        var decision = _store.GetDecisionById(id);
        if (decision == null)
        {
            return NotFound(new { error = $"Decision with ID '{id}' not found." });
        }

        var versions = _store.GetDecisionVersionHistory(id);
        return Ok(versions);
    }

    /// <summary>
    /// Create a new decision.
    /// </summary>
    [HttpPost]
    public ActionResult<Decision> CreateDecision([FromBody] CreateDecisionRequest request)
    {
        var (isValid, errorMessage, decision) = _store.CreateDecision(
            request.SectionId,
            request.Title,
            request.Context,
            request.DecisionText,
            request.Alternatives,
            request.Consequences,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetDecisionById), new { id = decision!.Id }, decision);
    }

    /// <summary>
    /// Update an existing decision. Creates a new version; prior versions are preserved as read-only.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<Decision> UpdateDecision(string id, [FromBody] UpdateDecisionRequest request)
    {
        var (isValid, errorMessage, decision) = _store.UpdateDecision(
            id,
            request.Title,
            request.Context,
            request.DecisionText,
            request.Alternatives,
            request.Consequences,
            request.ChangeNote,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(decision);
    }

    /// <summary>
    /// Deprecate a decision (mark as no longer applicable).
    /// </summary>
    [HttpPost("{id}/deprecate")]
    public ActionResult<Decision> DeprecateDecision(string id, [FromBody] DeprecateDecisionRequest request)
    {
        var (isValid, errorMessage, decision) = _store.DeprecateDecision(
            id,
            request.Reason,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(decision);
    }

    /// <summary>
    /// Link a decision to a report fragment (data point).
    /// </summary>
    [HttpPost("{id}/link")]
    public ActionResult LinkDecisionToFragment(string id, [FromBody] LinkDecisionRequest request)
    {
        var (isValid, errorMessage) = _store.LinkDecisionToFragment(id, request.FragmentId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Decision linked to fragment successfully." });
    }

    /// <summary>
    /// Unlink a decision from a report fragment.
    /// </summary>
    [HttpPost("{id}/unlink")]
    public ActionResult UnlinkDecisionFromFragment(string id, [FromBody] UnlinkDecisionRequest request)
    {
        var (isValid, errorMessage) = _store.UnlinkDecisionFromFragment(id, request.FragmentId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Decision unlinked from fragment successfully." });
    }

    /// <summary>
    /// Get all decisions referenced by a specific fragment.
    /// </summary>
    [HttpGet("fragment/{fragmentId}")]
    public ActionResult<IReadOnlyList<Decision>> GetDecisionsByFragment(string fragmentId)
    {
        var decisions = _store.GetDecisionsByFragment(fragmentId);
        return Ok(decisions);
    }

    /// <summary>
    /// Delete a decision. Only allowed if not referenced by any fragments.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteDecision(string id)
    {
        var (isValid, errorMessage) = _store.DeleteDecision(id);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Decision deleted successfully." });
    }
}

/// <summary>
/// Request to deprecate a decision.
/// </summary>
public sealed class DeprecateDecisionRequest
{
    public string Reason { get; set; } = string.Empty;
}
