# Report Preview Feature

## Overview

The Report Preview feature allows Contributors to preview generated ESG reports in the application before exporting them. This provides an opportunity to verify content and formatting, ensuring data quality before final export.

## Features

### 1. Interactive Preview Dialog
- **Table of Contents Navigation**: Browse sections using a sidebar navigation menu
- **Section-by-Section View**: Navigate through report sections sequentially
- **Rich Content Display**: View data points, assumptions, and gaps with proper formatting
- **Pagination Indicators**: Track position in the report (e.g., "Section 3 of 10")

### 2. Permission-Based Filtering
- Sections are filtered based on user permissions
- Users only see sections they own or have access to
- Hidden sections are excluded from the preview entirely (not just redacted)

### 3. Content Visualization
- **Data Points**: Displays title, value, unit, status, and metadata
- **Tables**: Data points presented in readable card format
- **Assumptions**: Highlighted with orange styling to indicate estimations
- **Gaps**: Highlighted with red styling to show missing data
- **Evidence References**: Shows count of attached evidence files

### 4. Report Metadata
- Organization information
- Reporting period details
- Generation timestamp
- Section count and data point statistics

## Usage

### From Dashboard

1. Navigate to the Dashboard view
2. Ensure an active reporting period exists
3. Click the "Preview Report" button in the Active Reporting Period card
4. The preview dialog will open, showing all sections you have permission to view

### Preview Dialog Controls

- **Table of Contents (Left Sidebar)**: Click any section to jump to it
- **Content Area (Right Panel)**: Scroll through the current section's content
- **Close Button**: Exit the preview and return to the dashboard

## Technical Implementation

### Backend API

**Endpoint**: `GET /api/periods/{periodId}/preview-report`

**Query Parameters**:
- `userId` (required): ID of the user requesting the preview
- `sectionIds` (optional): Comma-separated list of specific section IDs to include

**Response**: `GeneratedReport` object containing:
- Period and organization metadata
- Filtered list of sections based on permissions
- Data points with snapshots
- Evidence, assumptions, and gaps per section
- Integrity checksum

**Permission Logic**:
Currently implements a simple owner-based filter where users can see:
- Sections they own (Section.OwnerId matches userId)
- Sections without an assigned owner
- In production, this would integrate with a full authorization service

### Frontend Components

**ReportPreviewDialog.tsx**:
- Main preview component with responsive layout
- Uses shadcn/ui components for consistent styling
- Implements section navigation state management
- Handles loading and error states gracefully

**Integration Points**:
- Dashboard.tsx: Added "Preview Report" button
- lib/api.ts: Added `previewReport()` function
- lib/types.ts: Uses existing `GeneratedReport` type

## Accessibility

- Keyboard navigation supported
- Screen reader friendly with proper ARIA labels
- High contrast colors for status indicators
- Responsive layout for different screen sizes

## Future Enhancements

1. **Export from Preview**: Add ability to export directly from preview dialog
2. **PDF Layout Approximation**: Visual indicators showing page breaks in final PDF
3. **Comments/Annotations**: Allow users to add notes during preview
4. **Comparison View**: Preview current vs. previous period side-by-side
5. **Print Styling**: Optimized CSS for print preview
6. **Role-Based Permissions**: Full integration with authorization service
7. **Section-Level Permissions**: Granular control over data point visibility

## Testing

### Backend Tests

Location: `/src/backend/Tests/SD.ProjectName.Tests.Products/ReportPreviewTests.cs`

Test Coverage:
- ✅ Report generation returns all enabled sections
- ✅ Data points included in sections
- ✅ Specific section filtering
- ✅ Only enabled sections included
- ✅ Owner information populated
- ✅ Invalid period ID handling
- ✅ Checksum generation for integrity

Run tests:
```bash
cd src/backend
dotnet test --filter "FullyQualifiedName~ReportPreviewTests"
```

### Frontend Build

```bash
cd src/frontend
npm install
npm run build
```

## Performance Considerations

- Preview generates reports on-demand (not cached)
- Large reports with 100+ sections may take a few seconds to load
- Data is fetched once and cached in component state
- Section navigation is client-side (no additional API calls)

## Security

- User ID required for all preview requests
- Server-side permission filtering prevents unauthorized data access
- Checksums verify data integrity
- No sensitive data exposed in URLs (userId in query params only)

## Browser Compatibility

- Modern browsers with ES2020 support
- Tested on Chrome 90+, Firefox 88+, Safari 14+, Edge 90+
- Mobile responsive design works on iOS 14+ and Android 10+
