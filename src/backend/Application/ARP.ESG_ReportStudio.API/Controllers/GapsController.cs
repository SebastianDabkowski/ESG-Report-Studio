using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/gaps")]
public sealed class GapsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public GapsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get a specific gap by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Gap> GetGapById(string id)
    {
        var gap = _store.GetGapById(id);
        if (gap == null)
        {
            return NotFound(new { error = $"Gap with ID '{id}' not found." });
        }

        return Ok(gap);
    }

    /// <summary>
    /// Create a new gap.
    /// </summary>
    [HttpPost]
    public ActionResult<Gap> CreateGap([FromBody] CreateGapRequest request)
    {
        var (isValid, errorMessage, gap) = _store.CreateGap(
            request.SectionId,
            request.Title,
            request.Description,
            request.Impact,
            request.ImprovementPlan,
            request.TargetDate,
            User?.Identity?.Name ?? "anonymous"
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetGapById), new { id = gap!.Id }, gap);
    }

    /// <summary>
    /// Update an existing gap.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<Gap> UpdateGap(string id, [FromBody] UpdateGapRequest request)
    {
        var (isValid, errorMessage, gap) = _store.UpdateGap(
            id,
            request.Title,
            request.Description,
            request.Impact,
            request.ImprovementPlan,
            request.TargetDate,
            User?.Identity?.Name ?? "anonymous",
            request.ChangeNote
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(gap);
    }

    /// <summary>
    /// Resolve a gap.
    /// </summary>
    [HttpPost("{id}/resolve")]
    public ActionResult<Gap> ResolveGap(string id, [FromBody] ResolveGapRequest request)
    {
        var (isValid, errorMessage, gap) = _store.ResolveGap(
            id,
            User?.Identity?.Name ?? "anonymous",
            request.ResolutionNote
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(gap);
    }

    /// <summary>
    /// Reopen a resolved gap.
    /// </summary>
    [HttpPost("{id}/reopen")]
    public ActionResult<Gap> ReopenGap(string id, [FromBody] ResolveGapRequest request)
    {
        var (isValid, errorMessage, gap) = _store.ReopenGap(
            id,
            User?.Identity?.Name ?? "anonymous",
            request.ResolutionNote
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(gap);
    }
}
