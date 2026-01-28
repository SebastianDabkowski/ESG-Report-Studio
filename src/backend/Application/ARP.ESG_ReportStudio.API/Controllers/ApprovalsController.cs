using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing report approval workflows.
/// </summary>
/// <remarks>
/// SECURITY NOTE: In production, this controller should have:
/// - [Authorize] attribute at controller level for authenticated access
/// - Role-based authorization checks to ensure:
///   * Only report owners or admins can create approval requests
///   * Only assigned approvers can submit decisions for their approval records
///   * Users can only view approval requests they're involved in (as requester or approver)
/// - Additional authorization checks in each method to validate user permissions
/// </remarks>
[ApiController]
[Route("api/approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApprovalsController> _logger;

    public ApprovalsController(
        InMemoryReportStore store,
        INotificationService notificationService,
        ILogger<ApprovalsController> logger)
    {
        _store = store;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new approval request for a reporting period.
    /// </summary>
    [HttpPost("requests")]
    public async Task<ActionResult<ApprovalRequest>> CreateApprovalRequest([FromBody] CreateApprovalRequestRequest request)
    {
        var (isValid, errorMessage, approvalRequest) = _store.CreateApprovalRequest(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Get the reporting period for notification context
        var periods = _store.GetPeriods();
        var period = periods.FirstOrDefault(p => p.Id == request.PeriodId);
        if (period == null)
        {
            return BadRequest(new { error = "Reporting period not found" });
        }

        // Get requester details
        var requester = _store.GetUsers().FirstOrDefault(u => u.Id == request.RequestedBy);
        if (requester == null)
        {
            return BadRequest(new { error = "Requester not found" });
        }

        // Get approver details
        var approvers = _store.GetUsers().Where(u => request.ApproverIds.Contains(u.Id)).ToList();

        // Send notifications to all approvers
        try
        {
            await _notificationService.SendApprovalRequestNotificationAsync(
                approvalRequest!,
                period,
                requester,
                approvers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval request notifications for approval {ApprovalRequestId}", approvalRequest!.Id);
            // Continue even if notifications fail
        }

        return Ok(approvalRequest);
    }

    /// <summary>
    /// Submits an approval decision (approve or reject).
    /// </summary>
    [HttpPost("decisions")]
    public async Task<ActionResult<ApprovalRecord>> SubmitApprovalDecision([FromBody] SubmitApprovalDecisionRequest request)
    {
        var (isValid, errorMessage, approvalRecord) = _store.SubmitApprovalDecision(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Get the approval request to send notification
        var approvalRequest = _store.GetApprovalRequest(approvalRecord!.ApprovalRequestId);
        if (approvalRequest == null)
        {
            return BadRequest(new { error = "Approval request not found" });
        }

        // Get the reporting period
        var periods = _store.GetPeriods();
        var period = periods.FirstOrDefault(p => p.Id == approvalRequest.PeriodId);
        if (period == null)
        {
            return BadRequest(new { error = "Reporting period not found" });
        }

        // Get approver and requester details
        var approver = _store.GetUsers().FirstOrDefault(u => u.Id == request.DecidedBy);
        var requester = _store.GetUsers().FirstOrDefault(u => u.Id == approvalRequest.RequestedBy);

        if (approver != null && requester != null)
        {
            // Send notification to requester about the decision
            try
            {
                await _notificationService.SendApprovalDecisionNotificationAsync(
                    approvalRecord,
                    approvalRequest,
                    period,
                    approver,
                    requester);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval decision notification for approval {ApprovalRecordId}", approvalRecord.Id);
                // Continue even if notification fails
            }
        }

        return Ok(approvalRecord);
    }

    /// <summary>
    /// Gets all approval requests, optionally filtered by period or approver.
    /// </summary>
    /// <remarks>
    /// SECURITY NOTE: In production, add authorization to ensure users can only view:
    /// - Approval requests they created (as requester)
    /// - Approval requests where they are an approver
    /// - All approval requests if user has admin/auditor role
    /// </remarks>
    [HttpGet("requests")]
    public ActionResult<List<ApprovalRequest>> GetApprovalRequests(
        [FromQuery] string? periodId = null,
        [FromQuery] string? approverId = null)
    {
        var approvalRequests = _store.GetApprovalRequests(periodId, approverId);
        return Ok(approvalRequests);
    }

    /// <summary>
    /// Gets a specific approval request by ID.
    /// </summary>
    [HttpGet("requests/{id}")]
    public ActionResult<ApprovalRequest> GetApprovalRequest(string id)
    {
        var approvalRequest = _store.GetApprovalRequest(id);
        if (approvalRequest == null)
        {
            return NotFound(new { error = $"Approval request with ID '{id}' not found." });
        }

        return Ok(approvalRequest);
    }
}
