using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/remediation-plans")]
public sealed class RemediationPlansController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public RemediationPlansController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all remediation plans, optionally filtered by section.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<RemediationPlan>> GetRemediationPlans([FromQuery] string? sectionId)
    {
        return Ok(_store.GetRemediationPlans(sectionId));
    }

    /// <summary>
    /// Get a specific remediation plan by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<RemediationPlan> GetRemediationPlanById(string id)
    {
        var plan = _store.GetRemediationPlanById(id);
        if (plan == null)
        {
            return NotFound(new { error = $"Remediation plan with ID '{id}' not found." });
        }

        return Ok(plan);
    }

    /// <summary>
    /// Create a new remediation plan.
    /// </summary>
    [HttpPost]
    public ActionResult<RemediationPlan> CreateRemediationPlan([FromBody] CreateRemediationPlanRequest request)
    {
        var (isValid, errorMessage, plan) = _store.CreateRemediationPlan(
            request.SectionId,
            request.Title,
            request.Description,
            request.TargetPeriod,
            request.OwnerId,
            request.OwnerName,
            request.Priority,
            request.GapId,
            request.AssumptionId,
            request.DataPointId,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetRemediationPlanById), new { id = plan!.Id }, plan);
    }

    /// <summary>
    /// Update an existing remediation plan.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<RemediationPlan> UpdateRemediationPlan(string id, [FromBody] UpdateRemediationPlanRequest request)
    {
        var (isValid, errorMessage, plan) = _store.UpdateRemediationPlan(
            id,
            request.Title,
            request.Description,
            request.TargetPeriod,
            request.OwnerId,
            request.OwnerName,
            request.Priority,
            request.Status,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(plan);
    }

    /// <summary>
    /// Mark a remediation plan as completed.
    /// </summary>
    [HttpPost("{id}/complete")]
    public ActionResult<RemediationPlan> CompleteRemediationPlan(string id, [FromBody] CompleteRemediationPlanRequest request)
    {
        var (isValid, errorMessage, plan) = _store.CompleteRemediationPlan(
            id,
            request.CompletedBy
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(plan);
    }

    /// <summary>
    /// Delete a remediation plan and all its associated actions.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteRemediationPlan(string id)
    {
        var deleted = _store.DeleteRemediationPlan(id, User?.Identity?.Name ?? "anonymous");
        if (!deleted)
        {
            return NotFound(new { error = $"Remediation plan with ID '{id}' not found." });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all actions for a remediation plan.
    /// </summary>
    [HttpGet("{planId}/actions")]
    public ActionResult<IReadOnlyList<RemediationAction>> GetRemediationActions(string planId)
    {
        // Verify plan exists
        var plan = _store.GetRemediationPlanById(planId);
        if (plan == null)
        {
            return NotFound(new { error = $"Remediation plan with ID '{planId}' not found." });
        }

        return Ok(_store.GetRemediationActions(planId));
    }

    /// <summary>
    /// Get a specific remediation action by ID.
    /// </summary>
    [HttpGet("actions/{id}")]
    public ActionResult<RemediationAction> GetRemediationActionById(string id)
    {
        var action = _store.GetRemediationActionById(id);
        if (action == null)
        {
            return NotFound(new { error = $"Remediation action with ID '{id}' not found." });
        }

        return Ok(action);
    }

    /// <summary>
    /// Create a new remediation action.
    /// </summary>
    [HttpPost("actions")]
    public ActionResult<RemediationAction> CreateRemediationAction([FromBody] CreateRemediationActionRequest request)
    {
        var (isValid, errorMessage, action) = _store.CreateRemediationAction(
            request.RemediationPlanId,
            request.Title,
            request.Description,
            request.OwnerId,
            request.OwnerName,
            request.DueDate,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetRemediationActionById), new { id = action!.Id }, action);
    }

    /// <summary>
    /// Update an existing remediation action.
    /// </summary>
    [HttpPut("actions/{id}")]
    public ActionResult<RemediationAction> UpdateRemediationAction(string id, [FromBody] UpdateRemediationActionRequest request)
    {
        var (isValid, errorMessage, action) = _store.UpdateRemediationAction(
            id,
            request.Title,
            request.Description,
            request.OwnerId,
            request.OwnerName,
            request.DueDate,
            request.Status,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(action);
    }

    /// <summary>
    /// Mark a remediation action as completed with optional evidence and notes.
    /// </summary>
    [HttpPost("actions/{id}/complete")]
    public ActionResult<RemediationAction> CompleteRemediationAction(string id, [FromBody] CompleteRemediationActionRequest request)
    {
        var (isValid, errorMessage, action) = _store.CompleteRemediationAction(
            id,
            request.CompletedBy,
            request.CompletionNotes,
            request.EvidenceIds
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(action);
    }

    /// <summary>
    /// Delete a remediation action.
    /// </summary>
    [HttpDelete("actions/{id}")]
    public ActionResult DeleteRemediationAction(string id)
    {
        var deleted = _store.DeleteRemediationAction(id, User?.Identity?.Name ?? "anonymous");
        if (!deleted)
        {
            return NotFound(new { error = $"Remediation action with ID '{id}' not found." });
        }

        return NoContent();
    }
}
