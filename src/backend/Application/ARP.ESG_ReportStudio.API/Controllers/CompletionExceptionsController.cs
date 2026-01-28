using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing completion exceptions in ESG reports.
/// Exceptions allow controlled gaps with explicit justification and approval.
/// </summary>
[ApiController]
[Route("api/completion-exceptions")]
public sealed class CompletionExceptionsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public CompletionExceptionsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets all completion exceptions, optionally filtered by section or status.
    /// </summary>
    /// <param name="sectionId">Optional section ID filter.</param>
    /// <param name="status">Optional status filter (pending, accepted, rejected).</param>
    /// <returns>List of completion exceptions.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<CompletionException>> GetCompletionExceptions(
        [FromQuery] string? sectionId = null,
        [FromQuery] string? status = null)
    {
        var exceptions = _store.GetCompletionExceptions(sectionId, status);
        return Ok(exceptions);
    }

    /// <summary>
    /// Gets a specific completion exception by ID.
    /// </summary>
    /// <param name="id">Exception ID.</param>
    /// <returns>Completion exception details.</returns>
    [HttpGet("{id}")]
    public ActionResult<CompletionException> GetCompletionException(string id)
    {
        var exception = _store.GetCompletionException(id);
        if (exception == null)
        {
            return NotFound(new { error = $"Completion exception with ID '{id}' not found." });
        }

        return Ok(exception);
    }

    /// <summary>
    /// Creates a new completion exception request.
    /// </summary>
    /// <param name="request">Exception creation details.</param>
    /// <returns>Created exception.</returns>
    [HttpPost]
    public ActionResult<CompletionException> CreateCompletionException([FromBody] CreateCompletionExceptionRequest request)
    {
        var (isValid, errorMessage, exception) = _store.CreateCompletionException(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetCompletionException), new { id = exception!.Id }, exception);
    }

    /// <summary>
    /// Approves a pending completion exception.
    /// Restricted to users with appropriate permissions (admin, report-owner).
    /// </summary>
    /// <param name="id">Exception ID.</param>
    /// <param name="request">Approval details.</param>
    /// <returns>Updated exception.</returns>
    [HttpPost("{id}/approve")]
    public ActionResult<CompletionException> ApproveCompletionException(
        string id,
        [FromBody] ApproveCompletionExceptionRequest request)
    {
        // TODO: Add role-based authorization check here
        // For now, we'll rely on the approvedBy field being validated

        var (isValid, errorMessage, exception) = _store.ApproveCompletionException(id, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(exception);
    }

    /// <summary>
    /// Rejects a pending completion exception.
    /// Restricted to users with appropriate permissions (admin, report-owner).
    /// </summary>
    /// <param name="id">Exception ID.</param>
    /// <param name="request">Rejection details.</param>
    /// <returns>Updated exception.</returns>
    [HttpPost("{id}/reject")]
    public ActionResult<CompletionException> RejectCompletionException(
        string id,
        [FromBody] RejectCompletionExceptionRequest request)
    {
        // TODO: Add role-based authorization check here
        // For now, we'll rely on the rejectedBy field being validated

        var (isValid, errorMessage, exception) = _store.RejectCompletionException(id, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(exception);
    }

    /// <summary>
    /// Deletes a completion exception.
    /// Only pending exceptions can be deleted to preserve audit trail.
    /// </summary>
    /// <param name="id">Exception ID.</param>
    /// <param name="deletedBy">User ID performing the deletion.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    public ActionResult DeleteCompletionException(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "DeletedBy user ID is required." });
        }

        var exception = _store.GetCompletionException(id);
        if (exception == null)
        {
            return NotFound(new { error = $"Completion exception with ID '{id}' not found." });
        }

        if (exception.Status != "pending")
        {
            return BadRequest(new { error = $"Cannot delete exception with status '{exception.Status}'. Only pending exceptions can be deleted." });
        }

        var deleted = _store.DeleteCompletionException(id, deletedBy);
        if (!deleted)
        {
            return NotFound(new { error = $"Completion exception with ID '{id}' not found." });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets a completeness validation report for a reporting period.
    /// Lists missing, estimated, and simplified items by section with exception status.
    /// </summary>
    /// <param name="periodId">Reporting period ID.</param>
    /// <returns>Completeness validation report.</returns>
    [HttpGet("validation-report")]
    public ActionResult<CompletenessValidationReport> GetValidationReport([FromQuery] string periodId)
    {
        if (string.IsNullOrWhiteSpace(periodId))
        {
            return BadRequest(new { error = "Period ID is required." });
        }

        var report = _store.GetCompletenessValidationReport(periodId);
        return Ok(report);
    }
}
