# Backward-Compatible Data Export Contracts - Implementation Summary

## Overview

This implementation establishes versioned export schemas for ESG Report Studio data exports (JSON, PDF, DOCX), ensuring downstream tooling remains compatible across platform upgrades. The solution uses semantic versioning to clearly communicate breaking vs non-breaking changes.

## Acceptance Criteria

✅ **New Optional Fields**: Existing consumers can ignore new optional fields without breaking parsing  
✅ **Breaking Change Handling**: Breaking changes are released as new export versions with prior versions remaining available  
✅ **Version Metadata**: Exports include export version and schema identifier for validation  
✅ **Semantic Versioning**: Export schemas follow semantic versioning (MAJOR.MINOR.PATCH)  
✅ **Machine-Readable Schema**: JSON exports provide structured metadata for downstream validation

## Architecture

### Core Components

#### 1. ExportSchemaVersion
Represents a versioned export schema with semantic versioning support.

**Key Features:**
- Semantic version parsing (MAJOR.MINOR.PATCH)
- Breaking change detection
- Backward compatibility validation
- Format-specific versioning (JSON, PDF, DOCX)

**Example:**
```csharp
var version = new ExportSchemaVersion(1, 0, 0, "json");
version.VersionString; // "1.0.0"

var v2 = ExportSchemaVersion.Parse("2.0.0", "json");
v2.IsBreakingChangeFrom(version); // true
```

#### 2. ExportSchemaRegistry
Centralized registry for managing supported schema versions.

**Current Versions:**
- JSON: v1.0.0
- PDF: v1.0.0
- DOCX: v1.0.0

**Example:**
```csharp
var currentJson = ExportSchemaRegistry.GetCurrentVersion("json");
var allJsonVersions = ExportSchemaRegistry.GetSupportedVersions("json");
```

#### 3. ExportMetadata
Metadata container included in all exports for version validation.

**Fields:**
- `exportId` - Unique instance identifier
- `format` - Export format (json, pdf, docx)
- `schemaVersion` - Semantic version (e.g., "1.0.0")
- `schemaIdentifier` - Schema ID for validation (e.g., "esg-report-studio/json/v1")
- `exportedAt` - ISO 8601 timestamp
- `exportedBy` / `exportedByName` - User information
- `periodId` / `periodName` - Reporting period
- `generationId` - Source report generation ID
- `variantName` - Optional variant name

**Example:**
```csharp
var metadata = ExportMetadata.FromSchemaVersion(
    ExportSchemaRegistry.GetCurrentVersion("json"),
    "user123",
    "Jane Doe"
);
metadata.SchemaIdentifier; // "esg-report-studio/json/v1"
```

#### 4. JSON Export Service
New service for machine-readable JSON exports with versioned schema.

**Features:**
- Structured JSON with top-level metadata and report sections
- Configurable data filtering (evidence, assumptions, gaps)
- Formatted or compact output
- Schema version selection

**Example:**
```csharp
var jsonService = new JsonExportService();
var options = new JsonExportOptions
{
    IncludeEvidence = true,
    IncludeAssumptions = true,
    FormatOutput = true,
    SchemaVersion = ExportSchemaRegistry.JsonV1
};

byte[] jsonBytes = jsonService.GenerateJson(report, options);
```

**JSON Structure:**
```json
{
  "exportMetadata": {
    "exportId": "abc-123-def-456",
    "format": "json",
    "schemaVersion": "1.0.0",
    "schemaIdentifier": "esg-report-studio/json/v1",
    "exportedAt": "2024-01-31T10:30:00Z",
    "exportedBy": "user123",
    "exportedByName": "Jane Doe",
    "periodId": "period-456",
    "periodName": "2024 Annual Report",
    "generationId": "gen-789"
  },
  "report": {
    "id": "gen-789",
    "period": { ... },
    "organization": { ... },
    "sections": [ ... ],
    "generatedAt": "2024-01-30T15:00:00Z",
    "generatedBy": "user123",
    "generatedByName": "Jane Doe"
  },
  "extensions": null
}
```

#### 5. Enhanced PDF Export
Updated PDF export service to include version metadata.

**Metadata Locations:**
- **Title Page**: Structured metadata section with schema info
- **Footer**: Compact schema identifier and export ID
  - Example: `Export Schema: esg-report-studio/pdf/v1 (v1.0.0) | Export ID: abc-123`

**Example:**
```csharp
var pdfService = new PdfExportService(localizationService);
var options = new PdfExportOptions
{
    IncludeTitlePage = true,
    SchemaVersion = ExportSchemaRegistry.PdfV1,
    UserId = "user123",
    UserName = "Jane Doe"
};

byte[] pdfBytes = pdfService.GeneratePdf(report, options);
```

#### 6. Enhanced DOCX Export
Updated DOCX export service to include version metadata.

**Metadata Locations:**
- **Title Page**: Export Metadata table with version details
- **Footer**: Schema identifier and export ID

**Example:**
```csharp
var docxService = new DocxExportService(localizationService);
var options = new DocxExportOptions
{
    IncludeTitlePage = true,
    SchemaVersion = ExportSchemaRegistry.DocxV1,
    UserId = "user123",
    UserName = "Jane Doe"
};

byte[] docxBytes = docxService.GenerateDocx(report, options);
```

## Versioning Rules

### Non-Breaking Changes (MINOR)
Increment MINOR version for:
- Adding new optional fields
- Adding new optional sections
- Expanding enum values
- Documentation improvements

Example: `1.0.0` → `1.1.0`

### Breaking Changes (MAJOR)
Increment MAJOR version for:
- Removing fields
- Renaming fields
- Changing field types
- Making optional fields required
- Changing field semantics

Example: `1.2.3` → `2.0.0`

### Bug Fixes (PATCH)
Increment PATCH version for:
- Correcting data formatting
- Fixing calculations
- Typo corrections

Example: `1.1.0` → `1.1.1`

## Consumer Compatibility

### Validation Strategy
```javascript
// Check MAJOR version compatibility
function validateExport(exportMetadata) {
  const [major] = exportMetadata.schemaVersion.split('.');
  const expectedMajor = "1";
  
  if (major !== expectedMajor) {
    throw new Error(
      `Incompatible schema. Expected v${expectedMajor}, got v${major}`
    );
  }
}
```

### Handling New Fields
Consumers should:
1. Ignore unknown fields (forward compatibility)
2. Use defaults for missing optional fields
3. Validate presence of required fields
4. Log warnings for deprecated fields

## Testing

### Test Coverage

**ExportVersioningTests** (11 tests)
- Version string parsing
- Breaking change detection
- Backward compatibility validation
- Schema registry operations
- Metadata generation

**JsonExportTests** (6 tests)
- JSON generation with valid reports
- Version metadata inclusion
- Data filtering based on options
- Compact vs formatted output
- Custom schema version usage
- Filename generation

### Test Execution
```bash
# Run all export versioning tests
dotnet test --filter "FullyQualifiedName~ExportVersioningTests"

# Run JSON export tests
dotnet test --filter "FullyQualifiedName~JsonExportTests"
```

**Results**: All 17 tests passing ✅

## Files Modified/Created

### Created Files
```
src/backend/Application/ARP.ESG_ReportStudio.API/Services/
  - ExportSchemaVersion.cs          (159 lines)
  - ExportMetadata.cs                (120 lines)
  - IJsonExportService.cs            (113 lines)
  - JsonExportService.cs             (112 lines)

src/backend/Tests/SD.ProjectName.Tests.Products/
  - ExportVersioningTests.cs         (152 lines)
  - JsonExportTests.cs               (298 lines)

docs/adr/
  - 017-export-versioning.md         (260 lines)

Root:
  - EXPORT_VERSIONING_POLICY.md      (263 lines)
```

### Modified Files
```
src/backend/Application/ARP.ESG_ReportStudio.API/Services/
  - IPdfExportService.cs             (+12 lines)
  - PdfExportService.cs              (+23 lines)
  - IDocxExportService.cs            (+12 lines)
  - DocxExportService.cs             (+41 lines)
```

## Documentation

### Policy Documents
- **EXPORT_VERSIONING_POLICY.md** - Complete versioning policy for all export formats
  - Version format and schema identifiers
  - Change classification (breaking vs non-breaking)
  - Version support policy
  - Consumer guidelines
  - Migration procedures

### Architecture Decision Records
- **docs/adr/017-export-versioning.md** - ADR documenting design decisions
  - Context and requirements
  - Implementation approach
  - Consequences and tradeoffs
  - Alternatives considered
  - Future enhancements

## Usage Examples

### JSON Export with Version Control
```csharp
// Consumer specifies schema version for stability
var options = new JsonExportOptions
{
    SchemaVersion = ExportSchemaRegistry.JsonV1,
    UserId = "user123",
    UserName = "Jane Doe",
    VariantName = "Executive Summary",
    IncludeEvidence = true,
    FormatOutput = true
};

var jsonService = new JsonExportService();
byte[] exportBytes = jsonService.GenerateJson(report, options);

// Validate compatibility
var json = Encoding.UTF8.GetString(exportBytes);
var container = JsonSerializer.Deserialize<JsonExportContainer>(json);
ValidateSchemaCompatibility(container.ExportMetadata);
```

### PDF Export with Metadata
```csharp
// Metadata appears on title page and footer
var options = new PdfExportOptions
{
    IncludeTitlePage = true,
    IncludeTableOfContents = true,
    SchemaVersion = ExportSchemaRegistry.PdfV1,
    UserName = "Jane Doe",
    VariantName = "Stakeholder Report"
};

var pdfService = new PdfExportService(localizationService);
byte[] pdfBytes = pdfService.GeneratePdf(report, options);
```

### Future Version Migration
```csharp
// When v2.0.0 is released, consumers can choose
var v1Options = new JsonExportOptions
{
    SchemaVersion = ExportSchemaRegistry.JsonV1  // Still supported
};

var v2Options = new JsonExportOptions
{
    SchemaVersion = ExportSchemaRegistry.JsonV2  // New features
};
```

## Future Enhancements

### Planned for v1.1.0
- **JSON Schema Definitions**: Publish machine-readable schemas for validation
- **Schema URLs**: Host schemas at stable URLs (e.g., `https://schemas.example.com/json/v1.0.0.json`)
- **Additional Metadata**: Export generation duration, data completeness percentage

### Planned for v1.2.0
- **Migration Utilities**: CLI tools to convert between schema versions
- **Validation Tools**: Standalone validators for schema compliance
- **Documentation Generator**: Auto-generate API docs from schema definitions

### Planned for v2.0.0
- **Webhook Integration**: Notify consumers of version updates and deprecations
- **Enhanced Filtering**: Field-level inclusion/exclusion controls
- **Performance Metrics**: Include export performance data in metadata

## Migration Path

### For Existing Consumers
1. **No immediate action required** - Current exports now include version metadata
2. **Update parsers** to extract and validate `exportMetadata.schemaIdentifier`
3. **Implement version checking** to ensure compatibility
4. **Plan for future versions** using MAJOR version changes as upgrade triggers

### For New Consumers
1. **Always check** `exportMetadata.schemaIdentifier` before parsing
2. **Validate** MAJOR version matches expected version
3. **Ignore** unknown fields for forward compatibility
4. **Handle** missing optional fields gracefully

## Compliance Notes

This implementation satisfies the user story requirements:

✅ **Stable Export Formats**: Semantic versioning ensures consumers know when breaking changes occur  
✅ **Optional Field Additions**: MINOR version bumps allow new fields without breaking compatibility  
✅ **Version Availability**: Multiple versions can be supported simultaneously via `SchemaVersion` option  
✅ **Version Metadata**: All exports include schema version and identifier for validation  
✅ **Machine-Readable**: JSON exports provide structured metadata for automated processing

---

**Implementation Date**: 2024-01-31  
**Version**: 1.0.0  
**Status**: Complete  
**Test Coverage**: 17/17 tests passing
