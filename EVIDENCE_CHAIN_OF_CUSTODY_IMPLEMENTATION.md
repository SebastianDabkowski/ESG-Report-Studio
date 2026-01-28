# Evidence File Chain-of-Custody Implementation

## Overview

This implementation adds comprehensive chain-of-custody metadata tracking for evidence files in the ESG Report Studio, ensuring complete auditability and integrity validation for all evidence attachments.

## Acceptance Criteria Met

### ✅ File Metadata Storage
**Requirement**: Given a data record, when I upload an evidence file, then the system stores file metadata (uploader, time, filename, size, checksum).

**Implementation**:
- Enhanced `Evidence` model with:
  - `FileSize`: Size of the file in bytes
  - `Checksum`: SHA-256 hash of the file for integrity verification
  - `ContentType`: MIME type of the file
  - `IntegrityStatus`: Current integrity status ('valid', 'failed', 'not-checked')
- Upload endpoint automatically calculates SHA-256 checksum during file upload
- All metadata stored alongside evidence record

### ✅ Download Access Logging
**Requirement**: Given an evidence file, when it is downloaded, then the access is logged with user, time, and purpose.

**Implementation**:
- Created `EvidenceAccessLog` model with:
  - `Id`: Unique identifier
  - `EvidenceId`: Reference to the evidence file
  - `UserId`, `UserName`: User who accessed the file
  - `AccessedAt`: ISO 8601 timestamp
  - `Action`: Type of action ('download', 'view', 'validate')
  - `Purpose`: Optional description of why the file was accessed
- Download endpoint requires user information and optional purpose
- All access attempts are logged for audit trail

### ✅ Integrity Validation
**Requirement**: Given an evidence file, when its checksum does not match, then the system marks it as 'Integrity check failed' and blocks publication.

**Implementation**:
- `ValidateEvidenceIntegrity` method compares provided checksum against stored checksum
- Automatically updates `IntegrityStatus` to 'valid' or 'failed'
- `CanPublishEvidence` method blocks files with 'failed' status
- Download endpoint checks integrity status and blocks downloads of failed files
- Validation attempts are logged in access log

## Architecture

### Backend (.NET 9)

#### New/Enhanced Model Classes
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/ReportingModels.cs`

```csharp
public sealed class Evidence
{
    // ... existing fields ...
    
    // Chain-of-custody metadata
    public long? FileSize { get; set; }
    public string? Checksum { get; set; } // SHA-256 hash
    public string? ContentType { get; set; }
    public string IntegrityStatus { get; set; } = "not-checked"; // 'valid', 'failed', 'not-checked'
}

public sealed class EvidenceAccessLog
{
    public string Id { get; init; } = string.Empty;
    public string EvidenceId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string AccessedAt { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty; // 'download', 'view', 'validate'
    public string? Purpose { get; init; }
}
```

#### InMemoryReportStore Updates
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/InMemoryReportStore.cs`

New methods:
- `CreateEvidence`: Updated to accept fileSize, checksum, contentType parameters
- `LogEvidenceAccess`: Records access log entry with user, action, and optional purpose
- `GetEvidenceAccessLog`: Retrieves access log for specific evidence or all evidence
- `ValidateEvidenceIntegrity`: Validates checksum and updates integrity status
- `CanPublishEvidence`: Checks if evidence can be published (blocks 'failed' integrity)

#### Controller Updates
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Controllers/EvidenceController.cs`

Enhanced endpoints:
- `POST /api/evidence/upload`: Calculates SHA-256 checksum during upload, stores metadata
- `POST /api/evidence/{id}/download`: Downloads file with access logging
- `POST /api/evidence/{id}/validate`: Validates file integrity using checksum
- `GET /api/evidence/access-log?evidenceId={id}`: Retrieves access log entries

### Frontend (React 19 + TypeScript)

#### Type Definitions
**File**: `src/frontend/src/lib/types.ts`

```typescript
export interface Evidence {
  // ... existing fields ...
  
  // Chain-of-custody metadata
  fileSize?: number
  checksum?: string
  contentType?: string
  integrityStatus: string // 'valid', 'failed', 'not-checked'
}

export interface EvidenceAccessLog {
  id: string
  evidenceId: string
  userId: string
  userName: string
  accessedAt: string
  action: string // 'download', 'view', 'validate'
  purpose?: string
}
```

#### Component Updates
**File**: `src/frontend/src/components/EvidenceView.tsx`

- Displays integrity status with color-coded badges:
  - ✅ Green "Valid" with shield check icon
  - ❌ Red "Failed" with shield warning icon
  - ❓ Gray "Not Checked" with question icon
- Shows file size formatted (B, KB, MB)
- Displays content type (MIME type)
- Shows abbreviated checksum (first 16 characters) with full value on hover

## API Usage Examples

### Upload Evidence with Metadata
```http
POST /api/evidence/upload
Content-Type: multipart/form-data

{
  "sectionId": "section-123",
  "title": "Energy Consumption Invoice Q1 2024",
  "description": "Invoice from utility company",
  "uploadedBy": "user-1",
  "file": <binary file data>
}
```

**Response**:
```json
{
  "id": "evidence-456",
  "title": "Energy Consumption Invoice Q1 2024",
  "fileName": "invoice-q1.pdf",
  "fileSize": 51200,
  "checksum": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2",
  "contentType": "application/pdf",
  "integrityStatus": "valid",
  "uploadedAt": "2024-01-15T10:30:00Z"
}
```

### Download Evidence with Logging
```http
POST /api/evidence/{id}/download
Content-Type: application/json

{
  "userId": "user-2",
  "userName": "Jane Auditor",
  "purpose": "Annual audit review"
}
```

**Response**:
```json
{
  "fileUrl": "/api/evidence/files/file-uuid",
  "fileName": "invoice-q1.pdf",
  "message": "Access logged. In production, file would be streamed here."
}
```

### Validate Evidence Integrity
```http
POST /api/evidence/{id}/validate
Content-Type: application/json

{
  "checksum": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2",
  "userId": "user-2",
  "userName": "Jane Auditor"
}
```

**Success Response**:
```json
{
  "message": "Evidence file integrity validated successfully.",
  "integrityStatus": "valid",
  "checksum": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2"
}
```

**Failure Response** (400 Bad Request):
```json
{
  "error": "Checksum mismatch. File integrity validation failed.",
  "integrityStatus": "failed"
}
```

### Get Access Log
```http
GET /api/evidence/access-log?evidenceId=evidence-456
```

**Response**:
```json
[
  {
    "id": "log-1",
    "evidenceId": "evidence-456",
    "userId": "user-2",
    "userName": "Jane Auditor",
    "accessedAt": "2024-01-20T14:00:00Z",
    "action": "download",
    "purpose": "Annual audit review"
  },
  {
    "id": "log-2",
    "evidenceId": "evidence-456",
    "userId": "user-3",
    "userName": "Bob Reviewer",
    "accessedAt": "2024-01-18T09:30:00Z",
    "action": "view",
    "purpose": "Monthly review"
  }
]
```

## Testing

### Test Coverage
**File**: `src/backend/Tests/SD.ProjectName.Tests.Products/EvidenceChainOfCustodyTests.cs`

All 10 tests passing:
1. ✅ `CreateEvidence_WithMetadata_ShouldStoreChecksumAndFileSize`
2. ✅ `CreateEvidence_WithoutChecksum_ShouldHaveNotCheckedStatus`
3. ✅ `ValidateEvidenceIntegrity_WithMatchingChecksum_ShouldPass`
4. ✅ `ValidateEvidenceIntegrity_WithMismatchedChecksum_ShouldFail`
5. ✅ `CanPublishEvidence_WithValidIntegrity_ShouldReturnTrue`
6. ✅ `CanPublishEvidence_WithFailedIntegrity_ShouldReturnFalse`
7. ✅ `LogEvidenceAccess_ShouldCreateAccessLogEntry`
8. ✅ `GetEvidenceAccessLog_WithMultipleEntries_ShouldReturnAllForEvidence`
9. ✅ `GetEvidenceAccessLog_WithoutEvidenceId_ShouldReturnAllLogs`
10. ✅ `ValidateEvidenceIntegrity_WithoutChecksum_ShouldFail`

### Build Status
- ✅ Backend: Build successful
- ✅ Frontend: Build successful
- ✅ All new tests passing (10/10)
- ℹ️ 3 pre-existing test failures in AuditLogTests (unrelated to this implementation)

## Security Considerations

1. **Checksum Algorithm**: Uses SHA-256 for cryptographic hash generation
2. **File Size Limits**: 10MB maximum enforced in upload endpoint
3. **Content Type Validation**: Only allows specific file types (PDF, Word, Excel, CSV, PNG, JPEG)
4. **Filename Sanitization**: Removes path traversal and invalid characters
5. **Access Logging**: Complete audit trail of all file access
6. **Integrity Blocking**: Files with failed integrity checks cannot be downloaded or published

## Future Enhancements

1. **Actual File Storage**: Integrate with Azure Blob Storage or AWS S3
2. **Automatic Re-validation**: Periodic integrity checks of stored files
3. **Retention Policies**: Configure retention rules for evidence files (US-06-11)
4. **Bulk Operations**: Download multiple evidence files with single access log entry
5. **Advanced Search**: Filter evidence by integrity status, content type, date range
6. **Email Notifications**: Alert administrators of integrity check failures

## Notes

- Evidence can include invoices, HR reports, meter readings, calculations, and policies
- Checksum is calculated automatically during upload (transparent to users)
- Access log is append-only (cannot be modified or deleted)
- Integrity status defaults to 'valid' when checksum is provided during upload
- Files without checksums have 'not-checked' status and can still be downloaded
