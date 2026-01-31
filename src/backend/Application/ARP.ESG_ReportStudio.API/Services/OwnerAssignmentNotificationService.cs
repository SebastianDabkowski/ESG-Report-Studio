using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for sending notifications about ownership changes.
/// </summary>
public sealed class OwnerAssignmentNotificationService : INotificationService
{
    private readonly InMemoryReportStore _store;
    private readonly IEmailService _emailService;
    private readonly ILogger<OwnerAssignmentNotificationService> _logger;

    public OwnerAssignmentNotificationService(
        InMemoryReportStore store,
        IEmailService emailService,
        ILogger<OwnerAssignmentNotificationService> logger)
    {
        _store = store;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendSectionAssignedNotificationAsync(
        ReportSection section, 
        User newOwner, 
        User changedBy, 
        string? changeNote = null)
    {
        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = newOwner.Id,
            NotificationType = "section-assigned",
            EntityId = section.Id,
            EntityType = "ReportSection",
            EntityTitle = section.Title,
            Message = $"You have been assigned as owner of section '{section.Title}'",
            ChangedBy = changedBy.Id,
            ChangedByName = changedBy.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        // Send email notification
        var subject = $"ESG Report Studio: You've been assigned to {section.Title}";
        var body = BuildSectionAssignedEmailBody(section, newOwner, changedBy, changeNote);
        var emailSent = await _emailService.SendEmailAsync(newOwner.Email, newOwner.Name, subject, body);
        
        notification.EmailSent = emailSent;
        
        // Store notification
        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent section assignment notification for section {SectionId} to user {UserId} (email sent: {EmailSent})",
            section.Id, newOwner.Id, emailSent);
    }

    public async Task SendSectionRemovedNotificationAsync(
        ReportSection section, 
        User previousOwner, 
        User changedBy, 
        string? changeNote = null)
    {
        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = previousOwner.Id,
            NotificationType = "section-removed",
            EntityId = section.Id,
            EntityType = "ReportSection",
            EntityTitle = section.Title,
            Message = $"You have been removed as owner of section '{section.Title}'",
            ChangedBy = changedBy.Id,
            ChangedByName = changedBy.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        // Send email notification
        var subject = $"ESG Report Studio: Ownership change for {section.Title}";
        var body = BuildSectionRemovedEmailBody(section, previousOwner, changedBy, changeNote);
        var emailSent = await _emailService.SendEmailAsync(previousOwner.Email, previousOwner.Name, subject, body);
        
        notification.EmailSent = emailSent;
        
        // Store notification
        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent section removal notification for section {SectionId} to user {UserId} (email sent: {EmailSent})",
            section.Id, previousOwner.Id, emailSent);
    }

    public async Task SendDataPointAssignedNotificationAsync(
        DataPoint dataPoint, 
        User newOwner, 
        User changedBy)
    {
        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = newOwner.Id,
            NotificationType = "datapoint-assigned",
            EntityId = dataPoint.Id,
            EntityType = "DataPoint",
            EntityTitle = dataPoint.Title,
            Message = $"You have been assigned as owner of data point '{dataPoint.Title}'",
            ChangedBy = changedBy.Id,
            ChangedByName = changedBy.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        // Send email notification
        var subject = $"ESG Report Studio: You've been assigned to data point";
        var body = BuildDataPointAssignedEmailBody(dataPoint, newOwner, changedBy);
        var emailSent = await _emailService.SendEmailAsync(newOwner.Email, newOwner.Name, subject, body);
        
        notification.EmailSent = emailSent;
        
        // Store notification
        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent data point assignment notification for data point {DataPointId} to user {UserId} (email sent: {EmailSent})",
            dataPoint.Id, newOwner.Id, emailSent);
    }

    public async Task SendDataPointRemovedNotificationAsync(
        DataPoint dataPoint, 
        User previousOwner, 
        User changedBy)
    {
        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = previousOwner.Id,
            NotificationType = "datapoint-removed",
            EntityId = dataPoint.Id,
            EntityType = "DataPoint",
            EntityTitle = dataPoint.Title,
            Message = $"You have been removed as owner of data point '{dataPoint.Title}'",
            ChangedBy = changedBy.Id,
            ChangedByName = changedBy.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        // Send email notification
        var subject = $"ESG Report Studio: Ownership change for data point";
        var body = BuildDataPointRemovedEmailBody(dataPoint, previousOwner, changedBy);
        var emailSent = await _emailService.SendEmailAsync(previousOwner.Email, previousOwner.Name, subject, body);
        
        notification.EmailSent = emailSent;
        
        // Store notification
        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent data point removal notification for data point {DataPointId} to user {UserId} (email sent: {EmailSent})",
            dataPoint.Id, previousOwner.Id, emailSent);
    }

    private string BuildSectionAssignedEmailBody(
        ReportSection section, 
        User newOwner, 
        User changedBy, 
        string? changeNote)
    {
        var noteSection = !string.IsNullOrWhiteSpace(changeNote)
            ? $"\n\nNote from {changedBy.Name}: {changeNote}"
            : "";

        return $@"Hello {newOwner.Name},

You have been assigned as the owner of the following ESG report section:

Section: {section.Title}
Category: {section.Category}
Assigned by: {changedBy.Name}{noteSection}

As the section owner, you are responsible for:
- Ensuring all data points in this section are completed
- Reviewing and approving data submissions
- Managing section completeness and quality

Please log in to ESG Report Studio to review your new responsibilities.

Best regards,
ESG Report Studio";
    }

    private string BuildSectionRemovedEmailBody(
        ReportSection section, 
        User previousOwner, 
        User changedBy, 
        string? changeNote)
    {
        var noteSection = !string.IsNullOrWhiteSpace(changeNote)
            ? $"\n\nNote from {changedBy.Name}: {changeNote}"
            : "";

        return $@"Hello {previousOwner.Name},

You have been removed as the owner of the following ESG report section:

Section: {section.Title}
Category: {section.Category}
Changed by: {changedBy.Name}{noteSection}

This section has been reassigned to another user. You are no longer responsible for this section.

Best regards,
ESG Report Studio";
    }

    private string BuildDataPointAssignedEmailBody(
        DataPoint dataPoint, 
        User newOwner, 
        User changedBy)
    {
        var deadlineInfo = !string.IsNullOrWhiteSpace(dataPoint.Deadline)
            ? $"\nDeadline: {dataPoint.Deadline}"
            : "";

        return $@"Hello {newOwner.Name},

You have been assigned as the owner of the following ESG data point:

Title: {dataPoint.Title}
Status: {dataPoint.CompletenessStatus}
Assigned by: {changedBy.Name}{deadlineInfo}

Please log in to ESG Report Studio to review and complete this data point.

Best regards,
ESG Report Studio";
    }

    private string BuildDataPointRemovedEmailBody(
        DataPoint dataPoint, 
        User previousOwner, 
        User changedBy)
    {
        return $@"Hello {previousOwner.Name},

You have been removed as the owner of the following ESG data point:

Title: {dataPoint.Title}
Changed by: {changedBy.Name}

This data point has been reassigned to another user. You are no longer responsible for this data point.

Best regards,
ESG Report Studio";
    }

    public async Task SendApprovalRequestNotificationAsync(
        ApprovalRequest approvalRequest,
        ReportingPeriod period,
        User requester,
        List<User> approvers)
    {
        var deadlineInfo = !string.IsNullOrWhiteSpace(approvalRequest.ApprovalDeadline)
            ? $"\nDeadline: {approvalRequest.ApprovalDeadline}"
            : "";

        var messageInfo = !string.IsNullOrWhiteSpace(approvalRequest.RequestMessage)
            ? $"\n\nMessage from {requester.Name}:\n{approvalRequest.RequestMessage}"
            : "";

        foreach (var approver in approvers)
        {
            var notification = new OwnerNotification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientUserId = approver.Id,
                NotificationType = "approval-requested",
                EntityId = approvalRequest.Id,
                EntityType = "ApprovalRequest",
                EntityTitle = $"Approval for {period.Name}",
                Message = $"Approval requested for report '{period.Name}' by {requester.Name}",
                ChangedBy = requester.Id,
                ChangedByName = requester.Name,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                IsRead = false
            };

            var subject = $"ESG Report Studio: Approval Requested - {period.Name}";
            var body = $@"Hello {approver.Name},

{requester.Name} has requested your approval for the following ESG report:

Report Period: {period.Name}
Reporting Period: {period.StartDate} to {period.EndDate}
Requested by: {requester.Name}{deadlineInfo}{messageInfo}

Please log in to ESG Report Studio to review the report and provide your approval decision.

Best regards,
ESG Report Studio";

            var emailSent = await _emailService.SendEmailAsync(approver.Email, approver.Name, subject, body);
            notification.EmailSent = emailSent;

            _store.RecordNotification(notification);

            _logger.LogInformation(
                "Sent approval request notification for approval {ApprovalRequestId} to approver {UserId} (email sent: {EmailSent})",
                approvalRequest.Id, approver.Id, emailSent);
        }
    }

    public async Task SendApprovalDecisionNotificationAsync(
        ApprovalRecord approvalRecord,
        ApprovalRequest approvalRequest,
        ReportingPeriod period,
        User approver,
        User requester)
    {
        var decision = approvalRecord.Decision == "approve" ? "approved" : "rejected";
        var commentInfo = !string.IsNullOrWhiteSpace(approvalRecord.Comment)
            ? $"\n\nComment from {approver.Name}:\n{approvalRecord.Comment}"
            : "";

        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = requester.Id,
            NotificationType = $"approval-{decision}",
            EntityId = approvalRequest.Id,
            EntityType = "ApprovalRequest",
            EntityTitle = $"Approval for {period.Name}",
            Message = $"{approver.Name} has {decision} the report '{period.Name}'",
            ChangedBy = approver.Id,
            ChangedByName = approver.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        // Check overall approval status
        var allApprovals = approvalRequest.Approvals;
        var totalApprovals = allApprovals.Count;
        var completedApprovals = allApprovals.Count(a => a.Status != "pending");
        var approvedCount = allApprovals.Count(a => a.Status == "approved");
        var rejectedCount = allApprovals.Count(a => a.Status == "rejected");

        var statusInfo = completedApprovals == totalApprovals
            ? $"\n\nOverall Status: All {totalApprovals} approver(s) have responded ({approvedCount} approved, {rejectedCount} rejected)"
            : $"\n\nOverall Status: {completedApprovals} of {totalApprovals} approver(s) have responded";

        var subject = $"ESG Report Studio: Approval {decision.ToUpper()} - {period.Name}";
        var body = $@"Hello {requester.Name},

{approver.Name} has {decision} your approval request for:

Report Period: {period.Name}
Reporting Period: {period.StartDate} to {period.EndDate}
Decision: {decision.ToUpper()}
Decided at: {approvalRecord.DecidedAt}{commentInfo}{statusInfo}

Please log in to ESG Report Studio to view the complete approval status.

Best regards,
ESG Report Studio";

        var emailSent = await _emailService.SendEmailAsync(requester.Email, requester.Name, subject, body);
        notification.EmailSent = emailSent;

        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent approval decision notification for approval {ApprovalRequestId} to requester {UserId} (email sent: {EmailSent})",
            approvalRequest.Id, requester.Id, emailSent);
    }

    public async Task SendAccessRequestNotificationAsync(
        AccessRequest accessRequest,
        User requester,
        List<User> admins)
    {
        foreach (var admin in admins)
        {
            var notification = new OwnerNotification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientUserId = admin.Id,
                NotificationType = "access-requested",
                EntityId = accessRequest.Id,
                EntityType = "AccessRequest",
                EntityTitle = $"Access request for {accessRequest.ResourceName}",
                Message = $"{requester.Name} has requested access to {accessRequest.ResourceType} '{accessRequest.ResourceName}'",
                ChangedBy = requester.Id,
                ChangedByName = requester.Name,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                IsRead = false
            };

            var subject = $"ESG Report Studio: Access Request - {accessRequest.ResourceName}";
            var body = $@"Hello {admin.Name},

{requester.Name} has requested access to the following resource:

Resource Type: {accessRequest.ResourceType}
Resource Name: {accessRequest.ResourceName}
Requested by: {requester.Name}
Requested at: {accessRequest.RequestedAt}

Reason:
{accessRequest.Reason}

Please log in to ESG Report Studio to review and approve or reject this access request.

Best regards,
ESG Report Studio";

            var emailSent = await _emailService.SendEmailAsync(admin.Email, admin.Name, subject, body);
            notification.EmailSent = emailSent;

            _store.RecordNotification(notification);

            _logger.LogInformation(
                "Sent access request notification for access request {AccessRequestId} to admin {UserId} (email sent: {EmailSent})",
                accessRequest.Id, admin.Id, emailSent);
        }
    }

    public async Task SendAccessRequestDecisionNotificationAsync(
        AccessRequest accessRequest,
        User reviewer,
        User requester)
    {
        var decision = accessRequest.Status == "approved" ? "approved" : "rejected";
        var commentInfo = !string.IsNullOrWhiteSpace(accessRequest.ReviewComment)
            ? $"\n\nComment from {reviewer.Name}:\n{accessRequest.ReviewComment}"
            : "";

        var notification = new OwnerNotification
        {
            Id = Guid.NewGuid().ToString(),
            RecipientUserId = requester.Id,
            NotificationType = $"access-{decision}",
            EntityId = accessRequest.Id,
            EntityType = "AccessRequest",
            EntityTitle = $"Access request for {accessRequest.ResourceName}",
            Message = $"Your access request for {accessRequest.ResourceType} '{accessRequest.ResourceName}' has been {decision}",
            ChangedBy = reviewer.Id,
            ChangedByName = reviewer.Name,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false
        };

        var grantedInfo = decision == "approved"
            ? "\n\nYou now have access to this resource."
            : "";

        var subject = $"ESG Report Studio: Access Request {decision.ToUpper()} - {accessRequest.ResourceName}";
        var body = $@"Hello {requester.Name},

{reviewer.Name} has {decision} your access request for:

Resource Type: {accessRequest.ResourceType}
Resource Name: {accessRequest.ResourceName}
Decision: {decision.ToUpper()}
Reviewed at: {accessRequest.ReviewedAt}{commentInfo}{grantedInfo}

Please log in to ESG Report Studio for more details.

Best regards,
ESG Report Studio";

        var emailSent = await _emailService.SendEmailAsync(requester.Email, requester.Name, subject, body);
        notification.EmailSent = emailSent;

        _store.RecordNotification(notification);

        _logger.LogInformation(
            "Sent access request decision notification for access request {AccessRequestId} to requester {UserId} (email sent: {EmailSent})",
            accessRequest.Id, requester.Id, emailSent);
    }
}
