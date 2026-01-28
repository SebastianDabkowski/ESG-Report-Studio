using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for escalating overdue ESG data points to administrators.
/// </summary>
public sealed class EscalationService : IEscalationService
{
    private readonly InMemoryReportStore _store;
    private readonly IEmailService _emailService;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(
        InMemoryReportStore store,
        IEmailService emailService,
        ILogger<EscalationService> logger)
    {
        _store = store;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Processes escalations for all active reporting periods.
    /// </summary>
    public async Task ProcessEscalationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting escalation processing cycle");

        var snapshot = _store.GetSnapshot();
        var activePeriods = snapshot.Periods.Where(p => p.Status == "active").ToList();

        foreach (var period in activePeriods)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessPeriodEscalationsAsync(period, cancellationToken);
        }

        _logger.LogInformation("Completed escalation processing cycle");
    }

    /// <summary>
    /// Processes escalations for a specific reporting period.
    /// </summary>
    private async Task ProcessPeriodEscalationsAsync(ReportingPeriod period, CancellationToken cancellationToken)
    {
        var config = _store.GetEscalationConfiguration(period.Id);
        if (config == null || !config.Enabled)
        {
            _logger.LogDebug("Escalations disabled for period {PeriodId}", period.Id);
            return;
        }

        var today = DateTime.UtcNow;
        var dataPoints = _store.GetDataPointsForPeriod(period.Id);

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

            // Calculate days overdue (negative means not overdue yet)
            var daysOverdue = (int)(today.Date - deadline.Date).TotalDays;

            // Only process if overdue
            if (daysOverdue <= 0)
                continue;

            // Check if we should escalate based on configuration
            if (ShouldEscalate(dataPoint, config, daysOverdue))
            {
                await EscalateAsync(dataPoint, period, daysOverdue);
            }
        }
    }

    /// <summary>
    /// Determines if an item should be escalated.
    /// </summary>
    private bool ShouldEscalate(DataPoint dataPoint, EscalationConfiguration config, int daysOverdue)
    {
        // Check if today matches one of the configured escalation days
        if (!config.DaysAfterDeadline.Contains(daysOverdue))
            return false;

        // Check if we already escalated for this exact scenario today
        var alreadyEscalatedToday = _store.HasEscalationBeenSentToday(dataPoint.Id, daysOverdue);
        return !alreadyEscalatedToday;
    }

    /// <summary>
    /// Escalates an overdue data point to owner and administrator.
    /// </summary>
    private async Task EscalateAsync(DataPoint dataPoint, ReportingPeriod period, int daysOverdue)
    {
        var owner = _store.GetUser(dataPoint.OwnerId);
        if (owner == null)
        {
            _logger.LogWarning("Owner not found for DataPoint {DataPointId}", dataPoint.Id);
            return;
        }

        // Get period owner or admin as escalation recipient
        var admin = _store.GetUser(period.OwnerId);
        if (admin == null)
        {
            _logger.LogWarning("Administrator not found for period {PeriodId}", period.Id);
            return;
        }

        var isOwnerAndAdmin = admin.Id == owner.Id;

        // Send notification to owner
        var ownerSubject = $"OVERDUE: ESG Data Point - {dataPoint.Title}";
        var ownerBody = BuildOwnerEscalationEmailBody(dataPoint, owner.Name, daysOverdue, isOwnerAndAdmin);
        var ownerEmailSent = await _emailService.SendEmailAsync(owner.Email, owner.Name, ownerSubject, ownerBody);

        // Send escalation notification to admin (only if different from owner)
        var adminEmailSent = false;
        if (!isOwnerAndAdmin)
        {
            var adminSubject = $"ESCALATION: Overdue ESG Data Point - {dataPoint.Title}";
            var adminBody = BuildAdminEscalationEmailBody(dataPoint, admin.Name, owner.Name, daysOverdue);
            adminEmailSent = await _emailService.SendEmailAsync(admin.Email, admin.Name, adminSubject, adminBody);
        }

        // Determine if there was an error
        var hasError = !ownerEmailSent || (!isOwnerAndAdmin && !adminEmailSent);

        // Record the escalation in history
        _store.RecordEscalationSent(new EscalationHistory
        {
            Id = Guid.NewGuid().ToString(),
            DataPointId = dataPoint.Id,
            OwnerUserId = owner.Id,
            OwnerEmail = owner.Email,
            EscalatedToUserId = isOwnerAndAdmin ? null : admin.Id,
            EscalatedToEmail = isOwnerAndAdmin ? null : admin.Email,
            SentAt = DateTime.UtcNow.ToString("O"),
            DaysOverdue = daysOverdue,
            DeadlineDate = dataPoint.Deadline ?? string.Empty,
            OwnerEmailSent = ownerEmailSent,
            AdminEmailSent = adminEmailSent,
            ErrorMessage = hasError ? "One or more emails failed to send" : null
        });

        _logger.LogInformation(
            "Escalated overdue DataPoint {DataPointId} to owner {OwnerEmail} and admin {AdminEmail} ({DaysOverdue} days overdue)",
            dataPoint.Id, owner.Email, admin.Email, daysOverdue);
    }

    /// <summary>
    /// Builds the email body for owner notification.
    /// </summary>
    private string BuildOwnerEscalationEmailBody(DataPoint dataPoint, string ownerName, int daysOverdue, bool isOwnerAndAdmin)
    {
        var escalationNote = isOwnerAndAdmin
            ? "As the report administrator, please address this overdue item immediately."
            : "This item has been escalated to the report administrator.";

        return $@"Hello {ownerName},

URGENT: The following ESG data point is now {daysOverdue} day(s) OVERDUE and requires immediate attention:

Title: {dataPoint.Title}
Status: {dataPoint.CompletenessStatus}
Deadline: {dataPoint.Deadline}
Days Overdue: {daysOverdue}

{escalationNote} Please complete this data point immediately to ensure timely ESG reporting.

Best regards,
ESG Report Studio";
    }

    /// <summary>
    /// Builds the email body for administrator escalation.
    /// </summary>
    private string BuildAdminEscalationEmailBody(DataPoint dataPoint, string adminName, string ownerName, int daysOverdue)
    {
        return $@"Hello {adminName},

This is an escalation notice: The following ESG data point is {daysOverdue} day(s) OVERDUE:

Title: {dataPoint.Title}
Status: {dataPoint.CompletenessStatus}
Assigned Owner: {ownerName}
Deadline: {dataPoint.Deadline}
Days Overdue: {daysOverdue}

The owner has been notified. As the report administrator, you may need to follow up to ensure completion and maintain reporting deadlines.

Best regards,
ESG Report Studio";
    }
}
