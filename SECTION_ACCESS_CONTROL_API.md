# Granular Section Access Control - API Documentation

## Overview

This implementation provides granular access control at the section level, allowing Report Managers to grant and revoke access to specific sections for individual users. This ensures users can only work on content they are responsible for.

## Features

- **Grant Access**: Assign one or more users to specific sections
- **Revoke Access**: Remove section access from users
- **Access Checking**: Verify if a user has access to a section
- **Filtered Views**: Get only sections and data points accessible to a user
- **Audit Trail**: All access changes are logged with who, what, when, and why

## API Endpoints

### 1. Grant Section Access

**Endpoint**: `POST /api/section-access/grant`

Grants access to a section for one or more users.

**Request Body**:
```json
{
  "sectionId": "section-123",
  "userIds": ["user-3", "user-4"],
  "grantedBy": "user-2",
  "reason": "Contributors need access to environmental data"
}
```

**Response** (200 OK):
```json
{
  "grantedAccess": [
    {
      "id": "grant-abc-123",
      "sectionId": "section-123",
      "userId": "user-3",
      "userName": "Contributor One",
      "grantedBy": "user-2",
      "grantedByName": "Report Owner",
      "grantedAt": "2024-01-15T10:30:00Z",
      "reason": "Contributors need access to environmental data"
    },
    {
      "id": "grant-abc-124",
      "sectionId": "section-123",
      "userId": "user-4",
      "userName": "Contributor Two",
      "grantedBy": "user-2",
      "grantedByName": "Report Owner",
      "grantedAt": "2024-01-15T10:30:00Z",
      "reason": "Contributors need access to environmental data"
    }
  ],
  "failures": []
}
```

**Error Response** (400 Bad Request):
```json
{
  "error": "Failed to grant access to any users.",
  "failures": [
    {
      "userId": "user-5",
      "reason": "User not found"
    }
  ]
}
```

### 2. Revoke Section Access

**Endpoint**: `POST /api/section-access/revoke`

Revokes access to a section from one or more users.

**Request Body**:
```json
{
  "sectionId": "section-123",
  "userIds": ["user-3"],
  "revokedBy": "user-2",
  "reason": "Access no longer needed"
}
```

**Response** (200 OK):
```json
{
  "revokedUserIds": ["user-3"],
  "failures": []
}
```

### 3. Get User's Section Access

**Endpoint**: `GET /api/section-access/user/{userId}`

Retrieves all sections a user has explicit access to (does not include sections the user owns).

**Response** (200 OK):
```json
[
  {
    "id": "grant-abc-123",
    "sectionId": "section-123",
    "userId": "user-3",
    "userName": "Contributor One",
    "grantedBy": "user-2",
    "grantedByName": "Report Owner",
    "grantedAt": "2024-01-15T10:30:00Z",
    "reason": "Contributors need access to environmental data"
  }
]
```

### 4. Get Section Access Summary

**Endpoint**: `GET /api/section-access/section/{sectionId}`

Retrieves all users who have access to a section, including the section owner and explicit grants.

**Response** (200 OK):
```json
{
  "sectionId": "section-123",
  "sectionTitle": "Environmental Disclosures",
  "owner": {
    "id": "user-2",
    "name": "Report Owner",
    "email": "owner@test.com",
    "role": "report-owner",
    "isActive": true,
    "canExport": true
  },
  "accessGrants": [
    {
      "id": "grant-abc-123",
      "sectionId": "section-123",
      "userId": "user-3",
      "userName": "Contributor One",
      "grantedBy": "user-2",
      "grantedByName": "Report Owner",
      "grantedAt": "2024-01-15T10:30:00Z",
      "reason": "Contributors need access to environmental data"
    }
  ]
}
```

### 5. Check Section Access

**Endpoint**: `GET /api/section-access/check?userId={userId}&sectionId={sectionId}`

Checks if a user has access to a specific section (returns true for admins, section owners, or users with explicit grants).

**Response** (200 OK):
```json
{
  "userId": "user-3",
  "sectionId": "section-123",
  "hasAccess": true
}
```

### 6. Get Accessible Sections

**Endpoint**: `GET /api/sections/accessible?userId={userId}&periodId={periodId}`

Retrieves sections accessible to a specific user (owned or explicitly granted). Admins see all sections.

**Response** (200 OK):
```json
[
  {
    "id": "section-123",
    "periodId": "period-456",
    "title": "Environmental Disclosures",
    "category": "environmental",
    "description": "Environmental section",
    "ownerId": "user-2",
    "status": "draft",
    "order": 1,
    "catalogCode": "ENV-001",
    "isEnabled": true
  }
]
```

### 7. Get Accessible Section Summaries

**Endpoint**: `GET /api/section-summaries/accessible?userId={userId}&periodId={periodId}`

Retrieves section summaries accessible to a specific user with additional metadata.

**Response** (200 OK):
```json
[
  {
    "id": "section-123",
    "periodId": "period-456",
    "title": "Environmental Disclosures",
    "category": "environmental",
    "description": "Environmental section",
    "ownerId": "user-2",
    "ownerName": "Report Owner",
    "status": "draft",
    "order": 1,
    "dataPointCount": 5,
    "evidenceCount": 3,
    "gapCount": 1,
    "assumptionCount": 2,
    "completenessPercentage": 75,
    "progressStatus": "in-progress"
  }
]
```

### 8. Get Accessible Data Points

**Endpoint**: `GET /api/data-points/accessible?userId={userId}&sectionId={sectionId}`

Retrieves data points accessible to a user. If `sectionId` is provided, checks access to that section first.

**Response** (200 OK):
```json
[
  {
    "id": "dp-001",
    "sectionId": "section-123",
    "title": "Energy Consumption",
    "type": "narrative",
    "content": "Total energy consumption data...",
    "ownerId": "user-3",
    "status": "draft"
  }
]
```

**Error Response** (403 Forbidden):
```json
{
  "error": "Access denied. You do not have permission to view this section.",
  "sectionId": "section-123"
}
```

## Access Control Rules

### Who Has Access to a Section?

1. **Admins**: Have access to all sections automatically
2. **Section Owners**: Have automatic access to sections they own
3. **Explicitly Granted Users**: Users who have been granted access via the grant endpoint

### Permission Inheritance

- Section ownership provides automatic access
- Explicit grants are stored independently
- Revoking an explicit grant does NOT affect ownership

## Audit Trail

All access grant and revoke operations are logged to the audit trail with:

- **Action**: `grant-section-access` or `revoke-section-access`
- **Entity Type**: `SectionAccessGrant`
- **User ID**: Who performed the operation
- **Changes**: What changed (section ID, user ID, reason)
- **Timestamp**: When it occurred
- **Change Note**: Human-readable description

**Example Audit Entry**:
```json
{
  "id": "audit-789",
  "timestamp": "2024-01-15T10:30:00Z",
  "userId": "user-2",
  "userName": "Report Owner",
  "action": "grant-section-access",
  "entityType": "SectionAccessGrant",
  "entityId": "grant-abc-123",
  "changeNote": "Granted section access to user 'Contributor One' for section 'Environmental Disclosures' - Reason: Contributors need access to environmental data",
  "changes": [
    {
      "field": "SectionId",
      "oldValue": "",
      "newValue": "section-123"
    },
    {
      "field": "UserId",
      "oldValue": "",
      "newValue": "user-3"
    }
  ]
}
```

## Usage Examples

### Example 1: Grant Access to Multiple Contributors

```bash
curl -X POST http://localhost:5011/api/section-access/grant \
  -H "Content-Type: application/json" \
  -d '{
    "sectionId": "section-env-001",
    "userIds": ["contributor-1", "contributor-2", "contributor-3"],
    "grantedBy": "manager-1",
    "reason": "Team needs access to complete environmental data collection"
  }'
```

### Example 2: Check User Access Before Loading Section

```bash
curl "http://localhost:5011/api/section-access/check?userId=contributor-1&sectionId=section-env-001"
```

### Example 3: Get All Sections Accessible to a User

```bash
curl "http://localhost:5011/api/sections/accessible?userId=contributor-1&periodId=period-2024"
```

### Example 4: Revoke Access When User Changes Role

```bash
curl -X POST http://localhost:5011/api/section-access/revoke \
  -H "Content-Type: application/json" \
  -d '{
    "sectionId": "section-env-001",
    "userIds": ["contributor-1"],
    "revokedBy": "manager-1",
    "reason": "User transferred to different department"
  }'
```

## Best Practices

1. **Always Provide a Reason**: Include a meaningful reason when granting or revoking access for audit purposes
2. **Check Access Before Operations**: Use the check endpoint before performing operations on behalf of a user
3. **Use Filtered Endpoints**: When displaying UI to users, always use the accessible endpoints to ensure they only see what they have permission to view
4. **Regular Access Reviews**: Periodically review section access grants using the summary endpoint
5. **Immediate Effect**: Access changes apply immediately - no cache invalidation needed

## Security Considerations

- Section owners cannot have their implicit access revoked (they must transfer ownership first)
- Admins always have access to all sections (cannot be restricted)
- All access changes are audited and cannot be deleted
- Attempting to access a section without permission returns 403 Forbidden
- Invalid section or user IDs return clear error messages

## Implementation Details

- **Storage**: Access grants stored in `_sectionAccessGrants` list in `InMemoryReportStore`
- **Thread Safety**: All operations use locking to ensure thread safety
- **Validation**: Validates section and user existence before granting/revoking
- **Idempotency**: Attempting to grant already-granted access returns a failure for that user
- **Batch Operations**: Supports granting/revoking access for multiple users in one call
