# Export Schema Versioning Policy

## Overview

This document defines the versioning policy for ESG Report Studio data export formats (JSON, PDF, DOCX). The policy ensures backward compatibility for downstream tooling and provides clear guidance on handling breaking changes.

## Version Format

Export schemas use **Semantic Versioning 2.0.0** (https://semver.org/):

```
MAJOR.MINOR.PATCH
```

- **MAJOR**: Breaking changes that require consumer updates
- **MINOR**: Backward-compatible additions (new optional fields)
- **PATCH**: Backward-compatible bug fixes

## Schema Identifiers

Each export includes a schema identifier in the format:

```
esg-report-studio/{format}/v{MAJOR}
```

Examples:
- `esg-report-studio/json/v1`
- `esg-report-studio/pdf/v1`
- `esg-report-studio/docx/v1`

The schema identifier uses only the MAJOR version to simplify consumer compatibility checks. Consumers compatible with `v1` should work with `v1.0.0`, `v1.1.0`, `v1.2.0`, etc.

## Change Classification

### Non-Breaking Changes (MINOR version bump)

These changes **DO NOT** break existing consumers:

1. **Adding new optional fields**
   - Consumers can ignore unknown fields
   - Example: Adding `exportMetadata.generationVersion` as an optional field

2. **Adding new optional sections**
   - Example: Adding an optional `auditTrail` section to JSON exports

3. **Expanding enum values**
   - Adding new values to existing enums (e.g., new export formats)
   - Existing values remain unchanged

4. **Improving documentation**
   - Clarifying field descriptions
   - Adding usage examples

### Breaking Changes (MAJOR version bump)

These changes **DO** break existing consumers and require a new major version:

1. **Removing fields**
   - Deleting any field from the export schema
   - Example: Removing `exportMetadata.exportId`

2. **Renaming fields**
   - Changing field names
   - Example: Renaming `schemaVersion` to `version`

3. **Changing field types**
   - Modifying the data type of existing fields
   - Example: Changing `fileSize` from number to string

4. **Making optional fields required**
   - Requiring previously optional fields
   - Example: Making `variantName` required

5. **Changing field semantics**
   - Altering the meaning or format of existing fields
   - Example: Changing `exportedAt` from UTC to local time

6. **Removing enum values**
   - Deleting values from existing enums

### Bug Fixes (PATCH version bump)

These changes fix errors without affecting compatibility:

1. **Correcting data formatting**
   - Example: Fixing date format to match ISO 8601

2. **Fixing calculation errors**
   - Example: Correcting percentage calculations

3. **Typo corrections**
   - In field values, not field names

## Version Support Policy

### Current Version
- Always the latest MAJOR.MINOR.PATCH release
- Recommended for all new integrations
- Receives all new features and bug fixes

### Previous MAJOR Versions
- **Support Period**: Minimum 12 months after next major version release
- **Maintenance**: Security fixes and critical bugs only
- **Deprecation Notice**: 6 months before end of support
- **Access**: Available via `SchemaVersion` option in export requests

### Deprecated Versions
- **Warning**: Included in export metadata
- **Removal**: After support period ends
- **Migration Guide**: Provided before deprecation

## Export Metadata

All exports include version metadata to enable compatibility validation:

### JSON Exports
```json
{
  "exportMetadata": {
    "exportId": "unique-guid",
    "format": "json",
    "schemaVersion": "1.0.0",
    "schemaIdentifier": "esg-report-studio/json/v1",
    "exportedAt": "2024-01-15T10:30:00Z",
    "exportedBy": "user123",
    "exportedByName": "Jane Doe",
    "periodId": "period-456",
    "periodName": "2024 Annual Report",
    "generationId": "gen-789",
    "variantName": "Executive Summary"
  },
  "report": { ... }
}
```

### PDF/DOCX Exports
- **Footer**: Schema identifier and version
- **Title Page**: Complete export metadata section
- Example footer: `Export Schema: esg-report-studio/pdf/v1 (v1.0.0) | Export ID: abc-123`

## Consumer Guidelines

### Parsing Strategy
1. **Check schema identifier** - Verify MAJOR version compatibility
2. **Ignore unknown fields** - New optional fields may appear in MINOR updates
3. **Validate required fields** - Ensure all expected fields are present
4. **Handle missing optional fields** - Use defaults when optional fields are absent

### Version Compatibility Check
```javascript
// Example: Consumer compatible with v1.x.x
function isCompatible(exportMetadata) {
  const [major] = exportMetadata.schemaVersion.split('.');
  const expectedMajor = "1";
  
  if (major !== expectedMajor) {
    throw new Error(
      `Incompatible schema version. Expected v${expectedMajor}.x.x, got v${exportMetadata.schemaVersion}`
    );
  }
  
  return true;
}
```

### Handling Deprecated Versions
When consuming a deprecated version:
1. Log a warning with migration timeline
2. Plan upgrade to current version
3. Monitor for end-of-support notifications

## Version Migration

### MAJOR Version Upgrades

When releasing a new MAJOR version:

1. **Announcement** - 3 months before release
2. **Beta Period** - 1 month with parallel access
3. **Release** - New version becomes current
4. **Migration Guide** - Detailed field mapping and changes
5. **Support Period** - Old version supported for 12 months

### Migration Tools

For JSON exports, provide migration utilities:
```bash
# Example: Convert v1 export to v2
migrate-export --from v1 --to v2 --input report-v1.json --output report-v2.json
```

## Schema Validation (Future)

**Status**: Planned for v1.1.0

JSON exports will include JSON Schema definitions:
- Published at: `https://esg-report-studio.example.com/schemas/{format}/v{MAJOR}.{MINOR}.{PATCH}.json`
- Enables automated validation
- Supports IDE autocomplete and type checking

Example usage:
```json
{
  "$schema": "https://esg-report-studio.example.com/schemas/json/v1.0.0.json",
  "exportMetadata": { ... },
  "report": { ... }
}
```

## Change Log

All version changes are documented in `EXPORT_CHANGELOG.md`:

```markdown
## [1.0.0] - 2024-01-01
### Initial Release
- JSON export with full report data
- PDF export with QuestPDF rendering
- DOCX export with OpenXML
- Export metadata with version tracking
```

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [JSON Schema](https://json-schema.org/)
- [ISO 8601 Date Format](https://en.wikipedia.org/wiki/ISO_8601)

## Version History

| Version | Release Date | Status | End of Support |
|---------|-------------|--------|----------------|
| 1.0.0   | 2024-01-01  | Current | N/A           |

---

**Last Updated**: 2024-01-31  
**Owner**: Platform Team  
**Review Frequency**: Quarterly
