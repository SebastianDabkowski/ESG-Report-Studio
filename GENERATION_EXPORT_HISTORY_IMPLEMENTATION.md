# Generation and Export History Implementation

## Overview

This document describes the implementation of generation and export history tracking in the ESG Report Studio. This feature enables users to track all report generations, compare versions, mark versions as final, and maintain a complete audit trail of all exports.

## Acceptance Criteria Met

✅ **Generation History**: Track who generated what, when, which variant, and which structure/data snapshot was used.

✅ **Version Comparison**: Compare two generated versions with section-level differences and changed data sources highlighted.

✅ **Final Version Marking**: Mark versions as 'Final' with status and checksum stored to prevent confusion.

✅ **Immutable Identifiers**: Use SHA-256 checksums for all generated reports and exported files.

✅ **Export Tracking**: Complete audit trail of all PDF and DOCX exports with file metadata.

## Architecture

### Data Models

#### GenerationHistoryEntry
Represents a single report generation event:
- `Id`: Unique identifier matching the generated report ID
- `PeriodId`: Reporting period this generation belongs to
- `GeneratedAt`: ISO 8601 timestamp of generation
- `GeneratedBy`, `GeneratedByName`: User who generated
- `GenerationNote`: Optional note about the generation
- `Checksum`: SHA-256 hash of the report content
- `VariantId`, `VariantName`: Optional variant used
- `Status`: 'draft', 'final', or 'archived'
- `SectionCount`, `DataPointCount`, `EvidenceCount`: Summary statistics
- `SectionSnapshots`: List of sections with their catalog codes and data point counts
- `MarkedFinalAt`, `MarkedFinalBy`, `MarkedFinalByName`: Final marking metadata
- `Report`: Optional reference to the full GeneratedReport

#### SectionSnapshot
Captures section state at generation time:
- `SectionId`: Section identifier
- `SectionTitle`: Section title
- `CatalogCode`: Stable catalog code for cross-version tracking
- `DataPointCount`: Number of data points in this section

#### ExportHistoryEntry
Represents an export event:
- `Id`: Unique export identifier
- `GenerationId`: Generation this export is based on
- `PeriodId`: Reporting period
- `Format`: 'pdf' or 'docx'
- `FileName`: Generated file name
- `FileSize`: File size in bytes
- `FileChecksum`: SHA-256 hash of the exported file
- `ExportedAt`: ISO 8601 timestamp
- `ExportedBy`, `ExportedByName`: User who exported
- `VariantName`: Optional variant name
- `IncludedTitlePage`, `IncludedTableOfContents`, `IncludedAttachments`: Export options used
- `DownloadCount`: Number of times downloaded
- `LastDownloadedAt`: Last download timestamp

#### GenerationComparison
Result of comparing two generations:
- `Generation1`, `Generation2`: The two generations being compared
- `Period`: Reporting period for context
- `ComparedAt`: Timestamp of comparison
- `ComparedBy`: User who requested comparison
- `SectionDifferences`: List of section-level differences
- `ChangedDataSources`: List of section titles that changed
- `Summary`: Statistical summary of differences

#### GenerationSectionDifference
Describes a difference in a section between versions:
- `SectionId`, `SectionTitle`, `CatalogCode`: Section identification
- `DifferenceType`: 'added', 'removed', 'modified', or 'unchanged'
- `DataPointCount1`, `DataPointCount2`: Data point counts in each version
- `Changes`: List of specific changes detected

#### GenerationComparisonSummary
Summary statistics for comparison:
- `TotalSections`: Total number of sections compared
- `SectionsAdded`, `SectionsRemoved`, `SectionsModified`, `SectionsUnchanged`: Counts
- `TotalDataPoints1`, `TotalDataPoints2`: Total data point counts

## Backend Implementation

### InMemoryReportStore Extensions

#### Storage
Added two new lists for history tracking:
```csharp
private readonly List<GenerationHistoryEntry> _generationHistory = new();
private readonly List<ExportHistoryEntry> _exportHistory = new();
```

#### GenerateReport Enhancement
Modified to automatically create history entry:
- Creates `GenerationHistoryEntry` with all metadata
- Populates section snapshots for comparison
- Stores full report reference for later retrieval
- Adds entry to `_generationHistory`

#### New Methods

**GetGenerationHistory(string periodId)**
- Returns all generation history entries for a period
- Sorted by generation time (most recent first)
- Excludes full report from list view for efficiency

**GetGeneration(string generationId)**
- Returns a specific generation entry
- Includes the full GeneratedReport object

**MarkGenerationAsFinal(MarkGenerationFinalRequest request)**
- Updates generation status to 'final'
- Records who marked it and when
- Creates audit log entry
- Returns updated entry

**CompareGenerations(CompareGenerationsRequest request)**
- Validates both generations exist and are from same period
- Compares section snapshots
- Detects added, removed, and modified sections
- Calculates summary statistics
- Returns detailed comparison

**RecordExport(ExportHistoryEntry export)**
- Adds export entry to history
- Creates audit log entry

**GetExportHistory(string periodId)**
- Returns all exports for a period
- Sorted by export time (most recent first)

**GetExportHistoryForGeneration(string generationId)**
- Returns exports for a specific generation

**RecordExportDownload(string exportId)**
- Increments download count
- Updates last download timestamp

### API Endpoints (ReportingController)

#### GET /api/periods/{periodId}/generation-history
List all generations for a period.

**Response**: Array of GenerationHistoryEntry

#### GET /api/generation-history/{generationId}
Get a specific generation.

**Response**: GenerationHistoryEntry with full report

#### POST /api/generation-history/{generationId}/mark-final
Mark a generation as final.

**Request**:
```json
{
  "generationId": "gen-id",
  "userId": "user-id",
  "userName": "User Name",
  "note": "Optional note"
}
```

**Response**: Updated GenerationHistoryEntry

#### POST /api/generation-history/compare
Compare two generations.

**Request**:
```json
{
  "generation1Id": "gen1-id",
  "generation2Id": "gen2-id",
  "userId": "user-id"
}
```

**Response**: GenerationComparison object

#### GET /api/periods/{periodId}/export-history
List all exports for a period.

**Response**: Array of ExportHistoryEntry

### Export Enhancement

Both `ExportPdf` and `ExportDocx` endpoints now:
1. Generate the export file
2. Calculate SHA-256 checksum of file bytes
3. Create ExportHistoryEntry with all metadata
4. Call `RecordExport` to store in history
5. Return the file to user

## Frontend Implementation

### TypeScript Types
Added complete type definitions in `src/frontend/src/lib/types.ts`:
- `SectionSnapshot`
- `GenerationHistoryEntry`
- `ExportHistoryEntry`
- `MarkGenerationFinalRequest`
- `CompareGenerationsRequest`
- `GenerationSectionDifference`
- `GenerationComparisonSummary`
- `GenerationComparison`

### API Functions
Added in `src/frontend/src/lib/api.ts`:
- `getGenerationHistory(periodId)`
- `getGeneration(generationId)`
- `markGenerationFinal(generationId, payload)`
- `compareGenerations(payload)`
- `getExportHistory(periodId)`

### React Hooks
Created `src/frontend/src/hooks/useGenerationHistory.ts`:
- `useGenerationHistory(periodId)` - Query for generation history
- `useGeneration(generationId)` - Query for specific generation
- `useMarkGenerationFinal()` - Mutation for marking as final
- `useCompareGenerations()` - Mutation for comparing versions
- `useExportHistory(periodId)` - Query for export history

All hooks use TanStack Query for caching and state management.

### UI Components

#### GenerationHistoryView
**Location**: `src/frontend/src/components/GenerationHistoryView.tsx`

**Features**:
- Lists all generations for a period
- Shows generation metadata (user, timestamp, note, status)
- Displays statistics (section count, data point count, evidence count)
- Shows checksum for integrity verification
- Badge indicators for 'Final' vs 'Draft' status
- Variant name display
- Select up to 2 generations for comparison
- View generation details
- Mark as final button for draft generations
- Responsive card-based layout
- Scrollable history with recent first

#### VersionComparisonDialog
**Location**: `src/frontend/src/components/VersionComparisonDialog.tsx`

**Features**:
- Side-by-side version information
- Summary statistics card showing:
  - Total sections
  - Sections added/removed/modified/unchanged
  - Data point count changes
- Changed data sources highlighted as badges
- Section-by-section differences with:
  - Color-coded badges (added=green, removed=red, modified=amber)
  - Icons for difference types
  - Data point count changes
  - Detailed change descriptions
- Filters out unchanged sections by default
- Full-height scrollable dialog

#### ExportHistoryView
**Location**: `src/frontend/src/components/ExportHistoryView.tsx`

**Features**:
- Lists all exports for a period
- Shows export metadata (format, file size, user, timestamp)
- Displays file checksums
- Download count tracking
- Last download timestamp
- Export options indicators (title page, TOC, attachments)
- Format badges (PDF/DOCX)
- Variant name display
- File size formatting (bytes/KB/MB/GB)
- Scrollable history

## Integration Points

To integrate the history views into the application:

1. **Dashboard/Period View**: Add tabs for "Generation History" and "Export History"
2. **After Report Generation**: Show link to view generation history
3. **After Export**: Show link to view export history
4. **Settings/Admin**: Retention policy configuration (future)

Example integration:
```tsx
import GenerationHistoryView from '@/components/GenerationHistoryView'
import ExportHistoryView from '@/components/ExportHistoryView'

function ReportDashboard({ period, currentUser }) {
  return (
    <Tabs>
      <TabsList>
        <TabsTrigger value="data">Data</TabsTrigger>
        <TabsTrigger value="generation">Generation History</TabsTrigger>
        <TabsTrigger value="exports">Export History</TabsTrigger>
      </TabsList>
      <TabsContent value="generation">
        <GenerationHistoryView 
          periodId={period.id} 
          currentUser={currentUser}
          onViewGeneration={(id) => {/* Handle view */}}
        />
      </TabsContent>
      <TabsContent value="exports">
        <ExportHistoryView periodId={period.id} />
      </TabsContent>
    </Tabs>
  )
}
```

## Security Considerations

### Checksums
- All reports use SHA-256 checksums calculated from deterministic content
- Export files have separate checksums calculated from binary content
- Checksums can be used to verify file integrity
- Final versions preserve checksums for tamper detection

### Audit Trail
- All generation events logged with user, timestamp, and reason
- Mark-final events logged with user and reason
- Export events logged with user and timestamp
- Comparison requests logged (not currently in audit log, but could be added)

### Immutability
- Generation history entries are append-only
- Once marked as 'Final', status cannot be changed back to draft
- Checksums are immutable once calculated
- Section snapshots preserve point-in-time state

## Retention and Deletion

### Current Implementation
- All history is stored in memory
- No automatic deletion
- No retention policies enforced

### Future Enhancements
- Configurable retention policies by period or date range
- Legal hold functionality for litigation
- Automated archival of old generations
- Export file cleanup for archived generations
- Storage optimization for large history sets

## Testing

### Backend Tests
Created `GenerationHistoryTests.cs` with 8 comprehensive tests:

1. `GenerateReport_TracksHistoryEntry` - Verifies history entry created
2. `GetGeneration_ReturnsCorrectEntry` - Tests retrieval
3. `MarkGenerationAsFinal_UpdatesStatus` - Tests final marking
4. `MarkGenerationAsFinal_AlreadyFinal_ReturnsError` - Tests validation
5. `CompareGenerations_DetectsSectionDifferences` - Tests comparison
6. `CompareGenerations_DifferentPeriods_ReturnsError` - Tests validation
7. `RecordExport_AddsToHistory` - Tests export tracking
8. `GetExportHistoryForGeneration_FiltersCorrectly` - Tests filtering

All tests passing ✅

### Frontend Tests
- TypeScript compilation successful ✅
- Build successful ✅
- Components properly typed ✅

### Integration Tests
Manual testing recommended:
1. Generate multiple reports for a period
2. View generation history
3. Select two versions and compare
4. Mark a version as final
5. Export reports in PDF and DOCX
6. View export history
7. Verify checksums are displayed
8. Verify download counts increment

## Performance Considerations

### Backend
- History lists are in-memory for fast access
- No pagination currently implemented
- Comparison logic is O(n) where n is section count
- Checksum calculation is relatively fast (SHA-256)

### Frontend
- React Query caching prevents unnecessary API calls
- History views use ScrollArea for large lists
- Comparison dialog only shows changed sections
- File size formatting done client-side

### Optimization Opportunities
- Add pagination for very large history sets
- Implement server-side filtering
- Cache comparison results
- Lazy load full report details
- Add search/filter in UI

## Limitations

1. **In-Memory Storage**: History is lost on application restart
2. **No Pagination**: Could be slow with hundreds of generations
3. **No Search**: Users must scroll to find specific generations
4. **No Export File Storage**: Actual files not stored, only metadata
5. **No Download Tracking**: Download counts not actually incremented (no download endpoint)
6. **Comparison Depth**: Only section-level comparison, not data point-level
7. **No Rollback**: Cannot revert to previous generation

## Future Enhancements

1. **Persistent Storage**: Store history in database
2. **Export File Archive**: Store actual exported files for re-download
3. **Advanced Comparison**: Data point-level diff, visual diff
4. **Search and Filter**: Search by user, date, variant, note
5. **Pagination**: Support very large history sets
6. **Retention Policies**: Automated cleanup with configurable rules
7. **Email Notifications**: Alert on final marking or export
8. **Change Tracking**: Track who changed what between generations
9. **Rollback**: Restore a previous generation's data
10. **Bulk Operations**: Archive/delete multiple generations

## API Examples

### Get Generation History
```bash
GET /api/periods/period-123/generation-history
```

Response:
```json
[
  {
    "id": "gen-456",
    "periodId": "period-123",
    "generatedAt": "2024-01-15T10:30:00Z",
    "generatedBy": "user1",
    "generatedByName": "John Doe",
    "generationNote": "Initial version",
    "checksum": "abc123...",
    "status": "draft",
    "sectionCount": 10,
    "dataPointCount": 45,
    "evidenceCount": 12,
    "sectionSnapshots": [...]
  }
]
```

### Mark as Final
```bash
POST /api/generation-history/gen-456/mark-final
Content-Type: application/json

{
  "generationId": "gen-456",
  "userId": "user1",
  "userName": "John Doe",
  "note": "Approved for publication"
}
```

### Compare Generations
```bash
POST /api/generation-history/compare
Content-Type: application/json

{
  "generation1Id": "gen-456",
  "generation2Id": "gen-789",
  "userId": "user1"
}
```

Response includes detailed section differences and summary statistics.

### Get Export History
```bash
GET /api/periods/period-123/export-history
```

Response:
```json
[
  {
    "id": "export-111",
    "generationId": "gen-456",
    "periodId": "period-123",
    "format": "pdf",
    "fileName": "report_2024.pdf",
    "fileSize": 1048576,
    "fileChecksum": "def456...",
    "exportedAt": "2024-01-15T11:00:00Z",
    "exportedBy": "user1",
    "exportedByName": "John Doe",
    "includedTitlePage": true,
    "includedTableOfContents": true,
    "includedAttachments": false,
    "downloadCount": 0
  }
]
```

## Conclusion

The generation and export history implementation provides a complete audit trail for all report generation and export activities. It enables version comparison, final version marking, and comprehensive tracking of all exports with checksums for integrity verification.

The system meets all acceptance criteria and provides a solid foundation for future enhancements around retention policies, advanced comparison, and persistent storage.
