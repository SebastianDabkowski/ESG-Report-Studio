# Fragment Change History - Implementation Guide

## Overview

This feature provides comprehensive change tracking and version comparison capabilities for all report fragments (sections, data points, gaps, assumptions, etc.) in the ESG Report Studio. It enables auditors and reviewers to view complete change history, compare versions, and understand who changed what and when.

## User Story

**As an Auditor** I want to view the full change history of any report section, paragraph, or data point so that I can verify who changed what and when.

## Architecture

### Backend Components

#### 1. Enhanced Audit Log Infrastructure

The existing audit log system has been extended with:

- **Timeline Endpoint**: `/api/audit-log/timeline/{entityType}/{entityId}`
  - Returns chronological list of all changes for a specific entity
  - Includes metadata (section names, evidence, comments)
  - Ordered oldest-first for timeline visualization

- **Version Comparison Endpoint**: `/api/audit-log/compare/{entityType}/{entityId}`
  - Compares two specific versions by reconstructing their states
  - Returns field-by-field differences
  - Categorizes changes as: added, removed, or modified
  - Query parameters: `fromVersion` and `toVersion` (audit log entry IDs)

#### 2. Metadata Enrichment

Helper methods in `InMemoryReportStore`:

```csharp
- GetSection(string id)                    // Get section by ID
- GetDataPointsForSection(string sectionId) // Get all data points in a section
- GetEvidenceForDataPoint(string dataPointId) // Get evidence linked to data point
- GetNotesForDataPoint(string dataPointId)   // Get notes for data point
- GetGap(string id)                        // Get gap by ID
- GetAssumption(string id)                 // Get assumption by ID
```

These methods provide contextual information to enrich timeline and comparison views.

### Frontend Components

#### 1. FragmentHistoryView Component

**Location**: `/src/frontend/src/components/FragmentHistoryView.tsx`

**Features**:
- Displays chronological timeline of all changes
- Shows change note, user, timestamp for each version
- Displays field-level before/after values
- Allows selection of 2 versions for comparison
- Shows related evidence and notes
- Supports all entity types (DataPoint, ReportSection, Gap, Assumption)

**Props**:
```typescript
{
  entityType: string    // "DataPoint", "ReportSection", "Gap", "Assumption"
  entityId: string      // Unique ID of the entity
  entityTitle?: string  // Optional display title
  onClose?: () => void  // Optional close callback
}
```

#### 2. VersionComparisonView Component

**Location**: `/src/frontend/src/components/VersionComparisonView.tsx`

**Features**:
- Side-by-side comparison of two versions
- Visual diff highlighting (green=added, red=removed, blue=modified)
- Shows version metadata (user, timestamp, action, notes)
- Detailed text comparison for long content fields
- Summary of changes by type

**Props**:
```typescript
{
  entityType: string
  entityId: string
  fromVersion: string   // Audit log entry ID (earlier version)
  toVersion: string     // Audit log entry ID (later version)
  onClose?: () => void
}
```

#### 3. HistoryDemoView Component

**Location**: `/src/frontend/src/components/HistoryDemoView.tsx`

Demo/launcher component for testing the history feature with sample data.

## API Documentation

### GET /api/audit-log/timeline/{entityType}/{entityId}

Returns chronological timeline of changes for a specific entity with metadata.

**Parameters**:
- `entityType` (path): Type of entity (e.g., "DataPoint", "Gap", "Assumption", "ReportSection")
- `entityId` (path): Unique identifier of the entity

**Response**:
```json
{
  "entityType": "DataPoint",
  "entityId": "dp-001",
  "totalChanges": 5,
  "metadata": {
    "title": "Total Energy Consumption",
    "sectionId": "section-001",
    "sectionName": "Energy & Emissions",
    "type": "metric",
    "evidenceCount": 2,
    "notesCount": 1,
    "evidence": [
      {
        "id": "ev-001",
        "fileName": "energy-data.xlsx",
        "uploadedAt": "2024-01-15T10:30:00Z"
      }
    ],
    "notes": [
      {
        "id": "note-001",
        "content": "Verified with facility manager",
        "createdAt": "2024-01-15T14:20:00Z",
        "createdBy": "user-2"
      }
    ]
  },
  "timeline": [
    {
      "id": "audit-001",
      "timestamp": "2024-01-15T09:00:00Z",
      "userId": "user-1",
      "userName": "Sarah Chen",
      "action": "create",
      "changeNote": "Initial data point creation",
      "changes": [
        {
          "field": "Title",
          "before": null,
          "after": "Total Energy Consumption"
        },
        {
          "field": "Content",
          "before": null,
          "after": "Annual energy usage in MWh"
        }
      ]
    }
  ]
}
```

**Status Codes**:
- `200 OK`: Timeline retrieved successfully
- `404 Not Found`: No audit history found for the specified entity

### GET /api/audit-log/compare/{entityType}/{entityId}

Compares two versions of an entity by reconstructing their states.

**Parameters**:
- `entityType` (path): Type of entity
- `entityId` (path): Unique identifier of the entity
- `fromVersion` (query): Audit log entry ID representing earlier version
- `toVersion` (query): Audit log entry ID representing later version

**Response**:
```json
{
  "entityType": "DataPoint",
  "entityId": "dp-001",
  "fromVersion": {
    "id": "audit-001",
    "timestamp": "2024-01-15T09:00:00Z",
    "userId": "user-1",
    "userName": "Sarah Chen",
    "action": "create",
    "changeNote": "Initial creation"
  },
  "toVersion": {
    "id": "audit-003",
    "timestamp": "2024-01-16T11:30:00Z",
    "userId": "user-2",
    "userName": "Mike Johnson",
    "action": "update",
    "changeNote": "Updated with final figures"
  },
  "metadata": {
    "title": "Total Energy Consumption",
    "sectionName": "Energy & Emissions",
    "type": "metric"
  },
  "differences": [
    {
      "field": "Content",
      "fromValue": "Annual energy usage in MWh",
      "toValue": "Annual energy usage: 1,234 MWh",
      "changeType": "modified"
    },
    {
      "field": "Value",
      "fromValue": null,
      "toValue": "1234",
      "changeType": "added"
    }
  ]
}
```

**Status Codes**:
- `200 OK`: Comparison completed successfully
- `400 Bad Request`: Invalid version IDs or fromVersion is not earlier than toVersion
- `404 Not Found`: Entity or version not found

## Usage Examples

### Viewing Change History

```typescript
import FragmentHistoryView from '@/components/FragmentHistoryView'

// In your component
<FragmentHistoryView
  entityType="DataPoint"
  entityId="dp-001"
  entityTitle="Total Energy Consumption"
  onClose={() => setShowHistory(false)}
/>
```

### Comparing Versions

The version comparison is automatically triggered when users select 2 versions in the history view. Alternatively, you can use it directly:

```typescript
import VersionComparisonView from '@/components/VersionComparisonView'

<VersionComparisonView
  entityType="DataPoint"
  entityId="dp-001"
  fromVersion="audit-001"
  toVersion="audit-003"
  onClose={() => setShowComparison(false)}
/>
```

### API Integration

```typescript
import { getEntityTimeline, compareVersions } from '@/lib/api'

// Get timeline
const timeline = await getEntityTimeline('DataPoint', 'dp-001')

// Compare versions
const comparison = await compareVersions(
  'DataPoint',
  'dp-001',
  'audit-001',
  'audit-003'
)
```

## Testing

### Backend Tests

Location: `/src/backend/Tests/SD.ProjectName.Tests.Products/AuditLogTests.cs`

**Test Coverage**:
1. `GetEntityTimeline_ShouldReturnChronologicalChanges`
   - Creates a data point and updates it multiple times
   - Verifies chronological ordering of changes
   - Validates change notes and field transitions

2. `CompareVersions_ShouldShowDifferencesBetweenTwoStates`
   - Creates and updates an assumption
   - Compares two versions by reconstructing states
   - Validates field-level differences

3. `Timeline_ShouldIncludeMetadataForDataPoints`
   - Verifies metadata retrieval
   - Tests section name resolution
   - Checks evidence and notes linking

**Run Tests**:
```bash
cd src/backend
dotnet test --filter "FullyQualifiedName~AuditLogTests"
```

### Manual Testing

1. Navigate to "Change History" tab in the UI
2. Select entity type (Data Point, Gap, Assumption, etc.)
3. Choose a sample entity or enter an ID
4. Click "View Change History"
5. Select two versions and click "Compare Selected"

## Security Considerations

- All audit log access should be restricted to users with appropriate roles (auditor, report-owner, admin)
- Change notes may contain sensitive information - apply appropriate access controls
- Version comparison reveals full change history - ensure users have permission to view the entity

## Future Enhancements

1. **Export History**: Add export functionality (PDF, Excel) for audit reports
2. **Restore Version**: Allow reverting to a previous version
3. **Change Annotations**: Enable reviewers to add comments on specific changes
4. **Automated Alerts**: Notify stakeholders of significant changes
5. **Advanced Filtering**: Filter timeline by date range, user, or change type
6. **Bulk Comparison**: Compare multiple entities at once

## Acceptance Criteria Met

✅ **Given a report fragment, when I open its history view, then I see a chronological list of versions with timestamp, author, and change summary.**
- Implemented in `FragmentHistoryView` component
- Timeline ordered oldest-first with all metadata
- Shows change notes, user names, and timestamps

✅ **Given two selected versions, when I request a comparison, then I see a diff highlighting additions, removals, and modifications.**
- Implemented in `VersionComparisonView` component
- Visual diff with color coding (green/red/blue)
- Categorizes changes by type (added/removed/modified)

✅ **Given a version entry, when I open details, then I see the related evidence links, decision references, and comments.**
- Metadata section shows evidence files and notes
- Each evidence entry includes filename and upload timestamp
- Notes show content, author, and creation time

## Related Documentation

- [AUDIT_LOG_IMPLEMENTATION.md](../AUDIT_LOG_IMPLEMENTATION.md) - Original audit log design
- [DATA_PROVENANCE_IMPLEMENTATION.md](../DATA_PROVENANCE_IMPLEMENTATION.md) - Data provenance tracking
- [architecture.md](../architecture.md) - Overall system architecture
