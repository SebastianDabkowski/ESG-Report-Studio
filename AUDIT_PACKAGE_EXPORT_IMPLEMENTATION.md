# Audit Package Export Implementation

## Overview

The Audit Package Export feature allows Compliance Leads to generate comprehensive audit packages for external auditors. These packages bundle together report data, audit trails, decision logs, and evidence references in a single, verifiable ZIP file.

## User Story

**As a** Compliance Lead  
**I want to** export an audit package  
**So that** I can provide auditors with evidence, traceability, and decision history in a single bundle.

## Features

### Export Generation

- **Full Report Export**: Export all sections in a reporting period
- **Selective Export**: Choose specific sections to include
- **Metadata Tracking**: Records who exported, when, and package checksum
- **Integrity Verification**: SHA-256 checksums for the entire package

### Package Contents

Each audit package is a ZIP file containing:

1. **manifest.json** - Export metadata and summary
   - Export ID, timestamp, and user
   - Period information
   - Summary statistics (section count, data points, etc.)

2. **sections.json** - Complete section data
   - Section details and ownership
   - All data points with values and metadata
   - Provenance mappings (fragment audit views)
   - Gaps and assumptions

3. **audit-trail.json** - Filtered audit log
   - Changes to period, sections, and data points
   - Field-level change tracking
   - User attribution and timestamps

4. **decisions.json** - Decision log entries
   - ADR-style decisions linked to report fragments
   - Decision context, alternatives, and consequences
   - Version history for each decision

5. **evidence-references.json** - Evidence metadata
   - File names, URLs, and checksums
   - Integrity status
   - Links to associated data points

6. **README.txt** - Human-readable documentation
   - Package overview and contents
   - Usage instructions

## API Endpoints

### 1. Generate Export Metadata

**Endpoint:** `POST /api/audit-package/export`

**Request Body:**
```json
{
  "periodId": "period-id",
  "exportedBy": "user-id",
  "exportNote": "Optional description",
  "sectionIds": ["section-1", "section-2"]  // Optional: omit for all sections
}
```

**Response:**
```json
{
  "exportId": "export-id",
  "exportedAt": "2024-01-15T10:30:00Z",
  "exportedBy": "user-id",
  "checksum": "base64-checksum",
  "packageSize": 12345,
  "summary": {
    "periodId": "period-id",
    "periodName": "2024 Annual Report",
    "sectionCount": 6,
    "dataPointCount": 42,
    "auditLogEntryCount": 128,
    "decisionCount": 8,
    "assumptionCount": 5,
    "gapCount": 3,
    "evidenceFileCount": 15
  }
}
```

### 2. Download Audit Package

**Endpoint:** `POST /api/audit-package/export/download`

**Request Body:**
```json
{
  "periodId": "period-id",
  "exportedBy": "user-id",
  "exportNote": "Optional description",
  "sectionIds": ["section-1", "section-2"]  // Optional
}
```

**Response:** ZIP file download (`audit-package-{period-name}-{timestamp}.zip`)

### 3. View Export History

**Endpoint:** `GET /api/audit-package/exports/{periodId}`

**Response:**
```json
[
  {
    "id": "export-id",
    "periodId": "period-id",
    "sectionIds": ["section-1", "section-2", ...],
    "exportedAt": "2024-01-15T10:30:00Z",
    "exportedBy": "user-id",
    "exportedByName": "John Doe",
    "exportNote": "Q4 2024 audit",
    "checksum": "base64-checksum",
    "packageSize": 12345
  }
]
```

## Usage Examples

### Example 1: Export Full Report

```bash
curl -X POST http://localhost:5011/api/audit-package/export/download \
  -H "Content-Type: application/json" \
  -d '{
    "periodId": "4a2e6f81-e837-420e-bcf5-e1b4bb07e4bd",
    "exportedBy": "admin",
    "exportNote": "Complete 2024 audit package"
  }' \
  -o audit-package-2024.zip
```

### Example 2: Export Specific Sections

```bash
curl -X POST http://localhost:5011/api/audit-package/export/download \
  -H "Content-Type: application/json" \
  -d '{
    "periodId": "4a2e6f81-e837-420e-bcf5-e1b4bb07e4bd",
    "exportedBy": "admin",
    "exportNote": "Environmental sections only",
    "sectionIds": [
      "720f8813-159a-4b31-8f76-a7de9aa5e4bb",
      "01d17642-0868-46d3-a361-1ebd6ae71032"
    ]
  }' \
  -o audit-package-env.zip
```

### Example 3: View Export History

```bash
curl -X GET "http://localhost:5011/api/audit-package/exports/4a2e6f81-e837-420e-bcf5-e1b4bb07e4bd"
```

## Acceptance Criteria

✅ **Given** a report version, **when** I export an audit package, **then** it includes report output, provenance mappings, decision log, and audit trail extracts.

✅ **Given** selected scope, **when** I export, **then** I can choose full report or specific sections only.

✅ **Given** an export is generated, **when** it completes, **then** the system records who exported it and a checksum for the bundle.

## Implementation Details

### Backend Components

- **AuditPackageController** (`/api/audit-package`)
  - Handles export requests
  - Generates ZIP files
  - Tracks export history

- **InMemoryReportStore** (service methods)
  - `GenerateAuditPackage()` - Creates export metadata
  - `BuildAuditPackageContents()` - Assembles package data
  - `RecordAuditPackageExport()` - Tracks exports
  - `GetAuditPackageExports()` - Retrieves history

- **Models** (ReportingModels.cs)
  - `ExportAuditPackageRequest`
  - `ExportAuditPackageResult`
  - `AuditPackageContents`
  - `AuditPackageSummary`
  - `AuditPackageExportRecord`
  - `EvidenceReference`
  - `SectionAuditData`

### Tests

9 comprehensive unit tests covering:
- Valid export generation
- Invalid period handling
- Section filtering
- Export history tracking
- Audit trail inclusion
- Decision log linkage

### Security Considerations

1. **Integrity Verification**: SHA-256 checksums ensure package integrity
2. **Access Control**: Export records include user attribution
3. **Audit Trail**: All exports are logged for compliance
4. **Evidence Handling**: Evidence files are referenced but not included in the package (separate retrieval)

## Future Enhancements

- [ ] Frontend UI for triggering exports
- [ ] Email notifications when exports are ready
- [ ] Scheduled/automated exports
- [ ] Digital signatures for exported packages
- [ ] Evidence file inclusion option (with access controls)
- [ ] Export templates for different audit frameworks
- [ ] Excel/PDF format options in addition to JSON
