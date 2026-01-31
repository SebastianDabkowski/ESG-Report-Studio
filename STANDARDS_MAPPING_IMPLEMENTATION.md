# Standards Mapping Implementation - Technical Documentation

## Overview

This implementation adds comprehensive many-to-many mapping capabilities between report sections/data points and reporting standard disclosures (e.g., CSRD/ESRS, GRI, TCFD). It enables:

- Multiple standards to be mapped to the same content
- Multiple sections/data points to satisfy a single standard requirement
- Coverage analysis showing compliance gaps
- Versioned snapshots for export consistency
- Full audit trail with traceability

## Architecture

### Entity Model

```
StandardsCatalogItem (existing)
├─ StandardDisclosure (new)
│  ├─ SectionDisclosureMapping (new, many-to-many)
│  └─ DataPointDisclosureMapping (new, many-to-many)
│
ReportingPeriod
├─ ReportSection
│  ├─ SectionDisclosureMapping → StandardDisclosure
│  └─ DataPoint
│     └─ DataPointDisclosureMapping → StandardDisclosure
│
MappingVersion (new, versioned snapshots)
```

### Key Entities

#### StandardDisclosure
Represents individual disclosure requirements within a standard (e.g., "ESRS E1-1: Climate Change Mitigation").

**Fields:**
- `disclosureCode`: External reference (e.g., "ESRS E1-1", "GRI 305-1")
- `title`, `description`: Human-readable details
- `category`: environmental | social | governance
- `topic`: Sub-category for filtering (e.g., "Climate Change", "Workforce")
- `isMandatory`: Whether required for compliance
- Audit fields: `createdAt`, `createdBy`, `updatedAt`, `updatedBy`

#### SectionDisclosureMapping
Many-to-many mapping between `ReportSection` and `StandardDisclosure`.

**Fields:**
- `sectionId`, `disclosureId`: Foreign keys
- `coverageLevel`: "full" | "partial" | "reference"
- `notes`: Optional explanation of how section addresses disclosure
- Audit fields

#### DataPointDisclosureMapping
Many-to-many mapping between `DataPoint` and `StandardDisclosure` (more granular than section-level).

**Fields:**
- Same structure as `SectionDisclosureMapping`

#### MappingVersion
Versioned snapshot of all mappings at a point in time (e.g., when report is approved/exported).

**Fields:**
- `periodId`: Which reporting period
- `versionNumber`: Incremental version
- `mappingsSnapshot`: Serialized JSON of all mappings
- `reason`: Why snapshot was created (e.g., "Report approved", "Export generated")

## API Endpoints

### Standard Disclosures

```
GET    /api/standard-disclosures?standardId={id}&category={cat}&topic={topic}&mandatoryOnly={bool}
GET    /api/standard-disclosures/{id}
POST   /api/standard-disclosures
PUT    /api/standard-disclosures/{id}
DELETE /api/standard-disclosures/{id}
```

### Section Disclosure Mappings

```
GET    /api/mappings/section-disclosures?sectionId={id}&disclosureId={id}
GET    /api/mappings/section-disclosures/{id}
POST   /api/mappings/section-disclosures
PUT    /api/mappings/section-disclosures/{id}
DELETE /api/mappings/section-disclosures/{id}
```

### DataPoint Disclosure Mappings

```
GET    /api/mappings/datapoint-disclosures?dataPointId={id}&disclosureId={id}
GET    /api/mappings/datapoint-disclosures/{id}
POST   /api/mappings/datapoint-disclosures
PUT    /api/mappings/datapoint-disclosures/{id}
DELETE /api/mappings/datapoint-disclosures/{id}
```

### Mapping Versions

```
GET    /api/mappings/versions?periodId={id}
GET    /api/mappings/versions/{id}
POST   /api/mappings/versions
```

### Coverage Analysis

```
GET    /api/coverage?standardId={id}&periodId={id}&category={cat}&topic={topic}
```

Returns `StandardCoverageAnalysis` with:
- Overall coverage percentage
- Counts: total, fully covered, partially covered, not covered
- Detailed breakdown per disclosure with mapped sections/data points

## Frontend Components

### StandardsCoverageView
**Path:** `src/frontend/src/components/StandardsCoverageView.tsx`

**Purpose:** Visualize standards coverage for a reporting period

**Features:**
- Select standard and reporting period
- Filter by category and topic
- View coverage summary (overall %, fully/partially/not covered counts)
- Detailed disclosure table showing:
  - Coverage status (full/partial/missing)
  - Mapped sections and data points
  - Mandatory vs optional
- Export coverage to CSV

**Usage:**
```tsx
import StandardsCoverageView from '@/components/StandardsCoverageView'

<StandardsCoverageView />
```

### StandardDisclosuresManagement
**Path:** `src/frontend/src/components/StandardDisclosuresManagement.tsx`

**Purpose:** Manage disclosure definitions within standards

**Features:**
- Select a standard to manage
- Create new disclosures (code, title, description, category, topic, mandatory flag)
- Edit existing disclosures
- Delete disclosures (cascades to remove mappings)

**Usage:**
```tsx
import StandardDisclosuresManagement from '@/components/StandardDisclosuresManagement'

<StandardDisclosuresManagement />
```

## Coverage Calculation Logic

Coverage status is determined hierarchically:

1. **Full Coverage**: At least one section or data point has `coverageLevel = "full"`
2. **Partial Coverage**: Has mappings but none are "full", OR has `coverageLevel = "partial"`
3. **Missing**: No mappings exist

Coverage percentage formula:
```
(fullyCovered + (partiallyCovered * 0.5)) / totalDisclosures * 100
```

Partial coverage counts as 50% toward overall coverage.

## Workflow Examples

### Setup: Define Standard Disclosures

1. Navigate to Standard Disclosures Management
2. Select a standard (e.g., "CSRD/ESRS 2024")
3. Click "Add Disclosure"
4. Enter details:
   - Code: "ESRS E1-1"
   - Title: "Climate Change Mitigation"
   - Category: Environmental
   - Topic: "Climate Change"
   - Mandatory: Yes
5. Repeat for all disclosures

### Map Sections to Disclosures

Use API or create UI component:

```typescript
import { createSectionDisclosureMapping } from '@/lib/api'

await createSectionDisclosureMapping({
  sectionId: 'section-123',
  disclosureId: 'disclosure-456',
  coverageLevel: 'full',
  notes: 'This section fully addresses climate mitigation targets'
})
```

### Analyze Coverage

1. Navigate to Standards Coverage View
2. Select standard and reporting period
3. Apply filters (category, topic)
4. Review:
   - Overall coverage percentage
   - Which disclosures are covered/missing
   - Which sections/data points map to each disclosure
5. Export to CSV for documentation

### Create Export Snapshot

Before generating an export:

```typescript
import { createMappingVersion } from '@/lib/api'

await createMappingVersion({
  periodId: 'period-123',
  reason: 'Report approved for Q4 2024'
})
```

This creates an immutable snapshot of all mappings at this point in time.

## Integration Points

### Existing Features

This implementation integrates with:
- **Standards Catalogue** (existing): Defines available standards
- **Section Catalog** (existing): Templates for sections
- **Report Sections** (existing): Instances in reporting periods
- **Data Points** (existing): ESG metrics and narratives
- **Audit Logging** (existing): All operations are audited

### Export Pipeline

To use mapping versions in exports:

1. Retrieve latest `MappingVersion` for the reporting period
2. Deserialize `mappingsSnapshot` JSON
3. Use snapshot data instead of current mappings
4. This ensures export consistency even if mappings change later

Example:
```typescript
const versions = await getMappingVersions(periodId)
const latestVersion = versions[versions.length - 1]
const snapshot = JSON.parse(latestVersion.mappingsSnapshot)
// Use snapshot.SectionMappings and snapshot.DataPointMappings
```

## Security & Validation

### Validation Rules

1. **Unique disclosure codes**: Per standard, `disclosureCode` must be unique
2. **Valid coverage levels**: Must be "full", "partial", or "reference"
3. **Valid references**: Section/DataPoint/Disclosure must exist
4. **No duplicate mappings**: Same section/datapoint cannot map to same disclosure twice

### Authorization

- All operations use user context for audit trail
- TODO: Implement role-based access control (admin, report-owner, etc.)

### Cascade Deletes

Deleting a `StandardDisclosure`:
- Removes all `SectionDisclosureMapping` records
- Removes all `DataPointDisclosureMapping` records
- No orphaned mappings remain

## Performance Considerations

### Coverage Analysis

Coverage calculation is performed in-memory for responsiveness. For large datasets:
- Consider caching coverage results
- Use pagination for disclosure details
- Add database indexes on foreign keys

### Filtering

Filters (category, topic, standard, period) are applied at query time for accuracy.

### Export Snapshots

Snapshots are stored as JSON strings. For very large datasets (1000s of mappings):
- Consider compression
- Use separate storage (blob storage)
- Implement snapshot expiry policy

## Testing Recommendations

### Backend Tests

```csharp
[Fact]
public void CreateStandardDisclosure_UniqueCodePerStandard()
{
    // Test that duplicate codes within a standard are rejected
}

[Fact]
public void CreateSectionDisclosureMapping_ManyToMany()
{
    // Test that one section can map to multiple disclosures
    // Test that one disclosure can have multiple sections
}

[Fact]
public void CalculateCoverage_CorrectPercentage()
{
    // Test coverage calculation formula
}

[Fact]
public void CreateMappingVersion_CapturesSnapshot()
{
    // Test that snapshot contains all current mappings
}
```

### Frontend Tests

- Component rendering with filters
- Coverage percentage display
- CSV export functionality
- Form validation
- API integration

## Migration Notes

### Existing Data

This is a new feature with no existing data migration needed.

### Sample Data

To populate sample data (for testing):

```typescript
// Create standard disclosures for CSRD/ESRS
const esrsDisclosures = [
  { code: 'ESRS E1-1', title: 'Climate Change Mitigation', category: 'environmental', topic: 'Climate Change', mandatory: true },
  { code: 'ESRS S1-1', title: 'Own Workforce', category: 'social', topic: 'Workforce', mandatory: true },
  { code: 'ESRS G1-1', title: 'Business Conduct', category: 'governance', topic: 'Ethics', mandatory: true }
]

for (const disclosure of esrsDisclosures) {
  await createStandardDisclosure({ standardId: 'csrd-2024', ...disclosure })
}
```

## Future Enhancements

### Potential Improvements

1. **Automatic Mapping Suggestions**: ML-based suggestions for likely mappings
2. **Bulk Import**: CSV import for disclosure definitions
3. **Mapping Templates**: Pre-configured mappings for common standards
4. **Comparison View**: Side-by-side coverage across multiple standards
5. **Gap Remediation**: Link missing disclosures to remediation plans
6. **Conflict Detection**: Warn if sections claim full coverage for overlapping disclosures
7. **Weighted Coverage**: Different weights for mandatory vs optional disclosures

### Roadmap

- **Phase 1 (Complete)**: Core infrastructure and API
- **Phase 2**: Enhanced UI for mapping management
- **Phase 3**: Integration with export pipeline
- **Phase 4**: Advanced analytics and reporting
- **Phase 5**: Multi-tenant customization

## Support & Troubleshooting

### Common Issues

**Q: Coverage percentage seems incorrect**
- Check that mappings exist for the selected period's sections
- Verify coverage levels are set correctly (full/partial/reference)
- Ensure filters aren't excluding expected disclosures

**Q: Can't see my mappings in coverage view**
- Confirm section/datapoint belongs to the selected reporting period
- Check that disclosure belongs to the selected standard
- Verify mappings weren't accidentally deleted

**Q: Export uses old mappings**
- Create a new mapping version snapshot after making changes
- Ensure export pipeline retrieves the latest version

### Debug Queries

```typescript
// Check all mappings for a section
const sectionMappings = await getSectionDisclosureMappings(sectionId)

// Check all mappings for a disclosure
const disclosureMappings = await getSectionDisclosureMappings(undefined, disclosureId)

// Get coverage for debugging
const coverage = await getStandardCoverageAnalysis(standardId, periodId)
console.log('Coverage:', coverage.coveragePercentage, '%')
console.log('Details:', coverage.disclosureDetails)
```

## Conclusion

This implementation provides a robust, auditable, and scalable solution for mapping report content to multiple compliance standards. The many-to-many relationship model ensures flexibility, while versioning ensures export consistency and traceability.

For questions or issues, refer to the API documentation or contact the development team.
