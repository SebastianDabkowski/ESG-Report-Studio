# Report Variant Implementation

## Overview

This document describes the implementation of audience-specific report variants in the ESG Report Studio. Report variants enable the generation of different versions of a report tailored to specific audiences (e.g., Management, Bank, Client, Auditor) with configurable sections, detail levels, and redaction rules.

## Acceptance Criteria Met

✅ **Variant Settings**: Support for audience types (Management, Bank, Client, Auditor, Regulator, InternalTeam, Custom) with configurable sections and detail levels.

✅ **Redaction Rules**: Sensitive fields can be masked, removed, or replaced according to variant-specific redaction rules.

✅ **Variant Comparison**: Multiple variants can be compared to show which sections and fields differ and why.

## Architecture

### Data Models

#### ReportVariant
Core model representing a variant configuration:
- `Id`: Unique identifier
- `Name`: Display name (e.g., "Management Summary")
- `Description`: Purpose and intended use
- `AudienceType`: Target audience (management, bank, client, auditor, etc.)
- `Rules`: List of VariantRule for section/field filtering
- `RedactionRules`: List of RedactionRule for sensitive data handling
- `IsActive`: Whether the variant is available for use
- Audit fields: CreatedBy, CreatedAt, LastModifiedBy, LastModifiedAt

#### VariantRule
Defines inclusion/exclusion rules:
- `RuleType`: "include-section", "exclude-section", "include-field-group", "exclude-field-group", "exclude-attachments"
- `Target`: Section ID or field group identifier
- `Condition`: Optional conditional logic
- `Order`: Rule application precedence

#### RedactionRule
Specifies data redaction:
- `FieldIdentifier`: ID or pattern of field to redact
- `RedactionType`: "mask", "remove", or "replace"
- `ReplacementValue`: Value to use for "replace" type
- `Reason`: Audit reason for redaction

#### GeneratedReportVariant
Result of variant generation:
- `Report`: Base GeneratedReport with filtered/redacted data
- `Variant`: Variant configuration used
- `ExcludedSections`: List of section IDs excluded by rules
- `RedactedFields`: List of field IDs that were redacted
- `ExcludedAttachmentCount`: Number of attachments excluded

#### VariantComparison
Result of comparing variants:
- `Period`: Reporting period being compared
- `Variants`: List of variants in comparison
- `SectionDifferences`: Section-level differences
- `FieldDifferences`: Field-level differences

## API Endpoints

### Variant Management

#### GET /api/variants
List all report variants.

**Response**: Array of ReportVariant objects

#### GET /api/variants/{id}
Get a specific variant by ID.

**Response**: ReportVariant object

#### POST /api/variants
Create a new report variant.

**Request**:
```json
{
  "name": "Management Summary",
  "description": "High-level summary for management",
  "audienceType": "management",
  "createdBy": "user-id",
  "rules": [
    {
      "ruleType": "exclude-section",
      "target": "section-id-to-exclude",
      "order": 1
    }
  ],
  "redactionRules": [
    {
      "fieldIdentifier": "field-id",
      "redactionType": "mask",
      "reason": "Sensitive financial data"
    }
  ]
}
```

**Response**: Created ReportVariant

#### PUT /api/variants/{id}
Update an existing variant.

**Request**: UpdateVariantRequest (similar to create, includes isActive flag)

**Response**: Updated ReportVariant

#### DELETE /api/variants/{id}?deletedBy={userId}
Delete a variant.

**Response**: 204 No Content

### Variant Generation

#### POST /api/variants/generate
Generate a report using a specific variant.

**Request**:
```json
{
  "periodId": "reporting-period-id",
  "variantId": "variant-id",
  "generatedBy": "user-id",
  "generationNote": "Optional note"
}
```

**Response**: GeneratedReportVariant with filtered sections and redacted fields

#### POST /api/variants/compare
Compare multiple variants.

**Request**:
```json
{
  "periodId": "reporting-period-id",
  "variantIds": ["variant-1-id", "variant-2-id"],
  "requestedBy": "user-id"
}
```

**Response**: VariantComparison showing differences

## Implementation Details

### Variant Rule Processing

Rules are applied in the following order:

1. **Section Filtering**:
   - If include-section rules exist, only included sections are kept
   - exclude-section rules then remove specific sections
   - Rules are sorted by Order property before application

2. **Field-Level Filtering**:
   - exclude-field-group rules remove entire field groups
   - Redaction rules are applied to individual fields

3. **Attachment Handling**:
   - exclude-attachments rules remove all evidence from specified sections

### Redaction Types

- **mask**: Replaces field value with "***REDACTED***"
- **remove**: Removes the field entirely from the output
- **replace**: Replaces field value with specified replacement text

### Traceability

All variant operations are logged in the audit trail:
- Variant creation, updates, deletion
- Variant report generation (including which sections/fields were excluded/redacted)
- Variant comparisons

The system maintains complete traceability from variant reports back to source data for internal users through:
- Original report ID in GeneratedReportVariant
- Audit log entries with correlation IDs
- ExcludedSections and RedactedFields lists for transparency

## Usage Examples

### Creating a Bank-Specific Variant

```csharp
var request = new CreateVariantRequest
{
    Name = "Bank Disclosure Report",
    Description = "Report for bank financing with sensitive operational details excluded",
    AudienceType = "bank",
    CreatedBy = "admin-user-id",
    Rules = new List<VariantRule>
    {
        new VariantRule
        {
            RuleType = "exclude-section",
            Target = "internal-metrics-section-id",
            Order = 1
        },
        new VariantRule
        {
            RuleType = "exclude-attachments",
            Target = "employee-data-section-id",
            Order = 2
        }
    },
    RedactionRules = new List<RedactionRule>
    {
        new RedactionRule
        {
            FieldIdentifier = "detailed-cost-breakdown-field-id",
            RedactionType = "replace",
            ReplacementValue = "See summary figures in Section 3",
            Reason = "Detailed cost information is commercially sensitive"
        }
    }
};
```

### Generating a Variant Report

```csharp
var generateRequest = new GenerateVariantRequest
{
    PeriodId = "2024-annual-report",
    VariantId = "bank-disclosure-variant-id",
    GeneratedBy = "report-owner-id",
    GenerationNote = "Generated for XYZ Bank financing application"
};

var (isValid, errorMessage, variantReport) = store.GenerateReportVariant(generateRequest);
```

### Comparing Variants

```csharp
var compareRequest = new CompareVariantsRequest
{
    PeriodId = "2024-annual-report",
    VariantIds = new List<string> 
    { 
        "management-variant-id", 
        "bank-variant-id", 
        "client-variant-id" 
    },
    RequestedBy = "admin-user-id"
};

var (isValid, errorMessage, comparison) = store.CompareVariants(compareRequest);

// Review differences
foreach (var diff in comparison.SectionDifferences)
{
    Console.WriteLine($"Section: {diff.SectionName}");
    Console.WriteLine($"  Included in: {string.Join(", ", diff.IncludedInVariants)}");
    Console.WriteLine($"  Excluded from: {string.Join(", ", diff.ExcludedFromVariants)}");
}
```

## Testing

Comprehensive test coverage includes:

1. **Variant CRUD Operations**:
   - Creating variants with validation
   - Updating variant configurations
   - Deleting variants
   - Preventing duplicate names
   - Handling inactive variants

2. **Section Filtering**:
   - Excluding specific sections
   - Including only specified sections
   - Mixed include/exclude rules

3. **Redaction**:
   - Masking sensitive fields
   - Removing fields
   - Replacing field values

4. **Comparison**:
   - Two-variant comparison
   - Multi-variant comparison
   - Validation of comparison inputs

All 12 tests pass successfully.

## Security Considerations

- **Access Control**: Frontend should implement role-based access to limit who can create/modify variants
- **Audit Trail**: All variant operations are logged for compliance
- **Data Integrity**: Original reports remain unchanged; variants are generated on-demand
- **Redaction Transparency**: RedactedFields list allows auditors to verify what was redacted
- **Inactive Variants**: Cannot be used for generation until reactivated

## Future Enhancements

1. **Rule Templates**: Pre-configured rule sets for common audience types
2. **Field-Level Permissions**: Integration with user roles for automatic field filtering
3. **Conditional Rules**: More complex rule conditions based on data values
4. **Variant Scheduling**: Automated variant generation on schedule
5. **Export Formats**: Generate variants in multiple formats (PDF, DOCX, etc.)
6. **Watermarking**: Add audience-specific watermarks to generated variants
7. **Distribution Tracking**: Log when and to whom variants are distributed

## Related Documentation

- `architecture.md` - Overall system architecture
- `AUDIT_LOG_IMPLEMENTATION.md` - Audit logging details
- `REPORT_PREVIEW_IMPLEMENTATION.md` - Report preview functionality
- `DATA_PROVENANCE_IMPLEMENTATION.md` - Data traceability

## API Reference

Full OpenAPI/Swagger documentation is available at `/swagger` when running the application.
