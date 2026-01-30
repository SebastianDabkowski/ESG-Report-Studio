# Export Access Control Implementation

## Overview

This implementation adds role-based and owner-based access control to report export functionality in the ESG Report Studio. It ensures that only authorized users can export reports (PDF and DOCX formats) and maintains a complete audit trail of all export attempts.

## Features Implemented

### 1. Permission Model

**User Model Enhancement:**
- Added `CanExport` boolean property to the `User` model (default: `false`)
- Applies least privilege principle by default

**Permission Rules:**
- **Global Permission**: Users with `CanExport=true` can export any report
- **Owner-Based Permission**: Period owners can export their own reports even without global permission
- By default, these roles have `CanExport=true`:
  - `admin`
  - `report-owner`
  - `auditor`
- Contributors have `CanExport=false` by default

### 2. Backend Implementation

**Permission Check Service (`InMemoryReportStore`):**
```csharp
public (bool HasPermission, string? ErrorMessage) CheckExportPermission(string userId, string periodId)
```
- Checks global `CanExport` permission
- Falls back to owner-based permission check
- Returns clear error messages for denied access

**Export Audit Logging:**
```csharp
public void RecordExportAttempt(string userId, string userName, string periodId, 
    string format, string? variantName, bool wasAllowed, string? errorMessage = null)
```
- Records both successful and denied export attempts
- Tracks format, variant name, and permission status
- Uses existing audit log infrastructure

**Protected Export Endpoints:**
- `POST /api/periods/{periodId}/export-pdf`
- `POST /api/periods/{periodId}/export-docx`

Both endpoints now:
1. Validate export permissions before processing
2. Return `403 Forbidden` for unauthorized attempts
3. Log all attempts (successful and denied) to audit trail
4. Provide clear error messages

### 3. Frontend Implementation

**Type Definitions (`types.ts`):**
```typescript
export interface User {
  // ... existing fields
  canExport?: boolean // Permission to export reports
}
```

**Permission Helper (`helpers.ts`):**
```typescript
export function canUserExport(
  user: { canExport?: boolean; id: string }, 
  periodOwnerId?: string
): boolean
```
- Checks global export permission
- Checks owner-based permission for specific period
- Consistent with backend logic

**UI Changes (`Dashboard.tsx`):**
- Export buttons shown only to users with permission
- "Export Restricted" button displayed for users without permission
- Enhanced error handling for 403 responses with user-friendly messages
- Uses `LockKey` icon to indicate restricted access

**Export History View (`ExportHistoryView.tsx`):**
- Enhanced to display Generation ID and Export ID
- Shows complete audit trail:
  - User who performed export
  - Export timestamp
  - File format and size
  - Variant name (if applicable)
  - Download count
  - File checksum for integrity verification

### 4. Audit Trail

**Export Attempts Logged:**
All export attempts are recorded in the audit log with:
- `EntityType: "ReportExport"`
- `Action: "export"` (successful) or `"export-denied"` (blocked)
- User ID and name
- Period ID
- Format (PDF/DOCX)
- Variant name
- Permission status

**Audit Log Queries:**
```
GET /api/audit-log?entityType=ReportExport
GET /api/audit-log?entityType=ReportExport&action=export-denied
```

## Testing

### Backend Tests (`ExportAccessControlTests.cs`)

**16 comprehensive tests covering:**

1. **Permission Checks:**
   - Admin users can export ✓
   - Report owner role users can export ✓
   - Auditor role users can export ✓
   - Contributor role users blocked ✓
   - Period owners can export ✓
   - Invalid users blocked ✓

2. **PDF Export:**
   - Authorized users succeed ✓
   - Unauthorized users get 403 ✓
   - Denied attempts logged ✓
   - Successful attempts logged ✓

3. **DOCX Export:**
   - Authorized users succeed ✓
   - Unauthorized users get 403 ✓
   - Denied attempts logged ✓
   - Successful attempts logged ✓

4. **Audit Trail:**
   - Export attempts include required fields ✓
   - Filter by ReportExport entity type ✓

**All tests passing**: 16/16 ✓

## Usage Examples

### Backend

**Check Permission:**
```csharp
var (hasPermission, errorMessage) = _store.CheckExportPermission(userId, periodId);
if (!hasPermission) {
    // Handle denied access
}
```

**Record Attempt:**
```csharp
_store.RecordExportAttempt(userId, userName, periodId, "pdf", variantName, 
    wasAllowed: true);
```

### Frontend

**Check Permission in UI:**
```typescript
import { canUserExport } from '@/lib/helpers'

// In component
const canExport = canUserExport(currentUser, activePeriod.ownerId)

{canExport ? (
  <Button onClick={handleExport}>Export PDF</Button>
) : (
  <Button disabled title="You do not have permission to export reports">
    <LockKey /> Export Restricted
  </Button>
)}
```

**Handle Export Errors:**
```typescript
try {
  await exportReportPdf(periodId, { generatedBy: currentUser.id })
} catch (error) {
  const errorMessage = error instanceof Error ? error.message : 'Failed to export'
  
  if (errorMessage.includes('permission') || errorMessage.includes('403')) {
    alert('You do not have permission to export reports. Contact an administrator.')
  } else {
    alert(errorMessage)
  }
}
```

## Security Considerations

1. **Least Privilege by Default**: New users have `CanExport=false`
2. **Defense in Depth**: Permission checks at both UI and API levels
3. **Complete Audit Trail**: All attempts (successful and denied) are logged
4. **Clear Error Messages**: Users receive actionable feedback
5. **Owner-Based Permissions**: Supports delegation without global privileges

## Migration Notes

**Existing Users:**
- Admin, report-owner, and auditor roles: `CanExport=true` by default
- Contributors: `CanExport=false` by default
- Period owners can export their reports regardless of global permission

**No Breaking Changes:**
- Existing API contracts unchanged
- Frontend gracefully handles missing `canExport` field (treats as false)
- Preview functionality remains available to all users

## Acceptance Criteria Status

✅ **Given role-based permissions exist, when a user without export permission attempts to export, then the action is blocked and logged.**
- Users without permission receive 403 response
- All attempts logged to audit trail with user, timestamp, and reason

✅ **Given a user can preview but not export, when they open the export screen, then export options are hidden or disabled.**
- Export buttons replaced with "Export Restricted" button for unauthorized users
- Clear visual indication (lock icon) that feature is restricted
- Preview remains available

✅ **Given an export is performed, when audit logs are reviewed, then the log shows user, time, report id, variant, and output identifiers.**
- Audit log includes: user ID/name, timestamp, period ID, format, variant name
- Export history view displays all required information
- File checksums and generation IDs tracked for integrity

✅ **Owner-based permissions per report, not only global roles.**
- Period owners can export their reports
- Flexible permission model supports both global and per-report access

✅ **Least privilege by default.**
- New users have `CanExport=false`
- Contributors cannot export by default
- Explicit permission required for export access

## Files Modified

**Backend:**
- `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/ReportingModels.cs`
- `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/InMemoryReportStore.cs`
- `src/backend/Application/ARP.ESG_ReportStudio.API/Controllers/ReportingController.cs`
- `src/backend/Tests/SD.ProjectName.Tests.Products/ExportAccessControlTests.cs` (new)

**Frontend:**
- `src/frontend/src/lib/types.ts`
- `src/frontend/src/lib/helpers.ts`
- `src/frontend/src/components/Dashboard.tsx`
- `src/frontend/src/components/ExportHistoryView.tsx`

## Future Enhancements

1. **Fine-grained Permissions**: Add per-section export permissions
2. **Export Quotas**: Limit number of exports per user/period
3. **Watermarking**: Add user-specific watermarks to exports for tracking
4. **Time-based Permissions**: Allow exports only during specific periods
5. **Approval Workflow**: Require approval for sensitive exports
