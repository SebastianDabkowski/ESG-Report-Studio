# HR System Integration Configuration Guide

## Overview

The HR System Integration feature enables ESG Report Studio to connect with external HR systems to automatically import employee data, organizational structure, and other HR metrics. This guide covers configuration, usage, and best practices for HR integrations.

## Key Features

- ✅ **Test connection** - Validate HR system connectivity and authentication
- ✅ **Manual and scheduled sync** - Trigger data imports on-demand or via scheduled jobs
- ✅ **Field mapping and transformations** - Map HR system fields to ESG data points with transformations
- ✅ **Rejection handling** - Records that can't be mapped are rejected with clear reasons
- ✅ **Data protection** - Approved data is never overwritten by automated sync
- ✅ **Audit trail** - Complete history of all sync operations and rejections

## Prerequisites

1. An HR system with API access
2. API credentials (OAuth2, API Key, or Basic Auth)
3. Credentials stored in a secret store (Azure Key Vault, AWS Secrets Manager, etc.)
4. Understanding of your HR system's data structure and available endpoints

## Configuration Steps

### 1. Create an HR Connector

Use the Connectors API to create a new HR connector:

**Endpoint**: `POST /api/v1/connectors`

**Request Body**:
```json
{
  "name": "Company HR System",
  "connectorType": "HR",
  "endpointBaseUrl": "https://api.hr-system.example.com",
  "authenticationType": "OAuth2",
  "authenticationSecretRef": "SecretStore:HR-System-ClientId",
  "capabilities": "pull",
  "rateLimitPerMinute": 60,
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true,
  "description": "Integration with corporate HR system for headcount and training data"
}
```

**Response**:
```json
{
  "id": 1,
  "name": "Company HR System",
  "connectorType": "HR",
  "status": "Disabled",
  "endpointBaseUrl": "https://api.hr-system.example.com",
  "authenticationType": "OAuth2",
  "authenticationSecretRef": "SecretStore:HR-System-ClientId",
  "capabilities": "pull",
  "rateLimitPerMinute": 60,
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true,
  "mappingConfiguration": "{}",
  "description": "Integration with corporate HR system for headcount and training data",
  "createdAt": "2026-01-31T04:00:00Z",
  "createdBy": "admin"
}
```

### 2. Configure Field Mappings

Update the connector with field mapping configuration:

**Endpoint**: `PUT /api/v1/connectors/1`

**Request Body**:
```json
{
  "name": "Company HR System",
  "endpointBaseUrl": "https://api.hr-system.example.com",
  "authenticationType": "OAuth2",
  "authenticationSecretRef": "SecretStore:HR-System-ClientId",
  "capabilities": "pull",
  "rateLimitPerMinute": 60,
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true,
  "mappingConfiguration": "{\"mappings\":[{\"externalField\":\"employee_count\",\"internalField\":\"totalEmployees\",\"transform\":\"direct\",\"required\":true},{\"externalField\":\"weekly_hours\",\"internalField\":\"fullTimeEquivalent\",\"transform\":\"fte\",\"transformParams\":{\"standardHours\":\"40\"}},{\"externalField\":\"training_hours_array\",\"internalField\":\"totalTrainingHours\",\"transform\":\"sum\"},{\"externalField\":\"department_code\",\"internalField\":\"departmentName\",\"transform\":\"lookup\",\"transformParams\":{\"table\":\"{\\\"ENG\\\":\\\"Engineering\\\",\\\"HR\\\":\\\"Human Resources\\\",\\\"FIN\\\":\\\"Finance\\\"}\"}}]}"
}
```

**Mapping Configuration Format**:
```json
{
  "mappings": [
    {
      "externalField": "employee_count",
      "internalField": "totalEmployees",
      "transform": "direct",
      "required": true
    },
    {
      "externalField": "weekly_hours",
      "internalField": "fullTimeEquivalent",
      "transform": "fte",
      "transformParams": {
        "standardHours": "40"
      }
    },
    {
      "externalField": "training_hours_array",
      "internalField": "totalTrainingHours",
      "transform": "sum"
    },
    {
      "externalField": "department_code",
      "internalField": "departmentName",
      "transform": "lookup",
      "transformParams": {
        "table": "{\"ENG\":\"Engineering\",\"HR\":\"Human Resources\",\"FIN\":\"Finance\"}"
      }
    }
  ]
}
```

### 3. Test the Connection

Before enabling the connector, test that authentication and connectivity work:

**Endpoint**: `POST /api/v1/hr/connectors/1/test-connection`

**Success Response**:
```json
{
  "success": true,
  "message": "Successfully connected to HR system and validated authentication",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "durationMs": 234
}
```

**Failure Response**:
```json
{
  "success": false,
  "message": "Connection test failed: Unauthorized",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "errorDetails": "System.Net.Http.HttpRequestException: Response status code does not indicate success: 401 (Unauthorized)..."
}
```

### 4. Enable the Connector

Once the test connection succeeds, enable the connector:

**Endpoint**: `POST /api/v1/connectors/1/enable`

**Response**:
```json
{
  "id": 1,
  "status": "Enabled",
  "updatedAt": "2026-01-31T04:05:00Z",
  "updatedBy": "admin"
}
```

## Usage

### Manual Sync

Trigger a manual data sync:

**Endpoint**: `POST /api/v1/hr/sync/1`

**Response**:
```json
{
  "connectorId": 1,
  "correlationId": "660e8400-e29b-41d4-a716-446655440001",
  "success": true,
  "message": "Sync completed. Imported: 150, Updated: 50, Rejected: 5",
  "importedCount": 150,
  "updatedCount": 50,
  "rejectedCount": 5,
  "failedCount": 0,
  "startedAt": "2026-01-31T05:00:00Z",
  "completedAt": "2026-01-31T05:02:30Z"
}
```

### Scheduled Sync

For scheduled sync, use a background job scheduler (e.g., Hangfire, Quartz.NET) to call the sync endpoint periodically:

```csharp
// Example: Daily sync at 2 AM
RecurringJob.AddOrUpdate(
    "hr-sync-daily",
    () => hrSyncService.ExecuteSyncAsync(connectorId: 1, initiatedBy: "scheduler", isScheduled: true),
    Cron.Daily(2));
```

### View Sync History

Get a history of all sync operations:

**Endpoint**: `GET /api/v1/hr/sync-history/1?limit=100`

**Response**:
```json
[
  {
    "id": 123,
    "connectorId": 1,
    "correlationId": "660e8400-e29b-41d4-a716-446655440001",
    "status": "Success",
    "externalId": "EMP001",
    "rejectionReason": null,
    "overwroteApprovedData": false,
    "syncedAt": "2026-01-31T05:00:00Z",
    "initiatedBy": "admin",
    "hrEntityId": 456
  },
  {
    "id": 124,
    "connectorId": 1,
    "correlationId": "660e8400-e29b-41d4-a716-446655440001",
    "status": "Rejected",
    "externalId": "EMP002",
    "rejectionReason": "Required field 'employee_count' is missing",
    "overwroteApprovedData": false,
    "syncedAt": "2026-01-31T05:00:01Z",
    "initiatedBy": "admin",
    "hrEntityId": null
  }
]
```

### View Rejected Records

Get records that were rejected during sync:

**Endpoint**: `GET /api/v1/hr/rejected-records/1?limit=100`

**Response**:
```json
[
  {
    "id": 124,
    "connectorId": 1,
    "correlationId": "660e8400-e29b-41d4-a716-446655440001",
    "status": "Rejected",
    "externalId": "EMP002",
    "rawData": "{\"external_id\":\"EMP002\",\"name\":\"John Doe\",\"weekly_hours\":35}",
    "rejectionReason": "Required field 'employee_count' is missing",
    "overwroteApprovedData": false,
    "syncedAt": "2026-01-31T05:00:01Z",
    "initiatedBy": "admin",
    "hrEntityId": null
  }
]
```

## Field Transformation Types

### 1. Direct

Copy the value as-is from the external field to the internal field.

```json
{
  "externalField": "employee_count",
  "internalField": "totalEmployees",
  "transform": "direct"
}
```

### 2. FTE (Full-Time Equivalent)

Normalize hours to FTE. Useful for converting weekly hours to FTE values.

```json
{
  "externalField": "weekly_hours",
  "internalField": "fullTimeEquivalent",
  "transform": "fte",
  "transformParams": {
    "standardHours": "40"
  }
}
```

**Example**: 
- Input: 35 weekly hours
- Standard: 40 hours
- Output: 0.875 FTE

### 3. Sum

Sum all values in an array.

```json
{
  "externalField": "training_hours_array",
  "internalField": "totalTrainingHours",
  "transform": "sum"
}
```

**Example**:
- Input: [10, 15, 20, 5]
- Output: 50

### 4. Average

Calculate the average of values in an array.

```json
{
  "externalField": "satisfaction_scores",
  "internalField": "averageSatisfaction",
  "transform": "average"
}
```

**Example**:
- Input: [4.5, 4.0, 4.8, 4.2]
- Output: 4.375

### 5. Lookup

Map values using a lookup table.

```json
{
  "externalField": "department_code",
  "internalField": "departmentName",
  "transform": "lookup",
  "transformParams": {
    "table": "{\"ENG\":\"Engineering\",\"HR\":\"Human Resources\",\"FIN\":\"Finance\"}"
  }
}
```

**Example**:
- Input: "ENG"
- Output: "Engineering"

## Rejection Handling

Records are rejected when:

1. **Required field is missing**: A field marked as `required: true` in the mapping is not present in the HR data
2. **Mapping configuration is invalid**: The mapping configuration JSON is malformed or missing
3. **Data cannot be overwritten**: The record already exists and is marked as approved

### Rejection Example

When a record is rejected, it:
- Creates a `HRSyncRecord` with status `Rejected`
- Includes a clear `rejectionReason`
- Does NOT create or update an `HREntity`
- Does NOT overwrite any approved data
- Can be reviewed via the rejected records endpoint

## Data Protection

### Approved Data Protection

When an HR entity is marked as `IsApproved = true`, it cannot be overwritten by automated sync. Any sync attempt will result in rejection with the reason:

```
"Cannot overwrite approved data. Manual review required."
```

This ensures that:
- Manually reviewed and approved data is preserved
- Automated sync cannot accidentally overwrite corrections
- Data quality is maintained

## Best Practices

### 1. Test Connection First

Always test the connection before enabling a connector:
```bash
POST /api/v1/hr/connectors/1/test-connection
# Verify success
POST /api/v1/connectors/1/enable
```

### 2. Use Scheduled Sync During Off-Peak Hours

Schedule HR sync during off-peak hours to minimize impact:
- Early morning (2-4 AM)
- After business hours
- Avoid during reporting periods

### 3. Monitor Rejected Records

Regularly review rejected records to identify:
- Missing fields in HR system
- Mapping configuration errors
- Data quality issues

```bash
GET /api/v1/hr/rejected-records/1?limit=50
```

### 4. Set Appropriate Rate Limits

Configure rate limits to avoid overwhelming the HR system:
```json
{
  "rateLimitPerMinute": 60
}
```

### 5. Use Retry with Exponential Backoff

Enable exponential backoff for transient failures:
```json
{
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true
}
```

### 6. Mark Critical Data as Required

Ensure critical fields are marked as required in the mapping:
```json
{
  "externalField": "employee_count",
  "internalField": "totalEmployees",
  "transform": "direct",
  "required": true
}
```

## Troubleshooting

### Connection Test Fails

**Symptom**: Test connection returns failure

**Possible Causes**:
1. Invalid credentials in secret store
2. Network connectivity issues
3. HR system is down
4. Incorrect endpoint URL

**Solutions**:
1. Verify credentials in secret store
2. Test endpoint availability with curl/Postman
3. Check HR system status page
4. Verify `endpointBaseUrl` is correct

### All Records Rejected

**Symptom**: Sync completes but all records are rejected

**Possible Causes**:
1. Mapping configuration is incorrect
2. HR system changed data format
3. Required fields are missing

**Solutions**:
1. Review rejected records for common rejection reasons
2. Verify mapping configuration matches current HR data structure
3. Update mapping configuration if HR system changed
4. Test with a small sample before full sync

### Approved Data Not Updating

**Symptom**: Some records not updating even though sync succeeds

**Expected Behavior**: Approved data is protected from automated updates

**Resolution**: 
- This is by design to protect manually reviewed data
- Unapprove records if they should be updated by sync
- Review rejection reason to confirm it's due to approved status

## API Reference

### HR Sync Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/hr/connectors/{id}/test-connection` | POST | Test HR connector authentication |
| `/api/v1/hr/sync/{connectorId}` | POST | Manually trigger HR data sync |
| `/api/v1/hr/sync-history/{connectorId}` | GET | Get sync operation history |
| `/api/v1/hr/rejected-records/{connectorId}` | GET | Get rejected records |

### Connector Endpoints

See [Integration Connector Framework Guide](INTEGRATION_CONNECTOR_FRAMEWORK_GUIDE.md) for connector management endpoints.

## Examples

### Example 1: Simple Employee Count Sync

**Mapping Configuration**:
```json
{
  "mappings": [
    {
      "externalField": "total_employees",
      "internalField": "employeeCount",
      "transform": "direct",
      "required": true
    }
  ]
}
```

**HR System Data**:
```json
{
  "external_id": "ORG001",
  "total_employees": 250
}
```

**Result**: Creates HREntity with `employeeCount: 250`

### Example 2: FTE Calculation with Training Hours

**Mapping Configuration**:
```json
{
  "mappings": [
    {
      "externalField": "weekly_hours",
      "internalField": "fte",
      "transform": "fte",
      "transformParams": {
        "standardHours": "40"
      }
    },
    {
      "externalField": "training_sessions",
      "internalField": "totalTrainingHours",
      "transform": "sum"
    }
  ]
}
```

**HR System Data**:
```json
{
  "external_id": "EMP001",
  "weekly_hours": 35,
  "training_sessions": [8, 4, 6, 10]
}
```

**Result**: Creates HREntity with `fte: 0.875` and `totalTrainingHours: 28`

## Security Considerations

1. **Never store credentials in database**: Always use secret store references
2. **Use HTTPS only**: Ensure all HR system connections use HTTPS
3. **Limit API access**: Configure rate limits and authentication
4. **Audit all sync operations**: Review integration logs regularly
5. **Protect approved data**: Mark sensitive data as approved to prevent overwrites

## Next Steps

1. Configure your first HR connector
2. Test the connection
3. Set up field mappings for your HR system
4. Enable the connector
5. Perform a manual sync to validate
6. Schedule automated sync jobs
7. Monitor rejected records and adjust mappings as needed
