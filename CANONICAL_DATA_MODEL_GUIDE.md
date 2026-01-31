# Canonical Data Model for Integrations - Implementation Guide

## Overview

The Canonical Data Model provides a unified, version-controlled internal data structure for ESG Report Studio integrations. This model enables multiple external systems (HR platforms, finance systems, environmental monitoring tools) to map their data into a single, consistent set of canonical entities and attributes.

## Key Features

✅ **Unified Data Model** - Multiple external systems map into the same canonical entities  
✅ **Schema Versioning** - Explicit version control with backward compatibility tracking  
✅ **Provenance Tracking** - Full auditability with source, version, and import metadata  
✅ **Vendor Extensions** - Vendor-specific fields stored separately from core domain  
✅ **Backward Compatibility** - Version migration rules prevent breaking existing connectors  
✅ **Flexible Mapping** - Configurable field transformations (direct, sum, average, lookup, FTE)  

## Architecture

### Core Entities

#### 1. **CanonicalEntity**
Represents a unified data record imported from external systems.

**Key Fields:**
- `EntityType` - Type of entity (Employee, Spend, Revenue, etc.)
- `SchemaVersion` - Version of the canonical schema
- `Data` - JSON containing canonical attributes
- **Provenance Fields:**
  - `SourceSystem` - Name of the source system
  - `SourceVersion` - Version of the source API/export
  - `ImportedAt` - When data was imported
  - `ImportedByJobId` - Batch job identifier
- `VendorExtensions` - JSON for vendor-specific fields (kept separate from core domain)
- `IsApproved` - Whether entity is approved for ESG reporting use

#### 2. **CanonicalEntityVersion**
Defines a version of the canonical schema for an entity type.

**Key Fields:**
- `EntityType` - Type of entity this version applies to
- `Version` - Version number (incremental)
- `SchemaDefinition` - JSON schema definition
- `IsActive` - Whether this version is active for new mappings
- `IsDeprecated` - Whether this version is deprecated
- `BackwardCompatibleWithVersion` - Minimum version for compatibility
- `MigrationRules` - JSON rules for migrating from older versions

#### 3. **CanonicalAttribute**
Defines a standard attribute within the canonical model.

**Key Fields:**
- `EntityType` - Type of entity
- `SchemaVersion` - Version where attribute was introduced
- `AttributeName` - Name of the attribute
- `DataType` - Data type (string, number, boolean, date, array, object)
- `IsRequired` - Whether attribute is required
- `IsDeprecated` - Whether attribute is deprecated
- `ReplacedBy` - Replacement attribute if deprecated

#### 4. **CanonicalMapping**
Maps external system fields to canonical attributes.

**Key Fields:**
- `ConnectorId` - Reference to connector
- `TargetEntityType` - Canonical entity type
- `TargetSchemaVersion` - Target schema version
- `ExternalField` - Field name in external system
- `CanonicalAttribute` - Target canonical attribute
- `TransformationType` - Transformation to apply
- `TransformationParams` - Parameters for transformation
- `IsRequired` - Whether mapping is required
- `Priority` - Order for applying mappings

### Supported Entity Types

#### HR Entities
- **Employee** - Employee records
- **Department** - Department/division structures
- **OrganizationalUnit** - Organizational hierarchy nodes
- **Position** - Job positions/titles
- **TrainingRecord** - Employee training activities

#### Finance Entities
- **Spend** - Expenditure records
- **Revenue** - Income records
- **CapitalExpenditure** - CapEx items
- **OperationalExpenditure** - OpEx items
- **Supplier** - Supplier/vendor data
- **Invoice** - Invoice records

#### Environmental Entities (Future)
- **EnergyConsumption** - Energy usage data
- **WaterUsage** - Water consumption data
- **WasteGeneration** - Waste production records
- **EmissionsRecord** - Emissions data

#### Social Entities (Future)
- **SafetyIncident** - Safety and accident records
- **CommunityEngagement** - Community programs

#### Governance Entities (Future)
- **ComplianceRecord** - Compliance documentation
- **PolicyDocument** - Policy records

## Usage Guide

### 1. Define a Canonical Schema Version

Create a schema version for an entity type:

**API Endpoint:** `POST /api/v1/canonical/versions`

**Request:**
```json
{
  "entityType": "Employee",
  "version": 1,
  "schemaDefinition": "{\"type\":\"object\",\"properties\":{\"totalEmployees\":{\"type\":\"number\"},\"fullTimeEquivalent\":{\"type\":\"number\"},\"departmentName\":{\"type\":\"string\"}}}",
  "description": "Initial Employee canonical schema - v1.0",
  "backwardCompatibleWithVersion": null
}
```

**Response:**
```json
{
  "id": 1,
  "entityType": "Employee",
  "version": 1,
  "schemaDefinition": "...",
  "description": "Initial Employee canonical schema - v1.0",
  "isActive": true,
  "isDeprecated": false,
  "backwardCompatibleWithVersion": null,
  "createdAt": "2026-01-31T05:00:00Z",
  "createdBy": "admin"
}
```

### 2. Create Field Mappings

Configure how external fields map to canonical attributes:

**API Endpoint:** `POST /api/v1/canonical/mappings`

**Example 1: Direct Mapping**
```json
{
  "connectorId": 1,
  "targetEntityType": "Employee",
  "targetSchemaVersion": 1,
  "externalField": "employee_count",
  "canonicalAttribute": "totalEmployees",
  "transformationType": "direct",
  "isRequired": true,
  "priority": 0
}
```

**Example 2: FTE Transformation**
```json
{
  "connectorId": 1,
  "targetEntityType": "Employee",
  "targetSchemaVersion": 1,
  "externalField": "weekly_hours",
  "canonicalAttribute": "fullTimeEquivalent",
  "transformationType": "fte",
  "transformationParams": "{\"standardHours\":\"40\"}",
  "isRequired": false,
  "priority": 1
}
```

**Example 3: Lookup Transformation**
```json
{
  "connectorId": 1,
  "targetEntityType": "Employee",
  "targetSchemaVersion": 1,
  "externalField": "dept_code",
  "canonicalAttribute": "departmentName",
  "transformationType": "lookup",
  "transformationParams": "{\"ENG\":\"Engineering\",\"HR\":\"Human Resources\",\"FIN\":\"Finance\"}",
  "isRequired": false,
  "priority": 2
}
```

### 3. Integration with HR/Finance Sync

The canonical model integrates with existing HR and Finance sync services. Updated HR and Finance entities now include a `CanonicalEntityId` reference.

**Updated Flow:**
1. External data is fetched by HR/Finance sync service
2. Data is mapped to canonical entity using configured mappings
3. Canonical entity is created/updated
4. HR/Finance entity references the canonical entity ID
5. Both vendor-specific and canonical data are preserved

## Transformation Types

### 1. Direct
Copy value as-is.

```json
{
  "transformationType": "direct"
}
```

### 2. Sum
Sum all values in an array.

```json
{
  "transformationType": "sum"
}
```

**Example:**
- Input: `[10, 15, 20, 5]`
- Output: `50`

### 3. Average
Calculate average of values in an array.

```json
{
  "transformationType": "average"
}
```

**Example:**
- Input: `[4.5, 4.0, 4.8, 4.2]`
- Output: `4.375`

### 4. Lookup
Map values using a lookup table.

```json
{
  "transformationType": "lookup",
  "transformationParams": "{\"ENG\":\"Engineering\",\"HR\":\"Human Resources\"}"
}
```

**Example:**
- Input: `"ENG"`
- Output: `"Engineering"`

### 5. FTE (Full-Time Equivalent)
Convert hours to FTE.

```json
{
  "transformationType": "fte",
  "transformationParams": "{\"standardHours\":\"40\"}"
}
```

**Example:**
- Input: `35` (weekly hours)
- Standard: `40` hours
- Output: `0.875` FTE

### 6. Custom
Placeholder for future custom transformations.

```json
{
  "transformationType": "custom",
  "transformationParams": "{\"script\":\"...\"}"
}
```

## Schema Versioning

### Creating a New Version

When adding new attributes or changing schemas:

**API Endpoint:** `POST /api/v1/canonical/versions`

```json
{
  "entityType": "Employee",
  "version": 2,
  "schemaDefinition": "{\"type\":\"object\",\"properties\":{\"totalEmployees\":{\"type\":\"number\"},\"fullTimeEquivalent\":{\"type\":\"number\"},\"departmentName\":{\"type\":\"string\"},\"avgTrainingHours\":{\"type\":\"number\"}}}",
  "description": "Employee v2 - Added avgTrainingHours",
  "backwardCompatibleWithVersion": 1,
  "migrationRules": "[{\"action\":\"add\",\"attribute\":\"avgTrainingHours\",\"defaultValue\":0}]"
}
```

### Backward Compatibility Rules

**Backward Compatible Changes:**
- Adding optional attributes
- Deprecating attributes (with replacement)
- Relaxing validation constraints

**Breaking Changes:**
- Removing required attributes
- Changing attribute data types
- Adding required attributes without defaults

### Checking Compatibility

**API Endpoint:** `GET /api/v1/canonical/versions/{entityType}/compatibility?currentVersion=1&newVersion=2`

**Response:**
```json
{
  "entityType": "Employee",
  "currentVersion": 1,
  "newVersion": 2,
  "isCompatible": true
}
```

## Provenance Tracking

Every canonical entity includes full provenance metadata:

```json
{
  "id": 42,
  "entityType": "Employee",
  "schemaVersion": 1,
  "externalId": "EMP-001",
  "data": "{\"totalEmployees\":100,\"fullTimeEquivalent\":87.5}",
  "sourceSystem": "Workday HR",
  "sourceVersion": "v2023.1",
  "importedAt": "2026-01-31T05:00:00Z",
  "importedByJobId": "JOB-20260131050000-abc123",
  "vendorExtensions": "{\"workday_org_id\":\"ORG-456\",\"workday_cost_center\":\"CC-789\"}",
  "isApproved": false
}
```

**Provenance Fields:**
- `sourceSystem` - Name of the source system (e.g., "Workday HR", "SAP Finance")
- `sourceVersion` - Version of source API/export (e.g., "v2023.1", "API v2")
- `importedAt` - Timestamp when data was imported
- `importedByJobId` - Unique job identifier for batch tracking
- `vendorExtensions` - Vendor-specific fields not in canonical model

## Vendor Extensions

Vendor-specific fields that don't fit the canonical model are stored in `VendorExtensions`:

**Example:**
```json
{
  "data": "{\"totalEmployees\":100}",
  "vendorExtensions": "{\"workday_unique_field\":\"value\",\"sap_custom_attribute\":123}"
}
```

**Benefits:**
- Keeps canonical model clean
- Preserves vendor-specific data
- Enables vendor-specific workflows
- Prevents schema pollution

## Integration with Existing Entities

### HREntity
Now includes optional `CanonicalEntityId`:

```csharp
public class HREntity
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string ExternalId { get; set; }
    public string EntityType { get; set; }
    public string Data { get; set; } // Vendor-specific data
    public string MappedData { get; set; } // ESG-mapped data
    public int? CanonicalEntityId { get; set; } // Reference to canonical entity
    // ... other fields
}
```

### FinanceEntity
Now includes optional `CanonicalEntityId`:

```csharp
public class FinanceEntity
{
    public int Id { get; set; }
    public int ConnectorId { get; set; }
    public string ExternalId { get; set; }
    public string EntityType { get; set; }
    public string Data { get; set; } // Vendor-specific data
    public string MappedData { get; set; } // ESG-mapped data
    public string SourceSystem { get; set; }
    public string ImportJobId { get; set; }
    public int? CanonicalEntityId { get; set; } // Reference to canonical entity
    // ... other fields
}
```

## Database Schema

### CanonicalEntities Table
- `Id` (PK)
- `EntityType` (indexed)
- `SchemaVersion` (indexed, FK)
- `ExternalId`
- `Data` (nvarchar(max))
- `SourceSystem` (indexed)
- `SourceVersion`
- `ImportedAt`
- `ImportedByJobId` (indexed)
- `VendorExtensions` (nvarchar(max))
- `IsApproved` (indexed)
- `UpdatedAt`
- `UpdatedBy`

**Indexes:**
- `(EntityType)`
- `(SourceSystem, ExternalId)`
- `(ImportedByJobId)`
- `(SchemaVersion)`
- `(IsApproved)`

### CanonicalEntityVersions Table
- `Id` (PK)
- `EntityType`
- `Version`
- `SchemaDefinition` (nvarchar(max))
- `Description`
- `IsActive` (indexed)
- `IsDeprecated` (indexed)
- `BackwardCompatibleWithVersion`
- `MigrationRules` (nvarchar(max))
- `CreatedAt`
- `CreatedBy`
- `DeprecatedAt`

**Unique Index:**
- `(EntityType, Version)`

### CanonicalAttributes Table
- `Id` (PK)
- `EntityType`
- `SchemaVersion`
- `AttributeName`
- `DataType`
- `IsRequired` (indexed)
- `Description`
- `ExampleValues`
- `ValidationRules` (nvarchar(max))
- `DefaultValue`
- `IsDeprecated` (indexed)
- `DeprecatedInVersion`
- `ReplacedBy`
- `CreatedAt`
- `UpdatedAt`

**Unique Index:**
- `(EntityType, SchemaVersion, AttributeName)`

### CanonicalMappings Table
- `Id` (PK)
- `ConnectorId` (indexed, FK)
- `TargetEntityType` (indexed)
- `TargetSchemaVersion` (indexed)
- `ExternalField`
- `CanonicalAttribute`
- `TransformationType`
- `TransformationParams` (nvarchar(max))
- `IsRequired` (indexed)
- `DefaultValue`
- `Priority`
- `IsActive` (indexed)
- `Notes`
- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`

**Indexes:**
- `(ConnectorId)`
- `(ConnectorId, TargetEntityType)`
- `(ConnectorId, TargetEntityType, TargetSchemaVersion)`
- `(IsActive)`
- `(IsRequired)`

## Example: End-to-End Workflow

### Scenario: Mapping Workday HR Data to Canonical Employee Model

#### Step 1: Create Canonical Schema Version
```bash
POST /api/v1/canonical/versions
{
  "entityType": "Employee",
  "version": 1,
  "schemaDefinition": "...",
  "description": "Employee v1 - Initial schema"
}
```

#### Step 2: Configure Mappings
```bash
POST /api/v1/canonical/mappings
{
  "connectorId": 1,
  "targetEntityType": "Employee",
  "targetSchemaVersion": 1,
  "externalField": "headcount",
  "canonicalAttribute": "totalEmployees",
  "transformationType": "direct",
  "isRequired": true
}

POST /api/v1/canonical/mappings
{
  "connectorId": 1,
  "targetEntityType": "Employee",
  "targetSchemaVersion": 1,
  "externalField": "weekly_hours",
  "canonicalAttribute": "fullTimeEquivalent",
  "transformationType": "fte",
  "transformationParams": "{\"standardHours\":\"40\"}"
}
```

#### Step 3: Import Data via HR Sync

When HR sync runs:
1. Fetches data from Workday: `{"headcount": 100, "weekly_hours": 38, "workday_org_id": "ORG-456"}`
2. Maps to canonical using configured mappings
3. Creates canonical entity:
   ```json
   {
     "entityType": "Employee",
     "schemaVersion": 1,
     "data": "{\"totalEmployees\":100,\"fullTimeEquivalent\":0.95}",
     "sourceSystem": "Workday HR",
     "importedByJobId": "JOB-20260131050000-abc123",
     "vendorExtensions": "{\"workday_org_id\":\"ORG-456\"}"
   }
   ```
4. Creates HREntity with reference to canonical entity

#### Step 4: Query Unified Data

All systems mapping to `Employee` canonical entity can now be queried uniformly:
```sql
SELECT * FROM CanonicalEntities WHERE EntityType = 'Employee' AND SchemaVersion = 1
```

## Best Practices

### Schema Design
1. **Start with required fields only** - Add optional fields in later versions
2. **Use semantic naming** - Clear, business-friendly attribute names
3. **Document thoroughly** - Include descriptions and examples for all attributes
4. **Plan for evolution** - Design schemas with extensibility in mind

### Versioning
1. **Increment versions for changes** - Even minor changes should get new versions
2. **Mark compatibility** - Always specify backward compatibility rules
3. **Deprecate gracefully** - Provide migration path when deprecating
4. **Keep old versions active** - Support multiple versions during transition

### Mapping Configuration
1. **Validate early** - Test mappings with sample data before enabling connector
2. **Use priorities** - Order mappings when multiple map to same attribute
3. **Set required carefully** - Only mark truly essential fields as required
4. **Document transformations** - Add notes explaining complex transformations

### Provenance
1. **Always capture source** - Include source system name and version
2. **Use batch IDs** - Track imports with unique job identifiers
3. **Preserve vendor data** - Store vendor-specific fields in extensions
4. **Audit regularly** - Review provenance data for compliance

## Security Considerations

1. **Credentials** - Never store credentials in mappings; use secret store references
2. **Access Control** - Restrict schema version and mapping configuration to admins
3. **Data Validation** - Validate all external data before creating canonical entities
4. **Audit Trail** - Log all schema changes and mapping updates
5. **Encryption** - Use encryption at rest for canonical entity data

## Troubleshooting

### Issue: Mapping Fails with "Required field missing"
**Cause:** External data doesn't contain a required field  
**Solution:** 
- Add default value to mapping configuration
- Mark field as not required if appropriate
- Fix data at source

### Issue: Transformation Returns Unexpected Value
**Cause:** Transformation parameters incorrect  
**Solution:**
- Review transformation type and parameters
- Test with sample data
- Check transformation logic in documentation

### Issue: "No active schema version found"
**Cause:** No active version exists for entity type  
**Solution:**
- Create initial schema version via API
- Ensure `isActive: true` for the version

### Issue: Backward Compatibility Check Fails
**Cause:** New version breaks compatibility with existing connectors  
**Solution:**
- Review breaking changes
- Provide migration rules
- Update connectors to use new version
- Keep old version active during transition

## API Reference

### Schema Versions
- `POST /api/v1/canonical/versions` - Create schema version
- `GET /api/v1/canonical/versions/{entityType}/compatibility` - Check compatibility

### Mappings
- `POST /api/v1/canonical/mappings` - Create mapping
- `GET /api/v1/canonical/mappings/{connectorId}` - Get connector mappings

## Future Enhancements

- **Automated Migration** - Auto-migrate entities between schema versions
- **Mapping Validation** - Dry-run mapping configurations with sample data
- **Schema Registry UI** - Visual schema designer and version manager
- **Conflict Resolution** - Advanced merge strategies for overlapping data
- **Data Quality Metrics** - Track completeness and accuracy of canonical entities
- **Cross-System Lineage** - Trace data from source through canonical to ESG reports

## Conclusion

The Canonical Data Model provides a robust foundation for multi-system integration in ESG Report Studio. By mapping external data into unified canonical entities with full provenance tracking and version control, the system enables scalable, auditable ESG reporting across diverse data sources.
