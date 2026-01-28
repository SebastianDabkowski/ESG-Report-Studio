using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for managing and sending reminders for incomplete ESG data points.
/// </summary>
public sealed class ReminderService
{
    private readonly InMemoryReportStore _store;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        InMemoryReportStore store,
        IEmailService emailService,
        ILogger<ReminderService> logger)
    {
        _store = store;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Processes reminders for all active reporting periods.
    /// </summary>
    public async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reminder processing cycle");
        
        var snapshot = _store.GetSnapshot();
        var activePeriods = snapshot.Periods.Where(p => p.Status == "active").ToList();

        foreach (var period in activePeriods)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessPeriodRemindersAsync(period.Id, cancellationToken);
        }

        _logger.LogInformation("Completed reminder processing cycle");
    }

    /// <summary>
    /// Processes reminders for a specific reporting period.
    /// </summary>
    private async Task ProcessPeriodRemindersAsync(string periodId, CancellationToken cancellationToken)
    {
        var config = _store.GetReminderConfiguration(periodId);
        if (config == null || !config.Enabled)
        {
            _logger.LogDebug("Reminders disabled for period {PeriodId}", periodId);
            return;
        }

        var today = DateTime.UtcNow;
        var dataPoints = _store.GetDataPointsForPeriod(periodId);

        foreach (var dataPoint in dataPoints)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Skip completed items
            if (dataPoint.CompletenessStatus == "complete")
                continue;

            // Skip items without deadlines
            if (string.IsNullOrEmpty(dataPoint.Deadline))
                continue;

            if (!DateTime.TryParse(dataPoint.Deadline, out var deadline))
            {
                _logger.LogWarning("Invalid deadline format for DataPoint {DataPointId}: {Deadline}", 
                    dataPoint.Id, dataPoint.Deadline);
                continue;
            }

            var daysUntilDeadline = (int)(deadline.Date - today.Date).TotalDays;

            // Check if we should send a reminder based on configuration
            if (ShouldSendReminder(dataPoint, config, daysUntilDeadline))
            {
                await SendReminderAsync(dataPoint, daysUntilDeadline);
            }
        }
    }

    /// <summary>
    /// Determines if a reminder should be sent for a data point.
    /// </summary>
    private bool ShouldSendReminder(DataPoint dataPoint, ReminderConfiguration config, int daysUntilDeadline)
    {
        // Don't send reminders for past deadlines
        if (daysUntilDeadline < 0)
            return false;

        // Check if today matches one of the configured reminder days
        if (!config.DaysBeforeDeadline.Contains(daysUntilDeadline))
            return false;

        // Check if we already sent a reminder for this exact scenario today
        var alreadySentToday = _store.HasReminderBeenSentToday(dataPoint.Id, daysUntilDeadline);
        return !alreadySentToday;
    }

    /// <summary>
    /// Sends a reminder email for a data point.
    /// </summary>
    private async Task SendReminderAsync(DataPoint dataPoint, int daysUntilDeadline)
    {
        var owner = _store.GetUser(dataPoint.OwnerId);
        if (owner == null)
        {
            _logger.LogWarning("Owner not found for DataPoint {DataPointId}", dataPoint.Id);
            return;
        }

        var subject = $"ESG Data Collection Reminder: {dataPoint.Title}";
        var body = BuildReminderEmailBody(dataPoint, owner.Name, daysUntilDeadline);

        var emailSent = await _emailService.SendEmailAsync(owner.Email, owner.Name, subject, body);

        // Record the reminder in history
        _store.RecordReminderSent(new ReminderHistory
        {
            Id = Guid.NewGuid().ToString(),
            DataPointId = dataPoint.Id,
            RecipientUserId = owner.Id,
            RecipientEmail = owner.Email,
            SentAt = DateTime.UtcNow.ToString("O"),
            ReminderType = dataPoint.CompletenessStatus,
            DaysUntilDeadline = daysUntilDeadline,
            DeadlineDate = dataPoint.Deadline,
            EmailSent = emailSent,
            ErrorMessage = emailSent ? null : "Email sending failed"
        });

        _logger.LogInformation(
            "Sent reminder for DataPoint {DataPointId} to {UserEmail} ({DaysUntilDeadline} days until deadline)",
            dataPoint.Id, owner.Email, daysUntilDeadline);
    }

    /// <summary>
    /// Builds the email body for a reminder.
    /// </summary>
    private string BuildReminderEmailBody(DataPoint dataPoint, string ownerName, int daysUntilDeadline)
    {
        var urgency = daysUntilDeadline switch
        {
            0 => "today",
            1 => "tomorrow",
            _ => $"in {daysUntilDeadline} days"
        };

        return $@"Hello {ownerName},

This is a reminder that the following ESG data point is {dataPoint.CompletenessStatus} and the deadline is {urgency}:

Title: {dataPoint.Title}
Status: {dataPoint.CompletenessStatus}
Deadline: {dataPoint.Deadline}

Please complete this data point as soon as possible to ensure timely ESG reporting.

Best regards,
ESG Report Studio";
    }
}
