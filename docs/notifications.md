# Owner Assignment Notifications

## Overview

The ESG Report Studio now includes a comprehensive notification system that alerts users when they are assigned as owners of sections or data points, or when their ownership changes.

## Features

### Notification Types

1. **section-assigned**: User is assigned as the owner of a section
2. **section-removed**: User is removed as the owner of a section
3. **datapoint-assigned**: User is assigned as the owner of a data point
4. **datapoint-removed**: User is removed as the owner of a data point

### When Notifications Are Sent

**Section Ownership:**
- When a section owner is assigned via `PUT /api/sections/{id}/owner`
- When section owners are bulk-assigned via `POST /api/sections/bulk-owner`
- Both the new owner (assignment notification) and previous owner (removal notification) receive notifications
- No notifications are sent when re-assigning to the same owner

**Data Point Ownership:**
- When a data point owner is changed via `PUT /api/data-points/{id}`
- Both the new owner (assignment notification) and previous owner (removal notification) receive notifications
- No notifications when creating a data point with an initial owner
- No notifications when re-assigning to the same owner

### Notification Channels

1. **Email**: Sent via `IEmailService` (currently `MockEmailService` in development, logs to console)
2. **In-App**: Stored in the system and retrievable via API

## API Endpoints

### Get User Notifications

```http
GET /api/notifications?userId={userId}&unreadOnly={boolean}
```

**Parameters:**
- `userId` (required): The user ID to get notifications for
- `unreadOnly` (optional, default: false): If true, only returns unread notifications

**Response:**
```json
[
  {
    "id": "notif-123",
    "recipientUserId": "user-1",
    "notificationType": "section-assigned",
    "entityId": "section-456",
    "entityType": "ReportSection",
    "entityTitle": "Energy & Emissions",
    "message": "You have been assigned as owner of section 'Energy & Emissions'",
    "changedBy": "user-2",
    "changedByName": "Admin User",
    "createdAt": "2024-01-15T10:30:00Z",
    "isRead": false,
    "emailSent": true
  }
]
```

### Mark Notification as Read

```http
PUT /api/notifications/{id}/read
```

**Response:**
```json
{
  "message": "Notification marked as read."
}
```

## Email Content

### Section Assignment Email

```
Subject: ESG Report Studio: You've been assigned to {Section Title}

Hello {Owner Name},

You have been assigned as the owner of the following ESG report section:

Section: {Section Title}
Category: {Category}
Assigned by: {Changed By Name}

Note from {Changed By Name}: {Change Note}

As the section owner, you are responsible for:
- Ensuring all data points in this section are completed
- Reviewing and approving data submissions
- Managing section completeness and quality

Please log in to ESG Report Studio to review your new responsibilities.

Best regards,
ESG Report Studio
```

### Section Removal Email

```
Subject: ESG Report Studio: Ownership change for {Section Title}

Hello {Owner Name},

You have been removed as the owner of the following ESG report section:

Section: {Section Title}
Category: {Category}
Changed by: {Changed By Name}

Note from {Changed By Name}: {Change Note}

This section has been reassigned to another user. You are no longer responsible for this section.

Best regards,
ESG Report Studio
```

### Data Point Assignment/Removal Emails

Similar structure to section emails, but focused on data points with additional details like deadline (if set) and completeness status.

## Implementation Details

### Backend Services

- **INotificationService**: Interface for notification operations
- **OwnerAssignmentNotificationService**: Implementation that sends emails and records notifications
- **InMemoryReportStore**: Stores notification history and provides retrieval methods

### Notification Storage

Notifications are stored in-memory with the following properties:
- Unique ID
- Recipient user ID
- Notification type
- Entity ID and type
- Message content
- Who made the change
- Timestamp
- Read status
- Email delivery status

### Performance Optimizations

- **Bulk operations**: Notifications are sent concurrently using `Task.WhenAll`
- **Single operations**: Notifications are sent asynchronously after the database update

## Security Considerations

**⚠️ IMPORTANT FOR PRODUCTION:**

The current MVP implementation does NOT include authorization checks on notification endpoints. Before deploying to production:

1. **GET /api/notifications**: Add authentication and verify the requesting user is either:
   - The owner of the notifications (userId matches authenticated user)
   - An administrator

2. **PUT /api/notifications/{id}/read**: Add authentication and verify:
   - The notification belongs to the authenticated user
   - Or the user is an administrator

See code comments in `NotificationsController.cs` for implementation guidance.

## Testing

The notification system includes comprehensive unit tests:

1. **SendSectionAssignedNotification_ShouldCreateNotificationAndSendEmail**: Verifies section assignment notifications
2. **SendSectionRemovedNotification_ShouldCreateNotificationAndSendEmail**: Verifies section removal notifications
3. **SendDataPointAssignedNotification_ShouldCreateNotificationAndSendEmail**: Verifies data point assignment notifications
4. **GetNotifications_ShouldReturnOnlyUnreadWhenFlagSet**: Tests filtering unread notifications
5. **MarkNotificationAsRead_ShouldUpdateIsReadFlag**: Tests marking notifications as read
6. **MarkNotificationAsRead_ShouldReturnFalseForNonexistentNotification**: Tests error handling

All tests use mocked `IEmailService` to verify email sending without actual delivery.

## Future Enhancements

1. **Push Notifications**: Add support for browser push notifications
2. **Notification Preferences**: Allow users to configure which notifications they want to receive
3. **Digest Mode**: Option to receive daily/weekly notification digests instead of immediate emails
4. **Real Email Service**: Replace MockEmailService with actual email provider (SMTP, SendGrid, etc.)
5. **Notification Templates**: Make email templates customizable per organization
6. **Notification History Export**: Allow users to export their notification history
