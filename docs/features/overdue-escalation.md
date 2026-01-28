# Overdue Items Escalation Feature

## Overview

The overdue items escalation feature automatically detects when ESG data points miss their deadlines and escalates them to both the assigned owner and the report administrator. This ensures that reporting deadlines are not missed and accountability is maintained.

## How It Works

### 1. Overdue Detection

The system runs hourly checks (via `ReminderBackgroundService`) to identify data points that:
- Have a deadline date set
- Are past their deadline (deadline < today)
- Are not completed (completeness status != "complete")

### 2. Escalation Policy

Each reporting period can have a configurable escalation policy that defines:
- **Enabled**: Whether escalations are active for this period
- **DaysAfterDeadline**: Specific days after the deadline to send escalations (e.g., [3, 7] means escalate at 3 and 7 days overdue)

Default configuration: Escalate at 3 and 7 days after deadline.

### 3. Notification Process

When an item is overdue by a configured number of days:
1. **Owner Notification**: The data point owner receives an email marked as "OVERDUE"
2. **Admin Notification**: The report administrator (period owner) receives an escalation email
3. **History Tracking**: The escalation is recorded to prevent duplicate notifications on the same day

### 4. Email Content

**Owner Email:**
- Subject: "OVERDUE: ESG Data Point - [Title]"
- Contains: Data point title, status, deadline, days overdue
- Indicates that the item has been escalated to the administrator

**Administrator Email:**
- Subject: "ESCALATION: Overdue ESG Data Point - [Title]"
- Contains: Data point title, owner name, status, deadline, days overdue
- Notifies that the owner has also been contacted

## API Endpoints

### Get Escalation Configuration

```http
GET /api/escalations/config/{periodId}
```

Returns the escalation configuration for a reporting period. If none exists, returns default configuration.

**Response:**
```json
{
  "id": "string",
  "periodId": "string",
  "enabled": true,
  "daysAfterDeadline": [3, 7],
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### Update Escalation Configuration

```http
POST /api/escalations/config/{periodId}
```

**Request Body:**
```json
{
  "enabled": true,
  "daysAfterDeadline": [3, 7, 14]
}
```

**Validation Rules:**
- `daysAfterDeadline` must contain at least one value
- All values must be positive (> 0)

**Response:**
```json
{
  "id": "string",
  "periodId": "string",
  "enabled": true,
  "daysAfterDeadline": [3, 7, 14],
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### Get Escalation History

```http
GET /api/escalations/history?dataPointId={id}&userId={userId}
```

**Query Parameters:**
- `dataPointId` (optional): Filter by specific data point
- `userId` (optional): Filter by owner or escalated-to user

**Response:**
```json
[
  {
    "id": "string",
    "dataPointId": "string",
    "ownerUserId": "string",
    "ownerEmail": "string",
    "escalatedToUserId": "string",
    "escalatedToEmail": "string",
    "sentAt": "2024-01-01T00:00:00Z",
    "daysOverdue": 3,
    "deadlineDate": "2023-12-29",
    "ownerEmailSent": true,
    "adminEmailSent": true,
    "errorMessage": null
  }
]
```

## Database Models

### EscalationConfiguration

- `Id`: Unique identifier
- `PeriodId`: Associated reporting period
- `Enabled`: Whether escalations are active
- `DaysAfterDeadline`: List of days to escalate
- `CreatedAt`: Creation timestamp
- `UpdatedAt`: Last update timestamp

### EscalationHistory

- `Id`: Unique identifier
- `DataPointId`: Escalated data point
- `OwnerUserId`: Data point owner
- `OwnerEmail`: Owner email address
- `EscalatedToUserId`: Administrator (if different from owner)
- `EscalatedToEmail`: Administrator email address
- `SentAt`: Escalation timestamp
- `DaysOverdue`: Number of days past deadline
- `DeadlineDate`: The missed deadline
- `OwnerEmailSent`: Email delivery status (owner)
- `AdminEmailSent`: Email delivery status (admin)
- `ErrorMessage`: Error details if email failed

## Configuration Examples

### Default Configuration (Recommended)
```json
{
  "enabled": true,
  "daysAfterDeadline": [3, 7]
}
```
Escalates at 3 days and 7 days after the deadline.

### Aggressive Escalation
```json
{
  "enabled": true,
  "daysAfterDeadline": [1, 3, 5, 7, 10]
}
```
More frequent escalations for critical reporting periods.

### Single Escalation
```json
{
  "enabled": true,
  "daysAfterDeadline": [5]
}
```
One escalation at 5 days overdue.

### Disabled Escalation
```json
{
  "enabled": false,
  "daysAfterDeadline": [3, 7]
}
```
Temporarily disable escalations for a period.

## Implementation Details

### Services

- **IEscalationService**: Interface for escalation processing
- **EscalationService**: Core service implementing overdue detection and notification logic
- **ReminderBackgroundService**: Background service that runs hourly to process both reminders and escalations

### Controllers

- **EscalationsController**: API endpoints for managing escalation configuration and viewing history

### Data Store

- **InMemoryReportStore**: Extended with methods for escalation configuration and history management
  - `GetEscalationConfiguration(periodId)`
  - `CreateOrUpdateEscalationConfiguration(periodId, config)`
  - `RecordEscalationSent(history)`
  - `HasEscalationBeenSentToday(dataPointId, daysOverdue)`
  - `GetEscalationHistory(dataPointId?, userId?)`

## Testing

The feature includes comprehensive test coverage:

### EscalationServiceTests (8 tests)
- Overdue detection logic
- Escalation notification behavior
- Duplicate prevention
- Configuration-based escalation
- Owner/admin email handling

### EscalationsControllerTests (8 tests)
- Configuration CRUD operations
- Validation rules
- History retrieval and filtering

All tests pass successfully as part of the 199-test suite.

## Future Enhancements

Potential improvements for future iterations:
1. Custom email templates per organization
2. Multiple escalation recipients (CC lists)
3. SMS or other notification channels
4. Escalation severity levels
5. Auto-assignment fallback if owner doesn't respond
6. Dashboard widget for overdue items
7. Weekly summary reports of escalations
