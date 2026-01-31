using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for fragment audit operations.
/// Provides endpoints to trace any report fragment back to sources, evidence, and decisions.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/fragment-audit")]
public class FragmentAuditController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public FragmentAuditController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get audit view for a specific fragment.
    /// Provides complete traceability including linked sources, evidence, decisions, assumptions, and gaps.
    /// </summary>
    /// <param name="fragmentType">Type of fragment: 'section' or 'data-point'</param>
    /// <param name="fragmentId">Unique identifier of the fragment</param>
    /// <returns>FragmentAuditView with all traceability information</returns>
    /// <response code="200">Returns the fragment audit view</response>
    /// <response code="404">Fragment not found</response>
    [HttpGet("{fragmentType}/{fragmentId}")]
    [ProducesResponseType(typeof(FragmentAuditView), 200)]
    [ProducesResponseType(404)]
    public ActionResult<FragmentAuditView> GetFragmentAuditView(string fragmentType, string fragmentId)
    {
        var auditView = _store.GetFragmentAuditView(fragmentType, fragmentId);
        
        if (auditView == null)
        {
            return NotFound(new { message = $"Fragment of type '{fragmentType}' with ID '{fragmentId}' not found." });
        }

        return Ok(auditView);
    }

    /// <summary>
    /// Generate stable fragment identifier for export mapping.
    /// Used by export processes to create stable identifiers for PDF/DOCX mapping.
    /// </summary>
    /// <param name="fragmentType">Type of fragment: 'section' or 'data-point'</param>
    /// <param name="fragmentId">Unique identifier of the fragment</param>
    /// <returns>Stable fragment identifier</returns>
    /// <response code="200">Returns the stable fragment identifier</response>
    [HttpGet("{fragmentType}/{fragmentId}/stable-identifier")]
    [ProducesResponseType(typeof(string), 200)]
    public ActionResult<string> GetStableFragmentIdentifier(string fragmentType, string fragmentId)
    {
        var identifier = _store.GenerateStableFragmentIdentifier(fragmentType, fragmentId);
        return Ok(new { stableFragmentIdentifier = identifier });
    }
}
