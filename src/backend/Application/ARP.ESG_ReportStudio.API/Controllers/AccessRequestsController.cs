using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing access request workflows.
/// </summary>
/// <remarks>
/// SECURITY NOTE: In production, this controller should have:
/// - [Authorize] attribute at controller level for authenticated access
/// - Role-based authorization checks to ensure:
///   * Any authenticated user can create access requests
///   * Only admins can review (approve/reject) access requests
///   * Users can view their own requests
///   * Admins can view all requests
/// - Additional authorization checks in each method to validate user permissions
/// </remarks>
[ApiController]
[Route("api/access-requests")]
public sealed class AccessRequestsController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AccessRequestsController> _logger;

    public AccessRequestsController(
        InMemoryReportStore store,
        INotificationService notificationService,
        ILogger<AccessRequestsController> logger)
    {
        _store = store;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new access request for a section or report.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccessRequest>> CreateAccessRequest([FromBody] CreateAccessRequestRequest request)
    {
        var (isValid, errorMessage, accessRequest) = _store.CreateAccessRequest(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Get requester details
        var requester = _store.GetUsers().FirstOrDefault(u => u.Id == request.RequestedBy);
        if (requester == null)
        {
            return BadRequest(new { error = "Requester not found" });
        }

        // Get admin users to notify
        var allUsers = _store.GetUsers();
        var admins = allUsers.Where(u => u.RoleIds.Contains("admin") || u.Role == "admin").ToList();

        // Send notifications to all admins
        if (admins.Any())
        {
            try
            {
                await _notificationService.SendAccessRequestNotificationAsync(
                    accessRequest!,
                    requester,
                    admins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send access request notifications for access request {AccessRequestId}", accessRequest!.Id);
                // Continue even if notifications fail
            }
        }

        return Ok(accessRequest);
    }

    /// <summary>
    /// Gets all access requests, optionally filtered by status, requester, or resource.
    /// </summary>
    /// <remarks>
    /// SECURITY NOTE: In production, add authorization to ensure users can only view:
    /// - Access requests they created (as requester)
    /// - All access requests if user has admin role
    /// </remarks>
    [HttpGet]
    public ActionResult<List<AccessRequest>> GetAccessRequests(
        [FromQuery] string? status = null,
        [FromQuery] string? requestedBy = null,
        [FromQuery] string? resourceId = null)
    {
        var accessRequests = _store.GetAccessRequests(status, requestedBy, resourceId);
        return Ok(accessRequests);
    }

    /// <summary>
    /// Gets a specific access request by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<AccessRequest> GetAccessRequest(string id)
    {
        var accessRequest = _store.GetAccessRequest(id);
        if (accessRequest == null)
        {
            return NotFound(new { error = $"Access request with ID '{id}' not found." });
        }

        return Ok(accessRequest);
    }

    /// <summary>
    /// Approves an access request.
    /// </summary>
    /// <remarks>
    /// SECURITY NOTE: In production, add authorization to ensure only admins can approve requests.
    /// </remarks>
    [HttpPut("{id}/approve")]
    public async Task<ActionResult<AccessRequest>> ApproveAccessRequest(
        string id,
        [FromBody] ReviewAccessRequestRequest request)
    {
        // Ensure the request ID matches the route parameter
        if (request.AccessRequestId != id)
        {
            return BadRequest(new { error = "Request ID mismatch." });
        }

        // Set decision to approve
        request.Decision = "approve";

        var (isValid, errorMessage, accessRequest) = _store.ReviewAccessRequest(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Get reviewer and requester details
        var reviewer = _store.GetUsers().FirstOrDefault(u => u.Id == request.ReviewedBy);
        var requester = _store.GetUsers().FirstOrDefault(u => u.Id == accessRequest!.RequestedBy);

        if (reviewer != null && requester != null)
        {
            // Send notification to requester about the decision
            try
            {
                await _notificationService.SendAccessRequestDecisionNotificationAsync(
                    accessRequest!,
                    reviewer,
                    requester);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send access request decision notification for access request {AccessRequestId}", accessRequest!.Id);
                // Continue even if notification fails
            }
        }

        return Ok(accessRequest);
    }

    /// <summary>
    /// Rejects an access request.
    /// </summary>
    /// <remarks>
    /// SECURITY NOTE: In production, add authorization to ensure only admins can reject requests.
    /// </remarks>
    [HttpPut("{id}/reject")]
    public async Task<ActionResult<AccessRequest>> RejectAccessRequest(
        string id,
        [FromBody] ReviewAccessRequestRequest request)
    {
        // Ensure the request ID matches the route parameter
        if (request.AccessRequestId != id)
        {
            return BadRequest(new { error = "Request ID mismatch." });
        }

        // Set decision to reject
        request.Decision = "reject";

        var (isValid, errorMessage, accessRequest) = _store.ReviewAccessRequest(request);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Get reviewer and requester details
        var reviewer = _store.GetUsers().FirstOrDefault(u => u.Id == request.ReviewedBy);
        var requester = _store.GetUsers().FirstOrDefault(u => u.Id == accessRequest!.RequestedBy);

        if (reviewer != null && requester != null)
        {
            // Send notification to requester about the decision
            try
            {
                await _notificationService.SendAccessRequestDecisionNotificationAsync(
                    accessRequest!,
                    reviewer,
                    requester);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send access request decision notification for access request {AccessRequestId}", accessRequest!.Id);
                // Continue even if notification fails
            }
        }

        return Ok(accessRequest);
    }
}
