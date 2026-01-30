# Audit Retention and Access Control API Documentation

## Overview

The ESG Report Studio now includes comprehensive audit data retention policies and role-based access controls to meet compliance requirements and protect sensitive information.

## Key Features

- **Retention Policies**: Configure data retention periods per tenant or report type
- **Access Controls**: Role-based permissions for audit log access
- **Cleanup Service**: Automated or manual cleanup with dry-run support
- **Deletion Reports**: Signed metadata-only reports for audit trail
- **Legal Holds**: Placeholder for future legal hold functionality

## Role-Based Access Control

### User Roles and Permissions

| Role | Audit Log Access | Export | Retention Management | Cleanup |
|------|------------------|--------|---------------------|---------|
| **admin** | Full access | ✅ | Full control | ✅ |
| **auditor** | Full access | ✅ | Read-only | ❌ |
| **report-owner** | Full access | ❌ | No access | ❌ |
| **contributor** | Own actions only | ❌ | No access | ❌ |

### Access Control Details

- **Admin**: Full access to all audit data, can export, create/update/delete retention policies, and run cleanup
- **Auditor**: Read-only access to all audit data and can export, but cannot modify policies or run cleanup
- **Report-Owner**: Can view all audit data but cannot export or manage retention policies
- **Contributor**: Can only view their own actions in the audit log

## API Endpoints

### Retention Policy Management

#### Create Retention Policy
```http
POST /api/retention/policies
Headers:
  X-User-Id: {userId}
  Content-Type: application/json

Body:
{
  "tenantId": "tenant-1",          // Optional, null for default
  "reportType": "simplified",      // Optional, null for all types
  "dataCategory": "audit-log",     // "audit-log", "evidence", or "all"
  "retentionDays": 365,            // Minimum 1 day
  "allowDeletion": true            // false for regulated customers
}

Response: 201 Created
Location: /api/retention/policies/{policyId}
{
  "id": "policy-123",
  "tenantId": "tenant-1",
  "reportType": "simplified",
  "dataCategory": "audit-log",
  "retentionDays": 365,
  "isActive": true,
  "priority": 15,
  "allowDeletion": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "createdBy": "user-2",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Authorization**: Requires `admin` role

#### Get Retention Policies
```http
GET /api/retention/policies?tenantId={tenantId}&activeOnly={true|false}
Headers:
  X-User-Id: {userId}

Response: 200 OK
[
  {
    "id": "policy-123",
    "tenantId": "tenant-1",
    "dataCategory": "audit-log",
    "retentionDays": 365,
    "priority": 15,
    ...
  }
]
```

**Authorization**: Requires `admin` or `auditor` role

#### Get Applicable Retention Policy
```http
GET /api/retention/policies/applicable?dataCategory={category}&tenantId={tenantId}&reportType={type}
Headers:
  X-User-Id: {userId}

Response: 200 OK
{
  "id": "policy-123",
  "dataCategory": "audit-log",
  "retentionDays": 365,
  ...
}
```

Returns the highest-priority matching policy for the given context.

**Authorization**: Requires `admin` or `auditor` role

#### Update Retention Policy
```http
PATCH /api/retention/policies/{policyId}
Headers:
  X-User-Id: {userId}
  Content-Type: application/json

Body:
{
  "retentionDays": 730,
  "allowDeletion": false
}

Response: 204 No Content
```

**Authorization**: Requires `admin` role

#### Deactivate Retention Policy
```http
DELETE /api/retention/policies/{policyId}
Headers:
  X-User-Id: {userId}

Response: 204 No Content
```

Note: This deactivates the policy but doesn't delete it (audit trail preservation).

**Authorization**: Requires `admin` role

### Cleanup Operations

#### Run Cleanup
```http
POST /api/retention/cleanup
Headers:
  X-User-Id: {userId}
  Content-Type: application/json

Body:
{
  "dryRun": true,              // true for preview, false for actual deletion
  "tenantId": "tenant-1"       // Optional, limit to specific tenant
}

Response: 200 OK
{
  "success": true,
  "wasDryRun": true,
  "recordsIdentified": 127,
  "recordsDeleted": 0,
  "deletionReportIds": [],
  "errorMessage": null,
  "executedAt": "2024-01-15T10:45:00Z"
}
```

**Important**: 
- Always run with `dryRun: true` first to preview what will be deleted
- Legal holds prevent deletion even if retention period has expired
- Deletion creates a signed deletion report for audit compliance

**Authorization**: Requires `admin` role

### Deletion Reports

#### Get Deletion Reports
```http
GET /api/retention/deletion-reports?tenantId={tenantId}
Headers:
  X-User-Id: {userId}

Response: 200 OK
[
  {
    "id": "report-456",
    "deletedAt": "2024-01-15T10:45:00Z",
    "deletedBy": "user-2",
    "policyId": "policy-123",
    "dataCategory": "audit-log",
    "recordCount": 127,
    "dateRangeStart": "2023-01-01T00:00:00Z",
    "dateRangeEnd": "2023-03-31T23:59:59Z",
    "tenantId": "tenant-1",
    "deletionSummary": "127 audit-log records from 2023-01-01T00:00:00Z to 2023-03-31T23:59:59Z",
    "signature": "a1b2c3d4...",
    "contentHash": "e5f6g7h8..."
  }
]
```

**Note**: Deletion reports contain only metadata about what was deleted, not the actual data.

**Authorization**: Requires `admin` or `auditor` role

### Audit Log Access (Enhanced)

#### Get Audit Log
```http
GET /api/audit-log?entityType={type}&userId={userId}&startDate={date}&endDate={date}
Headers:
  X-User-Id: {userId}

Response: 200 OK
[
  {
    "id": "audit-789",
    "timestamp": "2024-01-15T10:30:00Z",
    "userId": "user-1",
    "userName": "Sarah Chen",
    "action": "update",
    "entityType": "DataPoint",
    "entityId": "dp-123",
    "changeNote": "Updated value",
    "changes": [
      {
        "field": "Value",
        "oldValue": "100",
        "newValue": "150"
      }
    ]
  }
]
```

**Access Filtering**:
- `admin` and `auditor`: See all entries
- `report-owner`: See all entries
- `contributor`: Only see their own actions (automatically filtered)

**Authorization**: All authenticated users, filtered by role

#### Export Audit Log (CSV/JSON)
```http
GET /api/audit-log/export/csv?startDate={date}&endDate={date}
GET /api/audit-log/export/json?startDate={date}&endDate={date}
Headers:
  X-User-Id: {userId}

Response: 200 OK
Content-Type: text/csv or application/json
Content-Disposition: attachment; filename="audit-log-20240115-103000.csv"
```

**Authorization**: Requires `admin` or `auditor` role

## Retention Policy Priority System

Policies are prioritized based on specificity to ensure the most relevant policy is applied:

| Criteria | Priority Points |
|----------|----------------|
| Has `tenantId` | +10 |
| Has `reportType` | +5 |
| Has specific `dataCategory` (not "all") | +3 |

**Examples**:
1. Default policy (all tenants, all types, all categories): Priority = 0
2. Category-specific policy: Priority = 3
3. Tenant-specific policy: Priority = 10
4. Tenant + report type + category: Priority = 18 (most specific)

When multiple policies could apply, the system uses the highest-priority policy.

## Compliance Features

### Signed Deletion Reports

Every deletion operation creates a tamper-evident report:

1. **Content Hash**: SHA-256 hash of the report metadata
2. **Signature**: Cryptographic signature of the hash (simplified in current version)
3. **Metadata Only**: No sensitive data is stored in the report

### Legal Holds (Future)

Legal holds will prevent deletion of data even if retention period has expired:

```json
{
  "id": "hold-123",
  "tenantId": "tenant-1",
  "periodId": null,
  "reason": "Litigation hold",
  "referenceNumber": "CASE-2024-001",
  "isActive": true,
  "placedAt": "2024-01-15T10:00:00Z",
  "placedBy": "user-2",
  "expiresAt": null
}
```

## Configuration Examples

### Example 1: Default 1-Year Retention for All Data
```http
POST /api/retention/policies
{
  "dataCategory": "all",
  "retentionDays": 365,
  "allowDeletion": true
}
```

### Example 2: Tenant-Specific 3-Year Retention for Regulated Customer
```http
POST /api/retention/policies
{
  "tenantId": "regulated-corp",
  "dataCategory": "all",
  "retentionDays": 1095,
  "allowDeletion": false  // Prevents automatic deletion
}
```

### Example 3: Evidence Retention Separate from Audit Logs
```http
// Audit logs: 2 years
POST /api/retention/policies
{
  "dataCategory": "audit-log",
  "retentionDays": 730,
  "allowDeletion": true
}

// Evidence: 5 years
POST /api/retention/policies
{
  "dataCategory": "evidence",
  "retentionDays": 1825,
  "allowDeletion": true
}
```

## Cleanup Workflow

### Recommended Process

1. **Create Retention Policies** (one-time setup)
   ```bash
   POST /api/retention/policies
   ```

2. **Preview Cleanup** (before actual deletion)
   ```bash
   POST /api/retention/cleanup
   { "dryRun": true }
   ```

3. **Review Results**
   - Check `recordsIdentified` count
   - Verify date ranges match expectations
   - Confirm no legal holds are active

4. **Execute Cleanup**
   ```bash
   POST /api/retention/cleanup
   { "dryRun": false }
   ```

5. **Verify Deletion Report**
   ```bash
   GET /api/retention/deletion-reports
   ```

6. **Archive Deletion Report** (external system)
   - Store deletion reports in secure, immutable storage
   - Include signature for tamper detection

## Error Codes

| Status | Error | Description |
|--------|-------|-------------|
| 400 | Invalid retention days | Retention days must be at least 1 |
| 403 | Insufficient permissions | User lacks required role |
| 404 | Policy not found | Retention policy ID doesn't exist |
| 404 | No applicable policy | No policy matches the given criteria |

## Best Practices

1. **Always use dry-run first**: Preview deletions before executing
2. **Start with conservative retention**: Longer is safer than shorter
3. **Tenant-specific policies**: Override defaults for regulated customers
4. **Monitor deletion reports**: Regular audits of what was deleted
5. **Archive deletion reports**: Store in immutable audit log system
6. **Role separation**: Limit cleanup operations to admins only
7. **Legal holds**: Implement before production use if required

## Security Considerations

1. **Audit All Operations**: All retention policy changes and cleanups are logged
2. **Signed Reports**: Deletion reports include cryptographic signatures
3. **Metadata Only**: No sensitive data in deletion reports
4. **Role-Based Access**: Strict permissions on who can delete data
5. **No Bypass**: Legal holds cannot be overridden by retention policies

## Migration Guide

### From No Retention Policies

1. Create a default retention policy:
   ```bash
   POST /api/retention/policies
   {
     "dataCategory": "all",
     "retentionDays": 730,  # 2 years
     "allowDeletion": false  # Safe mode
   }
   ```

2. Run dry-run cleanup to see impact:
   ```bash
   POST /api/retention/cleanup
   { "dryRun": true }
   ```

3. Adjust retention days based on results

4. Enable deletion when ready:
   ```bash
   PATCH /api/retention/policies/{policyId}
   { "allowDeletion": true }
   ```

## Support

For questions or issues:
- Review the API documentation above
- Check deletion reports for audit trail
- Contact security team for compliance questions
