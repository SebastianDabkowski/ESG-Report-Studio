using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing section status transitions and locking.
/// </summary>
/// <remarks>
/// SECURITY NOTE: In production, this controller should have:
/// - [Authorize] attribute at controller level for authenticated access
/// - Role-based authorization checks to ensure:
///   * Only section owners or contributors can submit sections for approval
///   * Only reviewers/approvers can approve or request changes
///   * Users can only create revisions of sections they have access to
/// - Additional authorization checks in each method to validate user permissions
/// </remarks>
[ApiController]
[Route("api/sections")]
public sealed class SectionStatusController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<SectionStatusController> _logger;

    public SectionStatusController(
        InMemoryReportStore store,
        ILogger<SectionStatusController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Submits a section for approval, locking it from edits.
    /// </summary>
    [HttpPost("{sectionId}/submit-for-approval")]
    public ActionResult<ReportSection> SubmitSectionForApproval(
        string sectionId,
        [FromBody] SubmitSectionForApprovalRequest request)
    {
        var (isValid, errorMessage, section) = _store.SubmitSectionForApproval(sectionId, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation(
            "Section {SectionId} submitted for approval by {UserId}",
            sectionId,
            request.SubmittedBy);

        return Ok(section);
    }

    /// <summary>
    /// Approves a submitted section.
    /// </summary>
    [HttpPost("{sectionId}/approve")]
    public ActionResult<ReportSection> ApproveSection(
        string sectionId,
        [FromBody] ApproveSectionRequest request)
    {
        var (isValid, errorMessage, section) = _store.ApproveSection(sectionId, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation(
            "Section {SectionId} approved by {UserId}",
            sectionId,
            request.ApprovedBy);

        return Ok(section);
    }

    /// <summary>
    /// Requests changes on a submitted section, unlocking it for edits.
    /// </summary>
    [HttpPost("{sectionId}/request-changes")]
    public ActionResult<ReportSection> RequestSectionChanges(
        string sectionId,
        [FromBody] RequestSectionChangesRequest request)
    {
        var (isValid, errorMessage, section) = _store.RequestSectionChanges(sectionId, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation(
            "Changes requested for section {SectionId} by {UserId}",
            sectionId,
            request.RequestedBy);

        return Ok(section);
    }

    /// <summary>
    /// Creates a new draft revision from an approved section.
    /// </summary>
    [HttpPost("{sectionId}/create-revision")]
    public ActionResult<ReportSection> CreateSectionRevision(
        string sectionId,
        [FromBody] CreateSectionRevisionRequest request)
    {
        var (isValid, errorMessage, section) = _store.CreateSectionRevision(sectionId, request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        _logger.LogInformation(
            "New revision created for section {SectionId} by {UserId}",
            sectionId,
            request.CreatedBy);

        return Ok(section);
    }

    /// <summary>
    /// Gets all versions of a section for audit purposes.
    /// </summary>
    [HttpGet("{sectionId}/versions")]
    public ActionResult<IReadOnlyList<SectionVersion>> GetSectionVersions(string sectionId)
    {
        var versions = _store.GetSectionVersions(sectionId);
        return Ok(versions);
    }

    /// <summary>
    /// Checks if a section can be edited based on its current status.
    /// </summary>
    [HttpGet("{sectionId}/can-edit")]
    public ActionResult<object> CanEditSection(string sectionId)
    {
        var (canEdit, reason) = _store.CanEditSection(sectionId);
        return Ok(new { canEdit, reason });
    }
}
