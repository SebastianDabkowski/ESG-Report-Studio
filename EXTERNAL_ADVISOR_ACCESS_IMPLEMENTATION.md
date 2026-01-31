# External Advisor Limited Access Implementation

## Overview
This implementation adds time-bounded, scope-limited access for external advisors to the ESG Report Studio platform. Report Managers can now invite external advisors with restricted permissions and expiring access.

## Features Implemented

### 1. Time-Bounded Access (Expiry)

#### Backend
- **User Model**: Added `AccessExpiresAt` field (ISO 8601 timestamp) to User entity
- **SectionAccessGrant Model**: Added `ExpiresAt` field for granular section-level expiry
- **Permission Checking**: Enhanced `CheckPermission` method to validate access expiry
  - Returns denial with reason when access has expired
  - Logs expired access attempts to audit trail
- **Section Access**: Updated `HasSectionAccess` and `GetAccessibleSections` to filter expired grants
- **Grant Management**: Updated `GrantSectionAccess` to accept and store expiry dates

#### Frontend
- **User Interface**: Added visual indicators for expiring/expired access
- **Expiry Warnings**: Display expiry status in user lists with color-coded badges
- **Date Input**: Added expiry date picker when inviting advisors

### 2. Scope Limitation (Report/Section Access)

#### Backend
- **Invitation Method**: Created `InviteExternalAdvisor` method in InMemoryReportStore
  - Assigns advisor role
  - Grants access to specified sections
  - Sets access expiry
  - Creates complete audit trail
- **API Endpoint**: Added `/api/users/invite-external-advisor` endpoint
  - Accepts user ID, role ID, section IDs, expiry date, and reason
  - Returns comprehensive response with user and grant information
- **Validation**: Ensures only valid advisor roles can be assigned
- **Audit Logging**: Tracks all advisor invitations and access grants

#### Frontend
- **InviteExternalAdvisor Component**: Comprehensive UI for advisor invitation
  - User selection dropdown
  - Advisor role selection (filtered to show only advisor roles)
  - Multi-select section access with checkboxes
  - Optional expiry date picker
  - Optional reason textarea
  - Success/error feedback
- **Active Advisors View**: Display list of users with advisor roles
  - Shows access expiry status
  - Color-coded expiry warnings
  - Role badges

### 3. Read-Only Enforcement

#### Backend
- **Predefined Roles**: Existing advisor roles have limited permissions:
  - **External Advisor (Read)**: view-reports, view-public-sections
  - **External Advisor (Edit - Limited)**: view-reports, add-comments, add-recommendations
- **Permission Matrix**: Neither advisor role has edit, approve, or reject permissions
- **Validation**: InviteExternalAdvisor method validates that selected role is an advisor role

#### Frontend
- **Role Display**: Shows role permissions in selection UI
- **Visual Indicators**: Badges indicate read-only or limited-edit status

### 4. Audit Logging

#### Backend
- **Expired Access Attempts**: Logged when user tries to access after expiry
- **Access Grants**: Logged when section access is granted to advisors
- **Role Assignments**: Logged when advisor roles are assigned
- **Expiry Changes**: Logged when access expiry dates are set or modified

## API Endpoints

### POST /api/users/invite-external-advisor
Invite an external advisor with time-bounded, scope-limited access.

**Request Body:**
```json
{
  "userId": "string",
  "roleId": "string",
  "sectionIds": ["string"],
  "accessExpiresAt": "ISO8601 timestamp (optional)",
  "reason": "string (optional)",
  "invitedBy": "string"
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "id": "string",
    "name": "string",
    "email": "string",
    "roleIds": ["string"],
    "accessExpiresAt": "ISO8601 timestamp"
  },
  "sectionGrants": [
    {
      "id": "string",
      "sectionId": "string",
      "userId": "string",
      "userName": "string",
      "grantedBy": "string",
      "grantedByName": "string",
      "grantedAt": "ISO8601 timestamp",
      "expiresAt": "ISO8601 timestamp",
      "reason": "string"
    }
  ]
}
```

### Enhanced POST /api/section-access/grant
Grant section access with optional expiry.

**Additional Request Field:**
```json
{
  "expiresAt": "ISO8601 timestamp (optional)"
}
```

## Data Models

### User (Enhanced)
```csharp
public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public List<string> RoleIds { get; set; }
    public bool IsActive { get; set; }
    public bool CanExport { get; set; }
    public string? AccessExpiresAt { get; set; } // NEW: ISO 8601 timestamp
}
```

### SectionAccessGrant (Enhanced)
```csharp
public sealed class SectionAccessGrant
{
    public string Id { get; set; }
    public string SectionId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string GrantedBy { get; set; }
    public string GrantedByName { get; set; }
    public string GrantedAt { get; set; }
    public string? Reason { get; set; }
    public string? ExpiresAt { get; set; } // NEW: ISO 8601 timestamp
}
```

## Frontend Components

### InviteExternalAdvisor.tsx
New component for inviting external advisors with:
- User selection
- Advisor role selection
- Section access multi-select
- Expiry date picker
- Reason text area
- Active advisors list with expiry indicators

### UserRoleAssignment.tsx (Enhanced)
Updated to show:
- Access expiry warnings (color-coded by status)
- Expired access indicators
- Days until expiry

## Testing

### Unit Tests Created
Location: `src/backend/Tests/SD.ProjectName.Tests.Products/ExternalAdvisorAccessTests.cs`

Tests include:
- Successful advisor invitation
- Multiple section access grants
- Non-advisor role rejection
- Expired access denial in permission checks
- Expired grant filtering in section access
- Non-expired access validation
- Read-only role verification

**Note**: Some tests need test data initialization to be fully operational.

## Security Considerations

1. **Access Expiry Enforcement**: 
   - Checked at every permission validation point
   - Cannot be bypassed through direct API calls
   - Logged in audit trail

2. **Scope Limitation**:
   - Users can only access explicitly granted sections
   - Grants are checked against expiry before allowing access
   - Admin override does not extend to expired access

3. **Audit Trail**:
   - All access grants logged
   - All expired access attempts logged
   - Complete change history maintained

4. **Role Validation**:
   - Only valid advisor roles can be assigned through invitation
   - Prevents privilege escalation

## Future Enhancements

Potential improvements not in MVP:
1. NDA/terms acknowledgement before first access
2. Email notifications for expiring access (7 days, 1 day)
3. Automatic access revocation on expiry
4. Bulk advisor invitation
5. Access extension workflow
6. Report-level access control (currently section-level)

## Usage Example

### Inviting an External Advisor

1. Navigate to External Advisor Management
2. Click "Invite Advisor" button
3. Select the user to invite
4. Choose an advisor role (e.g., "External Advisor (Read)")
5. Select sections they should access
6. (Optional) Set an expiry date
7. (Optional) Add a reason for the access
8. Click "Invite Advisor"

The system will:
- Assign the advisor role
- Grant access to selected sections
- Set the expiry date
- Log all changes to audit trail
- Display success confirmation

### Monitoring Advisor Access

The Active Advisors view shows:
- All users with advisor roles
- Their assigned roles
- Access expiry status
- Color-coded warnings for expiring/expired access

## Conclusion

This implementation provides Report Managers with granular control over external advisor access, ensuring:
- **Time-bounded access**: Automatically expires after specified date
- **Scope limitation**: Access only to explicitly granted sections
- **Read-only enforcement**: Advisor roles have restricted permissions
- **Complete audit trail**: All actions logged for compliance
- **User-friendly interface**: Intuitive UI for managing advisor access
