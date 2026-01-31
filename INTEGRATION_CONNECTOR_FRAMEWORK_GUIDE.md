# Integration Connector Framework Guide

## Overview

The Integration Connector Framework provides a standardized, secure, and auditable way to integrate ESG Report Studio with external systems such as HR platforms, ERP systems, and utility providers.

## Key Features

- ✅ **Standardized connector contract** for all external integrations
- ✅ **Secure credential management** using secret store references
- ✅ **Automatic retry policies** with exponential backoff
- ✅ **Correlation ID tracking** across all integration calls
- ✅ **Enable/disable controls** for connectors
- ✅ **Comprehensive audit logging** of all integration attempts
- ✅ **Rate limiting** to prevent overwhelming external systems
- ✅ **Field mapping configuration** for data transformation

## Architecture

The framework follows the N-Layer architecture:

```
API Layer (Controllers/DTOs)
    ↓
Application Layer (ConnectorService, IntegrationExecutionService)
    ↓
Domain Layer (Entities, Interfaces)
    ↓
Infrastructure Layer (Repositories, DbContext)
```

## Core Concepts

### Connector

A **Connector** represents a configured integration with an external system. Each connector includes:

- **Name**: User-friendly identifier
- **Type**: Category of external system (e.g., "HR", "ERP", "Utilities")
- **Status**: `Enabled` or `Disabled`
- **Endpoint Base URL**: Root API URL for the external system
- **Authentication**: Type and secret store reference
- **Capabilities**: Supported operations (pull/push/webhook)
- **Retry Policy**: Configuration for handling failures
- **Rate Limits**: Maximum requests per minute
- **Mapping Configuration**: JSON for field transformations

### Integration Log

Every integration call creates an **Integration Log** entry with:

- **Correlation ID**: For end-to-end tracing
- **Status**: Success, Failed, Skipped, or In Progress
- **Performance Data**: Duration, retry attempts
- **Error Details**: If the call failed
- **Request/Response Summaries**: Sanitized payloads

## Usage Guide

### 1. Creating a Connector

**API Endpoint**: `POST /api/v1/connectors`

**Example Request**:
```json
{
  "name": "Company HR System",
  "connectorType": "HR",
  "endpointBaseUrl": "https://api.hr-system.example.com",
  "authenticationType": "OAuth2",
  "authenticationSecretRef": "SecretStore:HR-System-ClientId",
  "capabilities": "pull,push",
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
  "capabilities": "pull,push",
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

**Note**: Connectors are created in `Disabled` status for safety. You must explicitly enable them.

### 2. Enabling a Connector

**API Endpoint**: `POST /api/v1/connectors/{id}/enable`

**Example**:
```bash
POST /api/v1/connectors/1/enable
```

**Response**:
```json
{
  "id": 1,
  "status": "Enabled",
  "updatedAt": "2026-01-31T04:05:00Z",
  "updatedBy": "admin"
}
```

### 3. Disabling a Connector

**API Endpoint**: `POST /api/v1/connectors/{id}/disable`

When a connector is disabled, any integration trigger will result in a `Skipped` status with no outbound calls executed.

### 4. Retrieving Integration Logs

**By Connector**:
```bash
GET /api/v1/connectors/1/logs?limit=50
```

**By Correlation ID**:
```bash
GET /api/v1/connectors/logs/by-correlation/corr-12345
```

**Example Response**:
```json
[
  {
    "id": 100,
    "connectorId": 1,
    "connectorName": "Company HR System",
    "correlationId": "corr-12345",
    "operationType": "pull",
    "status": "Success",
    "httpMethod": "GET",
    "endpoint": "/employees",
    "httpStatusCode": 200,
    "retryAttempts": 0,
    "durationMs": 1234,
    "startedAt": "2026-01-31T04:10:00Z",
    "completedAt": "2026-01-31T04:10:01Z",
    "initiatedBy": "scheduler"
  }
]
```

## Security Best Practices

### Secret Store Pattern

**Never store raw credentials in the database**. Instead, use a reference to a secret store:

```json
{
  "authenticationSecretRef": "SecretStore:HR-System-ApiKey"
}
```

The integration service will resolve this reference by:
1. Parsing the prefix (`SecretStore:`)
2. Looking up the key (`HR-System-ApiKey`) in the configured secret store (e.g., Azure Key Vault)
3. Using the retrieved secret for authentication

### Supported Secret Stores

- Azure Key Vault (recommended for Azure deployments)
- AWS Secrets Manager (for AWS deployments)
- HashiCorp Vault
- Environment variables (development only)

### Secret Rotation

Because credentials are referenced, not stored, you can rotate secrets in the secret store without updating connector configurations.

## Retry Policy

### How It Works

When an integration call fails:

1. **First Attempt**: Execute the call
2. **Failure**: Log error and check retry policy
3. **Delay**: Wait for configured delay (with exponential backoff if enabled)
4. **Retry**: Attempt again (up to `maxRetryAttempts`)
5. **Exhausted**: Mark as `Failed` after all retries exhausted

### Exponential Backoff

When `useExponentialBackoff: true`:
- Attempt 1: Immediate
- Attempt 2: Wait 5 seconds
- Attempt 3: Wait 10 seconds (5 × 2¹)
- Attempt 4: Wait 20 seconds (5 × 2²)

When `useExponentialBackoff: false`:
- All retry delays are fixed at `retryDelaySeconds`

### Example Configuration

```json
{
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true
}
```

## Correlation ID Tracking

### Automatic Propagation

The `CorrelationIdMiddleware` automatically:
1. Extracts correlation ID from `X-Correlation-ID` header (if present)
2. Generates a new GUID if not provided
3. Stores in `HttpContext.Items["CorrelationId"]`
4. Adds to response headers
5. Includes in all log entries

### End-to-End Tracing

Use correlation IDs to trace requests across:
- API endpoints
- Background jobs
- Integration calls
- Error logs

**Example Flow**:
```
API Request (corr-12345)
    → Application Service (corr-12345)
        → Integration Call (corr-12345)
            → External System (corr-12345 in headers)
```

### Retrieving Logs by Correlation ID

```bash
GET /api/v1/connectors/logs/by-correlation/corr-12345
```

Returns all integration logs across all connectors with this correlation ID.

## Field Mapping Configuration

The `mappingConfiguration` field stores JSON defining how external system fields map to internal ESG data points.

### Example Mapping

```json
{
  "mappings": [
    {
      "externalField": "emp_count",
      "internalField": "totalEmployees",
      "transform": "direct"
    },
    {
      "externalField": "training_hours",
      "internalField": "annualTrainingHours",
      "transform": "sum"
    }
  ]
}
```

### Transform Types

- `direct`: Copy value as-is
- `sum`: Aggregate multiple values
- `average`: Calculate average
- `lookup`: Use lookup table for conversion

## Rate Limiting

Configure `rateLimitPerMinute` to prevent overwhelming external systems:

```json
{
  "rateLimitPerMinute": 60
}
```

This limits the connector to 60 requests per minute across all operations.

## Common Integration Patterns

### Pull Pattern (Data Retrieval)

```csharp
var log = await integrationExecutionService.ExecuteWithRetryAsync(
    connectorId: 1,
    operationType: "pull",
    correlationId: correlationId,
    initiatedBy: "scheduler",
    executeCallAsync: async () => 
    {
        var client = new HttpClient();
        var response = await client.GetAsync($"{connector.EndpointBaseUrl}/employees");
        
        return new IntegrationCallResult
        {
            HttpMethod = "GET",
            Endpoint = "/employees",
            HttpStatusCode = (int)response.StatusCode,
            ResponseSummary = await response.Content.ReadAsStringAsync()
        };
    });
```

### Push Pattern (Data Submission)

```csharp
var log = await integrationExecutionService.ExecuteWithRetryAsync(
    connectorId: 1,
    operationType: "push",
    correlationId: correlationId,
    initiatedBy: userId,
    executeCallAsync: async () => 
    {
        var client = new HttpClient();
        var payload = JsonSerializer.Serialize(esgData);
        var response = await client.PostAsync(
            $"{connector.EndpointBaseUrl}/submit",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        
        return new IntegrationCallResult
        {
            HttpMethod = "POST",
            Endpoint = "/submit",
            HttpStatusCode = (int)response.StatusCode,
            RequestSummary = SanitizePayload(payload),
            ResponseSummary = await response.Content.ReadAsStringAsync()
        };
    });
```

## Troubleshooting

### Connector Not Executing

**Symptom**: Integration logs show `Skipped` status

**Solution**: Check that connector status is `Enabled`

```bash
POST /api/v1/connectors/1/enable
```

### All Retries Failing

**Symptom**: Integration log shows multiple retry attempts but still fails

**Possible Causes**:
1. Invalid credentials (check secret store reference)
2. External system is down
3. Network connectivity issues
4. Rate limit exceeded

**Debug Steps**:
1. Check error message in integration log
2. Verify secret store contains referenced credentials
3. Test external system availability independently
4. Review rate limit configuration

### Missing Correlation ID

**Symptom**: Cannot trace requests end-to-end

**Solution**: Ensure `CorrelationIdMiddleware` is registered early in the pipeline (before authentication):

```csharp
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
```

## API Reference

See [Integration API Endpoints](#api-endpoints) for complete API documentation.

## Future Enhancements

- **Webhook Receivers**: Support for external systems pushing data to ESG Report Studio
- **Message Queue Integration**: Async processing via message queues
- **Circuit Breaker Pattern**: Automatic protection against cascading failures
- **Metrics Export**: Prometheus/Grafana integration for monitoring
- **Connector Templates**: Pre-built connectors for common systems (SAP, Workday, etc.)
