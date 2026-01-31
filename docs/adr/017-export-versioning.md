# ADR-017: Backward-Compatible Data Export Contracts

## Status
Accepted

## Context

Enterprise customers integrate ESG Report Studio exports into their downstream tooling (data warehouses, BI systems, compliance platforms). Platform upgrades that change export formats can break these integrations, causing:

- Production failures in customer pipelines
- Manual intervention to update parsers
- Lost trust in platform stability
- Resistance to adopting new features

We need a versioning strategy that:
1. Allows us to evolve export formats
2. Maintains backward compatibility for existing consumers
3. Clearly communicates breaking vs non-breaking changes
4. Enables validation of export structure

## Decision

We will implement **Semantic Versioning** for all export formats (JSON, PDF, DOCX) with the following components:

### 1. Export Schema Versions

Each export format has a semantic version (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes requiring consumer updates
- **MINOR**: Backward-compatible additions (new optional fields)
- **PATCH**: Backward-compatible bug fixes

```csharp
public sealed class ExportSchemaVersion
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string Format { get; set; }
    public string VersionString => $"{Major}.{Minor}.{Patch}";
}
```

### 2. Export Metadata

Every export includes machine-readable version metadata:

```csharp
public sealed class ExportMetadata
{
    public string ExportId { get; set; }              // Unique instance ID
    public string Format { get; set; }                // json, pdf, docx
    public string SchemaVersion { get; set; }         // e.g., "1.0.0"
    public string SchemaIdentifier { get; set; }      // e.g., "esg-report-studio/json/v1"
    public string ExportedAt { get; set; }            // ISO 8601 timestamp
    // ... additional fields
}
```

For JSON exports, metadata is a top-level object:
```json
{
  "exportMetadata": { ... },
  "report": { ... }
}
```

For PDF/DOCX, metadata appears in:
- Document title page (structured section)
- Document footer (schema identifier and export ID)

### 3. Schema Registry

Centralized version management:

```csharp
public static class ExportSchemaRegistry
{
    public static readonly ExportSchemaVersion JsonV1 = new(1, 0, 0, "json");
    public static readonly ExportSchemaVersion PdfV1 = new(1, 0, 0, "pdf");
    public static readonly ExportSchemaVersion DocxV1 = new(1, 0, 0, "docx");
    
    public static ExportSchemaVersion GetCurrentVersion(string format);
    public static IEnumerable<ExportSchemaVersion> GetSupportedVersions(string format);
}
```

### 4. Version Selection

Consumers can request specific schema versions via export options:

```csharp
var options = new JsonExportOptions
{
    SchemaVersion = ExportSchemaRegistry.JsonV1  // Explicit version
};

var jsonBytes = jsonService.GenerateJson(report, options);
```

If not specified, current version is used.

### 5. Breaking Change Protocol

When a breaking change is needed:
1. Create new MAJOR version (e.g., v2.0.0)
2. Maintain old version for 12 months minimum
3. Provide migration guide
4. Mark old version as deprecated after 6 months
5. Remove old version after support period

### 6. Extensibility Pattern

New optional fields can be added without breaking changes:

```json
{
  "exportMetadata": {
    "schemaVersion": "1.1.0",
    // New optional field added in v1.1.0
    "complianceFlags": ["CSRD", "GRI"],
    ...
  },
  // Optional extensions object for future use
  "extensions": {
    "customMetrics": { ... }
  }
}
```

Consumers compatible with v1.0.0 can safely ignore unknown fields.

## Consequences

### Positive

1. **Stable Integrations**: Customers can upgrade platform without breaking their pipelines
2. **Clear Communication**: Semantic versioning signals breaking vs safe changes
3. **Validation Support**: Schema identifier enables automated compatibility checks
4. **Flexibility**: Can add features without disrupting existing consumers
5. **Audit Trail**: Export ID enables tracking which version produced each export
6. **Future-Proof**: Extensions field allows evolution without breaking changes

### Negative

1. **Complexity**: Must maintain multiple schema versions simultaneously
2. **Testing Burden**: Each version needs test coverage
3. **Documentation**: Must document changes across versions
4. **Storage**: Old version implementations must be retained during support period

### Neutral

1. **Performance**: Minimal overhead (metadata adds ~200 bytes per export)
2. **Migration Effort**: One-time implementation cost, ongoing maintenance is low

## Implementation Notes

### File Structure
```
src/backend/Application/ARP.ESG_ReportStudio.API/Services/
  - ExportSchemaVersion.cs     # Version model and registry
  - ExportMetadata.cs           # Metadata container
  - IJsonExportService.cs       # JSON export interface
  - JsonExportService.cs        # JSON export implementation
  - PdfExportService.cs         # Updated with metadata
  - DocxExportService.cs        # Updated with metadata
```

### Test Coverage
- Export versioning tests (11 tests)
- JSON export tests (6 tests)
- Integration tests for PDF/DOCX metadata inclusion

### Documentation
- `EXPORT_VERSIONING_POLICY.md` - Complete versioning policy
- `EXPORT_CHANGELOG.md` (future) - Version change history
- API documentation updated with version parameters

## Alternatives Considered

### 1. No Versioning
**Rejected**: Would force all consumers to adapt to every change, causing frequent breakage.

### 2. Date-Based Versions
**Rejected**: Doesn't communicate breaking vs non-breaking changes. E.g., "2024-01-15" vs "2024-02-20" doesn't indicate compatibility.

### 3. API Versioning Only
**Rejected**: Export format can change independently of API version. Export consumers may not use the API directly.

### 4. Content Negotiation Headers
**Rejected**: Only works for HTTP downloads. Doesn't help consumers who receive exports via email, S3, or other channels.

## Related Decisions

- ADR-012: Integration Connector Framework (references versioned schemas)
- ADR-014: Canonical Data Model (uses similar versioning approach)
- API Versioning Policy (API routes use separate versioning from export formats)

## Future Enhancements

### JSON Schema Definitions (v1.1.0)
Publish machine-readable JSON Schema for validation:
```json
{
  "$schema": "https://esg-report-studio.example.com/schemas/json/v1.0.0.json",
  "exportMetadata": { ... }
}
```

### Schema Migration Utilities (v1.2.0)
Command-line tools to convert between versions:
```bash
esg-migrate-export --from v1 --to v2 report.json
```

### Webhook Schema Notifications (v2.0.0)
Notify consumers when new versions are released or deprecations announced.

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [JSON Schema Specification](https://json-schema.org/)
- Canonical Data Model Implementation (similar versioning pattern)

---

**Decision Date**: 2024-01-31  
**Authors**: Platform Team, Integration Team  
**Reviewers**: Architecture Review Board
