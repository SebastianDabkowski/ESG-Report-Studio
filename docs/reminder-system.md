# Reminder System for ESG Data Collection

## Overview

The reminder system automatically sends email notifications to data point owners when their assigned items are missing or incomplete and approaching a deadline.

## Features

### 1. Deadline Management
- Data points can have optional deadlines
- Deadlines are specified in ISO 8601 date format (e.g., "2026-02-15")
- Set via the `Deadline` field in `CreateDataPointRequest` or `UpdateDataPointRequest`

### 2. Reminder Configuration
Each reporting period can have its own reminder configuration:

```json
{
  "enabled": true,
  "daysBeforeDeadline": [7, 3, 1],
  "checkFrequencyHours": 24
}
```

**Configuration Options:**
- `enabled` - Enable/disable reminders for the period (default: true)
- `daysBeforeDeadline` - Array of days before deadline to send reminders (default: [7, 3, 1])
- `checkFrequencyHours` - How often to check for items needing reminders (default: 24)

### 3. Automatic Reminder Processing
- Background service runs every hour checking for items needing reminders
- Only processes active reporting periods with reminders enabled
- Sends reminders to data point owners via email
- Prevents duplicate reminders on the same day
- Automatically stops when items become complete

### 4. Reminder History
All sent reminders are tracked with:
- Data point ID
- Recipient user ID and email
- Sent timestamp
- Reminder type (missing/incomplete)
- Days until deadline
- Email success/failure status

## API Endpoints

### Get Reminder Configuration
```http
GET /api/reminders/config/{periodId}
```

**Response:**
```json
{
  "id": "abc123",
  "periodId": "period-1",
  "enabled": true,
  "daysBeforeDeadline": [7, 3, 1],
  "checkFrequencyHours": 24,
  "createdAt": "2026-01-28T00:00:00Z",
  "updatedAt": "2026-01-28T00:00:00Z"
}
```

### Update Reminder Configuration
```http
POST /api/reminders/config/{periodId}
Content-Type: application/json

{
  "enabled": true,
  "daysBeforeDeadline": [14, 7, 3, 1],
  "checkFrequencyHours": 12
}
```

### Get Reminder History
```http
GET /api/reminders/history?dataPointId={id}
GET /api/reminders/history?userId={userId}
```

**Response:**
```json
[
  {
    "id": "hist-1",
    "dataPointId": "dp-1",
    "recipientUserId": "user-1",
    "recipientEmail": "user@example.com",
    "sentAt": "2026-01-28T10:00:00Z",
    "reminderType": "incomplete",
    "daysUntilDeadline": 3,
    "deadlineDate": "2026-01-31",
    "emailSent": true,
    "errorMessage": null
  }
]
```

## Usage Example

### 1. Create a Data Point with Deadline
```http
POST /api/data-points
Content-Type: application/json

{
  "sectionId": "section-1",
  "title": "Energy Consumption Data",
  "content": "Please provide total energy consumption",
  "ownerId": "user-1",
  "source": "Energy Management System",
  "informationType": "fact",
  "completenessStatus": "incomplete",
  "deadline": "2026-02-15"
}
```

### 2. Configure Reminders for the Period
```http
POST /api/reminders/config/period-1
Content-Type: application/json

{
  "enabled": true,
  "daysBeforeDeadline": [7, 3, 1],
  "checkFrequencyHours": 24
}
```

### 3. Automatic Reminder Flow
1. Background service checks every hour
2. On February 8 (7 days before), owner receives reminder email
3. On February 12 (3 days before), owner receives another reminder
4. On February 14 (1 day before), owner receives final reminder
5. If owner marks item as complete, no more reminders are sent

## Email Content

Reminder emails include:
- Personalized greeting with owner's name
- Data point title
- Current completeness status
- Deadline date and urgency ("today", "tomorrow", or "in X days")
- Call to action to complete the data point

Example:
```
Hello Sarah Chen,

This is a reminder that the following ESG data point is incomplete and the deadline is in 3 days:

Title: Energy Consumption Data
Status: incomplete
Deadline: 2026-02-15

Please complete this data point as soon as possible to ensure timely ESG reporting.

Best regards,
ESG Report Studio
```

## MVP Implementation

For the MVP, emails are logged to the console instead of being sent via SMTP:

```csharp
// Current implementation: MockEmailService
public class MockEmailService : IEmailService
{
    public Task<bool> SendEmailAsync(string email, string name, string subject, string body)
    {
        _logger.LogInformation("Mock Email Sent: To: {Name} <{Email}>, Subject: {Subject}", name, email, subject);
        return Task.FromResult(true);
    }
}
```

To enable real email sending, replace `MockEmailService` with an implementation that uses:
- SMTP (System.Net.Mail or MailKit)
- SendGrid API
- Azure Communication Services
- Or any other email service provider

## Architecture

### Components
1. **ReminderService** - Core business logic for processing reminders
2. **ReminderBackgroundService** - Hosted service that runs periodic checks
3. **IEmailService** - Abstraction for email sending
4. **MockEmailService** - MVP implementation that logs instead of sending
5. **InMemoryReportStore** - Extended to support reminder configuration and history

### Data Flow
```
Background Service (every hour)
    ↓
ReminderService.ProcessRemindersAsync()
    ↓
For each active period:
    ↓
Get data points with deadlines
    ↓
Filter incomplete/missing items
    ↓
Check if reminder should be sent (based on days until deadline)
    ↓
Send email via IEmailService
    ↓
Record in ReminderHistory
```

## Testing

Run reminder tests:
```bash
cd src/backend
dotnet test --filter "FullyQualifiedName~ReminderTests"
```

All 11 tests validate:
- Reminder configuration CRUD operations
- Data point deadline management
- Reminder sending logic
- Filtering of completed items
- Duplicate prevention
- History tracking

## Future Enhancements

1. **Email Templates** - Customizable email templates with branding
2. **Notification Preferences** - Allow users to configure their reminder preferences
3. **Multiple Channels** - Support SMS, Slack, Teams notifications
4. **Escalation** - Send to managers if deadlines are missed
5. **Digest Mode** - Option to receive daily/weekly summary instead of individual emails
6. **Reminder Snoozing** - Allow users to postpone reminders
7. **Custom Schedules** - Per-data point or per-user reminder schedules
