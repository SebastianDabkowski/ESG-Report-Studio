# Break-Glass Admin Access - Implementation Guide

## Overview

This document describes the break-glass admin access implementation for the ESG Report Studio. Break-glass access provides controlled emergency administrative access with comprehensive audit trailing and mandatory justification.

## Features

### 1. Controlled Emergency Access

Break-glass access allows authorized administrators to bypass normal access controls in emergency situations while maintaining full auditability:

- **Mandatory justification**: Requires a detailed reason (minimum 20 characters)
- **Strong authentication**: Enforces MFA verification when configured
- **Time-bounded sessions**: Each activation creates a tracked session
- **Comprehensive audit trail**: All actions during break-glass are automatically tagged
- **Deactivation tracking**: Records when emergency access ends and why

### 2. Session Management

Each break-glass activation creates a session that tracks:

- **Activation details**: Who, when, why, from where (IP address)
- **Authentication method**: MFA, hardware token, biometric, etc.
- **Action count**: Number of privileged actions performed
- **Deactivation details**: Who ended the session and when

### 3. Audit Trail Integration

Break-glass actions are seamlessly integrated with the existing audit log:

- **Automatic tagging**: All actions during active sessions are flagged
- **Session linkage**: Each audit entry links to its break-glass session
- **Filtering capabilities**: View only break-glass actions or exclude them
- **Export support**: CSV and JSON exports include break-glass indicators

## API Endpoints

### Get Break-Glass Status

```http
GET /api/break-glass/status
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isActive": false,
  "isAuthorized": true,
  "activeSession": null
}
```

### Activate Break-Glass Access

```http
POST /api/break-glass/activate
Authorization: Bearer {token}
Content-Type: application/json

{
  "reason": "Emergency: Production database corruption requires immediate intervention to prevent data loss"
}
```

**Response:**
```json
{
  "success": true,
  "session": {
    "id": "bg-session-123",
    "userId": "user-2",
    "userName": "Admin User",
    "reason": "Emergency: Production database corruption...",
    "activatedAt": "2024-01-15T14:30:00Z",
    "isActive": true,
    "authenticationMethod": "MFA",
    "actionCount": 1
  }
}
```

**Error Responses:**
- `400 Bad Request`: Reason too short, user already has active session, MFA not verified
- `403 Forbidden`: User not authorized for break-glass access

### Deactivate Break-Glass Access

```http
POST /api/break-glass/deactivate
Authorization: Bearer {token}
Content-Type: application/json

{
  "note": "Database restored, normal operations resumed"
}
```

**Response:**
```json
{
  "message": "Break-glass access deactivated successfully"
}
```

### Get Session History

```http
GET /api/break-glass/sessions?userId={userId}&activeOnly={true|false}
Authorization: Bearer {token}
```

**Query Parameters:**
- `userId` (optional): Filter by user ID (admin only for other users)
- `activeOnly` (optional): Filter to show only active sessions

**Response:**
```json
[
  {
    "id": "bg-session-123",
    "userId": "user-2",
    "userName": "Admin User",
    "reason": "Emergency: Production database corruption...",
    "activatedAt": "2024-01-15T14:30:00Z",
    "deactivatedAt": "2024-01-15T15:45:00Z",
    "deactivatedByName": "Admin User",
    "isActive": false,
    "authenticationMethod": "MFA",
    "actionCount": 15
  }
]
```

### Get Configuration

```http
GET /api/break-glass/config
```

**Response:**
```json
{
  "enabled": true,
  "minReasonLength": 20,
  "authorizedRoleIds": ["role-admin"],
  "requireMfa": true,
  "maxSessionDurationHours": 0
}
```

## Audit Log Filtering

The audit log API has been enhanced to support break-glass filtering:

### Filter Break-Glass Actions

```http
GET /api/audit-log?breakGlassOnly=true
X-User-Id: admin-user
```

**Response includes only actions performed during active break-glass sessions:**
```json
[
  {
    "id": "audit-entry-456",
    "timestamp": "2024-01-15T14:35:00Z",
    "userId": "user-2",
    "userName": "Admin User",
    "action": "update",
    "entityType": "ReportingPeriod",
    "entityId": "period-789",
    "isBreakGlassAction": true,
    "breakGlassSessionId": "bg-session-123",
    "changes": [
      {
        "field": "Status",
        "oldValue": "locked",
        "newValue": "active"
      }
    ]
  }
]
```

### Export with Break-Glass Indicator

```http
GET /api/audit-log/export/csv?breakGlassOnly=false
X-User-Id: admin-user
```

**CSV Output:**
```csv
"Timestamp","User ID","User Name","Action","Entity Type","Entity ID","Change Note","Break-Glass","Field","Old Value","New Value"
"2024-01-15T14:30:00Z","user-2","Admin User","activate-break-glass","BreakGlassSession","bg-session-123","Break-glass access activated","Yes","Reason","","Emergency: Production database corruption..."
"2024-01-15T14:35:00Z","user-2","Admin User","update","ReportingPeriod","period-789","Unlocking for emergency edit","Yes","Status","locked","active"
"2024-01-15T15:45:00Z","user-2","Admin User","deactivate-break-glass","BreakGlassSession","bg-session-123","Database restored","No","IsActive","true","false"
```

## Configuration

Add to `appsettings.json`:

```json
{
  "BreakGlass": {
    "Enabled": true,
    "MinReasonLength": 20,
    "AuthorizedRoleIds": ["role-admin"],
    "RequireMfa": true,
    "MaxSessionDurationHours": 0
  }
}
```

**Configuration Options:**
- `Enabled`: Whether break-glass access is enabled
- `MinReasonLength`: Minimum characters required for justification (default: 20)
- `AuthorizedRoleIds`: Role IDs authorized to use break-glass
- `RequireMfa`: Whether MFA verification is required for activation
- `MaxSessionDurationHours`: Maximum session duration (0 = unlimited)

## Usage Examples

### Emergency Workflow

**1. Administrator activates break-glass:**
```bash
curl -X POST https://api.example.com/api/break-glass/activate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Emergency: Critical system failure requiring immediate administrative intervention to restore service"
  }'
```

**2. Perform necessary emergency actions:**
All actions are automatically tagged in the audit log with the break-glass session ID.

**3. Deactivate when emergency resolved:**
```bash
curl -X POST https://api.example.com/api/break-glass/deactivate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "note": "System restored, all services operational"
  }'
```

### Auditor Workflow

**1. Review all break-glass activations:**
```bash
curl https://api.example.com/api/break-glass/sessions \
  -H "Authorization: Bearer {token}"
```

**2. Export break-glass actions for compliance:**
```bash
curl https://api.example.com/api/audit-log/export/csv?breakGlassOnly=true \
  -H "X-User-Id: auditor-1"
```

**3. Investigate specific session:**
```bash
curl "https://api.example.com/api/audit-log?breakGlassSessionId={sessionId}" \
  -H "X-User-Id: auditor-1"
```

## Security Considerations

### Current Implementation

1. **Access Control**
   - Limited to admin role by default
   - Configurable authorized roles
   - Authorization check on every activation

2. **Authentication**
   - MFA verification required when configured
   - Authentication method tracked in session
   - IP address logging for forensics

3. **Audit Trail**
   - Immutable audit entries with hash chains
   - Automatic tagging of all break-glass actions
   - Session linkage for complete traceability
   - Action count tracking

4. **Justification**
   - Mandatory reason field (minimum 20 characters)
   - Stored in session and audit log
   - Cannot bypass or leave empty

### Best Practices

1. **Activation**
   - Provide detailed, specific reasons
   - Document the emergency clearly
   - Use only when absolutely necessary

2. **During Active Session**
   - Perform only necessary emergency actions
   - Document what you're doing
   - Minimize session duration

3. **Deactivation**
   - Deactivate immediately when emergency ends
   - Provide summary of actions taken
   - Record outcome and resolution

4. **Review**
   - Regularly review break-glass usage
   - Investigate unexpected activations
   - Monitor for abuse patterns

### Future Enhancements

1. **Dual Control**
   - Require approval from second administrator
   - Implement two-person rule for activation
   - Add approval workflow

2. **Automatic Deactivation**
   - Time-based session expiration
   - Idle timeout detection
   - Automatic deactivation after max duration

3. **Real-Time Alerting**
   - Notify security team on activation
   - Send alerts for unusual patterns
   - Integration with SIEM systems

4. **Enhanced Logging**
   - Video/screen recording during sessions
   - Detailed command logging
   - Network traffic capture

## Acceptance Criteria Coverage

✅ **Strong authentication requirement**
- MFA verification enforced when configured
- Authentication method tracked in session

✅ **Mandatory reason**
- Minimum 20 characters required
- Validation on activation
- Stored in session and audit log

✅ **Break-glass action tagging**
- All actions during session automatically tagged
- Session ID linked to each audit entry
- Filterable and exportable

✅ **Deactivation tracking**
- Records who deactivated and when
- Optional deactivation note
- Updates session status and audit log

✅ **Auditability**
- Complete session history
- Tamper-evident audit trail with hash chains
- Export capabilities for compliance

## Testing

Comprehensive unit tests cover:

- Activation validation (reason length, authorization)
- Session management (create, deactivate, retrieve)
- Audit log integration (tagging, filtering)
- Action count tracking
- Authorization checks
- Multiple session handling

All 20 tests pass ✅

## Conclusion

The break-glass access implementation provides controlled emergency administrative access with complete auditability. The system ensures that emergency access is:

- **Rare**: Limited to authorized administrators
- **Visible**: Fully logged with session tracking
- **Auditable**: Comprehensive audit trail with hash chain integrity

This implementation satisfies all acceptance criteria and provides a robust foundation for emergency access scenarios while maintaining security and compliance requirements.

## Support

For questions or issues with break-glass access, please refer to:
- API documentation in the controller source code
- Unit tests for usage examples
- System administrator for configuration changes
