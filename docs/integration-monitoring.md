# Integration Monitoring and Audit Trail

## Overview

The Integration Monitoring and Audit Trail feature provides comprehensive auditability of integration activities, enabling compliance auditors to trace imported/exported data and changes back to their source and approvals.

## Features

### 1. Job Metadata Tracking

Every integration job (HR sync, finance sync, manual import, etc.) is tracked with:

- **Job Identification**: Unique job ID and correlation ID for end-to-end tracing
- **Execution Details**: Start time, end time, duration, status
- **Processing Metrics**: Total records, success count, failure count, skipped count
- **Error Tracking**: Aggregated error summaries for failed jobs
- **Provenance**: Who initiated the job and when

### 2. Searchable Integration Logs

All integration API calls are logged with:

- **Request/Response Details**: HTTP method, endpoint, status code
- **Retry Information**: Number of retry attempts made
- **Performance Metrics**: Call duration in milliseconds
- **Error Details**: Error messages and stack traces for failures

### 3. Approval History

Track who approved data overrides and when:

- **Override Approvals**: When automated data conflicts with manual entries
- **Conflict Resolution**: How conflicts were resolved (preserved manual, admin override, etc.)
- **Full Audit Trail**: Timestamp, approver, entity details, and reason

### 4. Export for Compliance

Export audit data as CSV for:

- **Regulatory Compliance**: Meet audit requirements
- **Data Analysis**: Analyze integration patterns and performance
- **Long-term Archival**: Keep records per retention policies

## API Endpoints

### Search Integration Jobs

**GET** `/api/v1/integration-monitoring/jobs`

Search and filter integration jobs with pagination.

**Query Parameters:**
- `startDate` (optional): Filter jobs started after this date (ISO 8601)
- `endDate` (optional): Filter jobs started before this date (ISO 8601)
- `status` (optional): Job status - `Queued`, `Running`, `Completed`, `CompletedWithErrors`, `Failed`, `Cancelled`
- `connectorId` (optional): Filter by connector ID
- `jobType` (optional): Filter by job type (e.g., `HRSync`, `FinanceSync`)
- `initiatedBy` (optional): Filter by user or service that initiated the job
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Results per page (default: 50, max: 100)

**Example Request:**
```bash
curl -X GET "http://localhost:5011/api/v1/integration-monitoring/jobs?startDate=2026-01-01T00:00:00Z&status=Completed&page=1&pageSize=20" \
  -H "Content-Type: application/json"
```

**Response:**
```json
{
  "jobs": [
    {
      "id": 1,
      "jobId": "job-abc123",
      "connectorId": 1,
      "connectorName": "HR System Connector",
      "correlationId": "corr-xyz789",
      "jobType": "HRSync",
      "status": "Completed",
      "startedAt": "2026-01-15T10:30:00Z",
      "completedAt": "2026-01-15T10:35:00Z",
      "durationMs": 300000,
      "totalRecords": 150,
      "successCount": 148,
      "failureCount": 2,
      "skippedCount": 0,
      "errorSummary": "2 records failed validation",
      "initiatedBy": "scheduled-job-service",
      "notes": "Daily HR sync"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### Get Job Details

**GET** `/api/v1/integration-monitoring/jobs/{jobId}`

Get detailed information about a specific job including all related logs.

**Example Request:**
```bash
curl -X GET "http://localhost:5011/api/v1/integration-monitoring/jobs/job-abc123" \
  -H "Content-Type: application/json"
```

**Response:**
```json
{
  "job": {
    "id": 1,
    "jobId": "job-abc123",
    "connectorId": 1,
    "connectorName": "HR System Connector",
    "status": "Completed",
    "startedAt": "2026-01-15T10:30:00Z",
    "completedAt": "2026-01-15T10:35:00Z",
    "totalRecords": 150,
    "successCount": 148,
    "failureCount": 2
  },
  "logs": [
    {
      "id": 1,
      "connectorId": 1,
      "correlationId": "corr-xyz789",
      "operationType": "pull",
      "status": "Success",
      "httpMethod": "GET",
      "endpoint": "/api/employees",
      "httpStatusCode": 200,
      "retryAttempts": 0,
      "durationMs": 1250,
      "startedAt": "2026-01-15T10:30:00Z",
      "completedAt": "2026-01-15T10:30:01Z",
      "initiatedBy": "scheduled-job-service"
    }
  ]
}
```

### Get Approval History

**GET** `/api/v1/integration-monitoring/approvals`

View history of who approved data overrides and when.

**Query Parameters:**
- `startDate` (optional): Filter approvals after this date
- `endDate` (optional): Filter approvals before this date
- `connectorId` (optional): Filter by connector ID
- `approvedBy` (optional): Filter by approver username

**Example Request:**
```bash
curl -X GET "http://localhost:5011/api/v1/integration-monitoring/approvals?approvedBy=admin-user" \
  -H "Content-Type: application/json"
```

**Response:**
```json
[
  {
    "timestamp": "2026-01-15T14:25:00Z",
    "approvedBy": "admin-user",
    "action": "Override Approved",
    "entityType": "FinanceEntity",
    "entityId": "123",
    "connectorId": 2,
    "correlationId": "corr-finance-456",
    "conflictResolution": "AdminOverride",
    "details": "Override approved for external ID: INV-2025-001"
  }
]
```

### Get Integration Statistics

**GET** `/api/v1/integration-monitoring/statistics`

Get aggregated statistics for integration activities in a date range.

**Query Parameters:**
- `startDate` (required): Start date for statistics
- `endDate` (required): End date for statistics
- `connectorId` (optional): Filter by connector ID

**Example Request:**
```bash
curl -X GET "http://localhost:5011/api/v1/integration-monitoring/statistics?startDate=2026-01-01T00:00:00Z&endDate=2026-01-31T23:59:59Z" \
  -H "Content-Type: application/json"
```

**Response:**
```json
{
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-01-31T23:59:59Z",
  "totalJobs": 124,
  "completedJobs": 118,
  "failedJobs": 3,
  "jobsWithErrors": 3,
  "totalRecordsProcessed": 15420,
  "totalRecordsSucceeded": 15350,
  "totalRecordsFailed": 70,
  "totalApiCalls": 372,
  "successfulApiCalls": 368,
  "failedApiCalls": 4,
  "averageJobDurationMs": 245000
}
```

### Export Audit Data

**GET** `/api/v1/integration-monitoring/export/audit-csv`

Export integration audit data as CSV for compliance purposes.

**Query Parameters:**
- `startDate` (optional): Start date for export
- `endDate` (optional): End date for export
- `connectorId` (optional): Filter by connector ID

**Example Request:**
```bash
curl -X GET "http://localhost:5011/api/v1/integration-monitoring/export/audit-csv?startDate=2026-01-01T00:00:00Z&endDate=2026-01-31T23:59:59Z" \
  -H "Content-Type: application/json" \
  -o integration-audit.csv
```

**Response:** CSV file with columns:
- JobId
- JobType
- ConnectorId
- Status
- StartedAt
- CompletedAt
- DurationMs
- TotalRecords
- SuccessCount
- FailureCount
- SkippedCount
- InitiatedBy
- ErrorSummary

## Data Provenance

Every imported/exported entity maintains a link to the integration job that produced it through:

1. **CorrelationId**: Links all operations in a single job
2. **JobId**: Unique identifier for the job execution
3. **ImportJobId**: Stored on entities (FinanceEntity, CanonicalEntity) to track their source
4. **SourceSystem**: Identifies the external system that provided the data

## Retention Policy Considerations

When implementing retention policies, consider:

1. **Active Job Data**: Keep recent job metadata (last 90 days) for operational monitoring
2. **Archived Audit Data**: Export and archive older data per compliance requirements
3. **Log Rotation**: Integrate with log management systems for detailed logs
4. **Approval History**: Retain indefinitely or per regulatory requirements

## Security Considerations

1. **Access Control**: Ensure only authorized users (auditors, admins) can access audit data
2. **Data Sanitization**: Integration logs sanitize sensitive data (secrets, PII) from request/response payloads
3. **Correlation IDs**: Used for tracing without exposing sensitive information
4. **Audit Trail Immutability**: Job metadata and logs are append-only; no updates after completion

## Integration with Existing Systems

The monitoring infrastructure integrates with:

1. **Existing Integration Services**: HRSyncService, FinanceSyncService automatically create job metadata
2. **Correlation ID Middleware**: Automatic propagation across all API calls
3. **General Audit Log**: Complements existing application audit logging
4. **Webhook Delivery Logs**: Separate tracking for webhook events

## Example Use Cases

### Use Case 1: Investigate Failed Import

1. Search for failed jobs: `GET /jobs?status=Failed&startDate=2026-01-15`
2. Get job details: `GET /jobs/{jobId}`
3. Review error messages and retry attempts in logs
4. Identify root cause and remediate

### Use Case 2: Audit Override Approvals

1. Get approval history: `GET /approvals?startDate=2026-01-01&endDate=2026-01-31`
2. Filter by approver: `GET /approvals?approvedBy=admin-user`
3. Export for compliance review

### Use Case 3: Performance Analysis

1. Get statistics: `GET /statistics?startDate=2026-01-01&endDate=2026-01-31`
2. Analyze success rates, average durations
3. Identify bottlenecks and optimization opportunities

### Use Case 4: Compliance Export

1. Export all integration activity: `GET /export/audit-csv?startDate=2025-01-01&endDate=2025-12-31`
2. Submit CSV to auditors or archive per retention policy
3. Maintain evidence of data lineage and traceability

## Technical Implementation

### Database Schema

**IntegrationJobMetadata Table:**
- Id (PK)
- JobId (Unique)
- ConnectorId (FK to Connectors)
- CorrelationId
- JobType
- Status (Queued, Running, Completed, CompletedWithErrors, Failed, Cancelled)
- StartedAt
- CompletedAt
- DurationMs
- TotalRecords, SuccessCount, FailureCount, SkippedCount
- ErrorSummary
- InitiatedBy
- Notes

**Indexes:**
- JobId (Unique)
- ConnectorId
- CorrelationId
- Status
- StartedAt
- JobType

### Service Layer

**IntegrationMonitoringService** provides:
- SearchJobsAsync: Filter and paginate jobs
- GetJobDetailsAsync: Get job with related logs
- GetApprovalHistoryAsync: Query approval records
- GetStatisticsAsync: Calculate aggregated metrics
- CreateJobAsync/UpdateJobAsync: Manage job lifecycle

### Repository Layer

Enhanced repositories with search capabilities:
- IIntegrationJobMetadataRepository
- IIntegrationLogRepository (extended)
- IFinanceSyncRecordRepository (extended for approval queries)

## Migration

Database migration `20260131065846_AddIntegrationJobMetadata` adds:
- IntegrationJobMetadata table
- Indexes for efficient querying
- Foreign key relationships

Run migration:
```bash
dotnet ef database update -p Modules/SD.ProjectName.Modules.Integrations -s Application/ARP.ESG_ReportStudio.API -c IntegrationDbContext
```
