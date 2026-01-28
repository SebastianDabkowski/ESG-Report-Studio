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
}
