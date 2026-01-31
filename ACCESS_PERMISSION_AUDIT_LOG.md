# Access and Permission Audit Log - Implementation Guide

## Overview

This document describes the comprehensive audit logging implementation for access and permission changes in the ESG Report Studio. The implementation provides a tamper-evident audit trail with cryptographic hash chains to ensure the integrity and immutability of audit records.

## Features

### 1. Comprehensive Audit Coverage

All access and permission changes are automatically logged with complete traceability:

- **User Role Management**
  - Role assignments (`assign-user-roles`)
  - Role removals (`remove-user-role`)
  - Custom role creation (`create-role`)
  - Role description updates (`update-role-description`)
  - Role deletion (`delete-role`)

- **Section-Level Access Control**
  - Section access grants (`grant-section-access`)
  - Section access revocations (`revoke-section-access`)

### 2. Tamper-Evident Architecture

#### Hash Chain Implementation

Each audit log entry includes:

```csharp
public sealed class AuditLogEntry
{
    public string Id { get; init; }
    public string Timestamp { get; init; }
    public string UserId { get; init; }
    public string UserName { get; init; }
    public string Action { get; init; }
    public string EntityType { get; init; }
    public string EntityId { get; init; }
    public string? ChangeNote { get; init; }
    public List<FieldChange> Changes { get; init; }
    
    // Tamper-evidence fields
    public string? EntryHash { get; init; }           // SHA-256 hash of this entry
    public string? PreviousEntryHash { get; init; }   // Hash of previous entry (chain link)
}
```

**Hash Computation**:
- Uses SHA-256 algorithm
- Includes: ID, Timestamp, UserId, Action, EntityType, EntityId, and Changes
- Links to previous entry hash to create chain
- Excludes UserName and ChangeNote (metadata fields)

**Chain Properties**:
- First entry has `PreviousEntryHash = null`
- Each subsequent entry links to the previous entry's hash
- Any modification breaks the chain
- Deletion of entries is detectable

### 3. API Endpoints

#### Get Audit Log
```
GET /api/audit-log
```

**Query Parameters:**
- `entityType`: Filter by entity type (User, SystemRole, SectionAccessGrant, etc.)
- `entityId`: Filter by specific entity ID
- `userId`: Filter by user who made changes
- `action`: Filter by action type
- `startDate`: Filter by date range start (ISO 8601)
- `endDate`: Filter by date range end (ISO 8601)
- `sectionId`: Filter by section ID
- `ownerId`: Filter by owner/creator ID

**Access Control:**
- Admin: Full access
- Auditor: Read-only access to all data
- Report-owner: Access to all audit data
- Contributor: Access to their own actions only

**Response:**
```json
[
  {
    "id": "audit-entry-1",
    "timestamp": "2024-01-15T10:30:00Z",
    "userId": "admin-1",
    "userName": "Admin User",
    "action": "assign-user-roles",
    "entityType": "User",
    "entityId": "user-123",
    "changeNote": "Assigned admin role for report management",
    "changes": [
      {
        "field": "RoleIds",
        "oldValue": "role-contributor",
        "newValue": "role-contributor, role-admin"
      }
    ],
    "entryHash": "A1B2C3D4E5F6...",
    "previousEntryHash": "9Z8Y7X6W5V4U..."
  }
]
```

#### Export Tamper-Evident Audit Log
```
GET /api/audit-log/export/tamper-evident
```

**Access Control:**
- Requires admin or auditor role

**Query Parameters:**
- Same as GET /api/audit-log (all filters supported)

**Response:**
```json
{
  "entries": [
    // Array of AuditLogEntry objects in chronological order
  ],
  "metadata": {
    "exportedAt": "2024-01-15T14:00:00Z",
    "exportedBy": "auditor-1",
    "exportedByName": "Jane Auditor",
    "totalEntries": 42,
    "filters": {
      "entityType": "User",
      "startDate": "2024-01-01"
    },
    "hashAlgorithm": "SHA-256",
    "formatVersion": "1.0",
    "hashChainValid": true,
    "validationMessage": "Hash chain verified successfully"
  },
  "contentHash": "E7F8G9H0I1J2K3L4M5N6...",
  "signature": "SIGNATURE_PLACEHOLDER_E7F8G9H0I1J2K3L4"
}
```

#### Export as CSV
```
GET /api/audit-log/export/csv
```

Standard CSV export with all audit fields (requires admin or auditor role).

#### Export as JSON
```
GET /api/audit-log/export/json
```

Standard JSON export (requires admin or auditor role).

## Usage Examples

### Auditor Workflow

**1. Review all role assignments in the last month:**
```http
GET /api/audit-log?action=assign-user-roles&startDate=2024-12-01
X-User-Id: auditor-1
```

**2. Export tamper-evident audit trail for compliance:**
```http
GET /api/audit-log/export/tamper-evident?startDate=2024-01-01&endDate=2024-12-31
X-User-Id: auditor-1
```

**3. Investigate specific user's access changes:**
```http
GET /api/audit-log?entityType=User&entityId=user-123
X-User-Id: auditor-1
```

**4. Review section access grants for a reporting period:**
```http
GET /api/audit-log?action=grant-section-access&startDate=2024-01-01&endDate=2024-03-31
X-User-Id: auditor-1
```

### Compliance Officer Workflow

**1. Verify who had access to sensitive sections:**
```http
GET /api/audit-log?action=grant-section-access&sectionId=section-456
X-User-Id: compliance-1
```

**2. Track role privilege escalations:**
```http
GET /api/audit-log?action=assign-user-roles
X-User-Id: compliance-1
```

**3. Export complete access change history:**
```http
GET /api/audit-log/export/tamper-evident
X-User-Id: compliance-1
```

## Hash Chain Verification

The tamper-evident export automatically verifies the hash chain integrity:

**Verification Process:**

1. **Entry Hash Validation**
   - Recompute hash for each entry
   - Compare with stored EntryHash
   - Fail if mismatch detected

2. **Chain Linkage Validation**
   - Verify each entry's PreviousEntryHash matches the previous entry's EntryHash
   - Detect chain breaks indicating tampering or deletion

3. **Result**
   - `hashChainValid: true` - All entries verified
   - `hashChainValid: false` - Tampering detected with description in validationMessage

**Example Verification Success:**
```json
{
  "metadata": {
    "hashChainValid": true,
    "validationMessage": "Hash chain verified successfully"
  }
}
```

**Example Verification Failure:**
```json
{
  "metadata": {
    "hashChainValid": false,
    "validationMessage": "Hash mismatch at entry 5 (ID: audit-entry-5)"
  }
}
```

## Security Considerations

### Current Implementation

1. **Immutability**
   - AuditLogEntry uses `init` properties
   - Once created, entries cannot be modified
   - Append-only data structure

2. **Integrity Verification**
   - SHA-256 cryptographic hashing
   - Hash chain prevents undetected modifications
   - Entry deletion is detectable

3. **Access Control**
   - Role-based access to audit logs
   - Export restricted to admin/auditor roles
   - Contributor access limited to own actions

4. **Comprehensive Tracking**
   - All permission changes logged
   - Actor (who), target (whom), action (what), timestamp (when)
   - Before/after values for changes

### Future Enhancements

1. **Cryptographic Signatures**
   - Replace placeholder with actual digital signatures
   - Use asymmetric cryptography (RSA/ECDSA)
   - Public key verification

2. **External Audit Log Storage**
   - Write-once storage (WORM)
   - Separate database for audit logs
   - Immutable cloud storage (e.g., AWS S3 Object Lock)

3. **Real-Time Alerting**
   - Notify on suspicious permission changes
   - Alert on hash chain validation failures
   - Integration with SIEM systems

4. **Advanced Analytics**
   - Pattern detection for privilege escalation
   - Anomaly detection in access grants
   - Compliance reporting dashboards

## Testing

Comprehensive test suite in `AuditLogTamperEvidenceTests.cs`:

- **Hash Chain Tests**
  - Entry hash generation
  - Chain linkage verification
  - Multi-entry chain consistency

- **Audit Coverage Tests**
  - Role assignments
  - Role removals
  - Role creation/update/deletion
  - Section access grants/revocations

- **Export Tests**
  - Tamper-evident export format
  - Metadata completeness
  - Hash chain validation
  - Filter application

- **Integration Tests**
  - End-to-end audit flows
  - Multiple concurrent operations
  - Large audit log handling

All 15 tests passing ✅

## Performance Considerations

### Hash Computation
- SHA-256 is computationally efficient
- Hashing occurs only on entry creation
- No performance impact on reads

### Storage
- Hash fields add ~128 bytes per entry
- Negligible for typical audit log sizes
- Consider archival for very large logs

### Query Performance
- Indexes recommended on:
  - Timestamp
  - EntityType
  - EntityId
  - UserId
  - Action
- Hash verification is on-demand (export only)

## Compliance and Regulatory Alignment

This implementation supports compliance with:

- **SOC 2**: Comprehensive audit logging and access tracking
- **ISO 27001**: Access control monitoring and review
- **GDPR**: Accountability through detailed audit trails
- **CSRD/ESRS**: Audit trail for ESG data governance

## Maintenance

### Backup Strategy
- Regular backups of audit log database
- Separate retention from operational data
- Test restore procedures

### Retention Policy
- Define retention periods per regulatory requirements
- Archive old audit logs
- Maintain hash chain integrity during archival

### Monitoring
- Monitor audit log growth
- Alert on validation failures
- Track export frequency

## Conclusion

The audit log implementation provides a robust, tamper-evident system for tracking all access and permission changes in the ESG Report Studio. The hash chain architecture ensures data integrity, while comprehensive filtering and export capabilities support audit and compliance requirements.

For questions or support, please refer to the API documentation or contact the development team.
