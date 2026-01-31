# ADR-003: Integration Connector Framework

Status: Accepted  
Date: 2026-01-31  

## Context

ESG Report Studio needs to integrate with external systems (HR, ERP, utilities, etc.) to pull ESG-related data. These integrations must be:
- Standardized and consistent across different external system types
- Secure (no raw credentials in database)
- Resilient (with retry policies for transient failures)
- Auditable (with correlation IDs and detailed logging)
- Controllable (connectors can be enabled/disabled)

## Decision

We will implement a standardized integration connector framework with the following components:

### 1. Connector Entity
Each external system integration is represented by a **Connector** entity with:
- **Metadata**: Name, Type, Description
- **Status**: Enabled or Disabled (prevents outbound calls when disabled)
- **Endpoint Configuration**: Base URL, authentication type
- **Security**: Reference to secret store for credentials (never raw secrets in DB)
- **Capabilities**: Supported operations (pull/push/webhook)
- **Rate Limiting**: Maximum requests per minute
- **Retry Policy**: Max attempts, delay, exponential backoff flag
- **Field Mappings**: JSON configuration for external-to-internal field mapping

### 2. Integration Execution Service
Provides retry logic and failure handling:
- Checks if connector is enabled before executing calls
- Implements configurable retry policy with exponential backoff
- Logs all integration attempts with correlation IDs
- Returns `IntegrationStatus.Skipped` when connector is disabled
- Captures detailed error information for failed calls

### 3. Integration Logging
All integration calls are logged with:
- **Correlation ID**: For tracing across API, background jobs, and integrations
- **Operation details**: Type, HTTP method, endpoint
- **Status tracking**: Success, Failed, Skipped, In Progress
- **Retry information**: Number of attempts made
- **Performance metrics**: Duration in milliseconds
- **Error details**: Messages and stack traces (sanitized)

### 4. Correlation ID Propagation
- Middleware extracts or generates correlation ID from HTTP headers (`X-Correlation-ID`)
- Stored in `HttpContext.Items` for access throughout request pipeline
- Included in response headers
- Added to logging scope for all log entries
- Propagated to integration calls and background jobs

### 5. API Layer
RESTful endpoints for connector management:
- `POST /api/v1/connectors` - Create new connector
- `GET /api/v1/connectors` - List all connectors
- `GET /api/v1/connectors/{id}` - Get connector details
- `PUT /api/v1/connectors/{id}` - Update connector configuration
- `POST /api/v1/connectors/{id}/enable` - Enable connector
- `POST /api/v1/connectors/{id}/disable` - Disable connector
- `GET /api/v1/connectors/{id}/logs` - Get integration logs for connector
- `GET /api/v1/connectors/logs/by-correlation/{correlationId}` - Trace by correlation ID

### 6. Security Practices
- **No raw secrets in database**: Use secret reference pattern (e.g., `SecretStore:HR-ApiKey`)
- **Secret storage**: Credentials stored in Azure Key Vault, AWS Secrets Manager, or similar
- **Secret rotation**: Reference-based approach supports secret rotation without DB changes
- **Audit trail**: All connector changes tracked with user and timestamp
- **Least privilege**: Integration service accounts should have minimal required permissions

### 7. Architecture Adherence
Following the N-Layer architecture defined in ADR-001:
- **Domain**: Connector, IntegrationLog entities; repository interfaces
- **Application**: ConnectorService, IntegrationExecutionService
- **Infrastructure**: EF Core repositories, DbContext
- **API**: Controllers with DTOs (never expose domain entities)

## Consequences

### Positive
- **Consistency**: All external integrations follow same patterns
- **Security**: Centralized credential management via secret store
- **Resilience**: Automatic retries reduce impact of transient failures
- **Auditability**: Correlation IDs enable end-to-end tracing
- **Control**: Enable/disable connectors without code changes
- **Testability**: Services can be unit tested with mocked repositories

### Negative
- **Additional complexity**: More layers and abstractions than direct HTTP calls
- **Setup overhead**: Each new integration requires connector configuration
- **Secret management**: Requires external secret store infrastructure

### Risks and Mitigations
- **Risk**: Secret references could be incorrect or missing  
  **Mitigation**: Validate secret references during connector creation
  
- **Risk**: Retry policy could cause excessive load on external systems  
  **Mitigation**: Configurable rate limits and retry delays per connector
  
- **Risk**: Correlation IDs might not propagate to background jobs  
  **Mitigation**: Ensure background job framework supports context propagation

## Implementation Notes

1. **Secret Store Integration**: Add a `ISecretStoreService` interface in future iteration to retrieve credentials by reference
2. **Background Jobs**: When adding background job support, ensure correlation IDs are captured and propagated
3. **Webhook Support**: Future iteration should add webhook receiver endpoints and event processing
4. **Monitoring**: Consider adding metrics export for integration success/failure rates

## Alternatives Considered

### Direct HTTP client in application services
Rejected because it lacks standardization, retry logic, and audit trails.

### Message queue-based integration
Deferred to future iteration. Current synchronous approach is simpler for initial implementation.

### Per-integration custom implementations
Rejected because it leads to inconsistency and duplicated retry/logging code.

## References
- ADR-001: Architecture style and layering rules
- [OAuth 2.0 RFC 6749](https://tools.ietf.org/html/rfc6749)
- [Retry Pattern (Microsoft)](https://docs.microsoft.com/en-us/azure/architecture/patterns/retry)
- [Correlation ID Pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CorrelationIdentifier.html)
