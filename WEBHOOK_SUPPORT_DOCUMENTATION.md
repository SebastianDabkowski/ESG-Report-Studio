# Webhook Support Documentation

## Overview

The ESG Report Studio now supports outbound webhooks that allow external systems to react to key events in near real-time. This feature enables integration developers to build responsive integrations that automatically trigger when important events occur.

## Event Catalogue

The system supports the following webhook event types:

### Data Events

| Event Type | Description | Payload |
|------------|-------------|---------|
| `data.changed` | Triggered when data points are created or updated | Contains the changed data point details |

### Approval Events

| Event Type | Description | Payload |
|------------|-------------|---------|
| `approval.requested` | Triggered when an approval request is created | Contains approval request details |
| `approval.granted` | Triggered when an approval is granted | Contains approval decision details |
| `approval.rejected` | Triggered when an approval is rejected | Contains approval decision details and reason |

### Export Events

| Event Type | Description | Payload |
|------------|-------------|---------|
| `export.started` | Triggered when a report export is initiated | Contains export job details |
| `export.completed` | Triggered when a report export completes successfully | Contains download URL and export metadata |
| `export.failed` | Triggered when a report export fails | Contains error details |

## Webhook Payload Schema

All webhook payloads follow this standard structure:

```json
{
  "event": "event.type",
  "timestamp": "2026-01-31T12:34:56.789Z",
  "correlationId": "unique-correlation-id",
  "data": {
    // Event-specific data
  }
}
```

### Example Payloads

#### Data Change Event

```json
{
  "event": "data.changed",
  "timestamp": "2026-01-31T12:34:56.789Z",
  "correlationId": "data-change-12345",
  "data": {
    "dataPointId": 42,
    "reportingPeriodId": 1,
    "sectionId": 5,
    "value": "150000",
    "unit": "kWh",
    "changedBy": "user@example.com",
    "changedAt": "2026-01-31T12:34:55.000Z"
  }
}
```

#### Approval Granted Event

```json
{
  "event": "approval.granted",
  "timestamp": "2026-01-31T14:20:30.123Z",
  "correlationId": "approval-98765",
  "data": {
    "approvalRequestId": 10,
    "sectionId": 5,
    "approvedBy": "manager@example.com",
    "approvedAt": "2026-01-31T14:20:29.000Z",
    "comments": "Approved after review"
  }
}
```

#### Export Completed Event

```json
{
  "event": "export.completed",
  "timestamp": "2026-01-31T15:45:00.456Z",
  "correlationId": "export-54321",
  "data": {
    "exportId": 7,
    "reportingPeriodId": 1,
    "format": "PDF",
    "downloadUrl": "https://storage.example.com/exports/report-2026.pdf",
    "fileSize": 2048576,
    "generatedBy": "user@example.com",
    "generatedAt": "2026-01-31T15:44:58.000Z"
  }
}
```

## Webhook Security

### Signature Verification

All webhook deliveries include an HMAC-SHA256 signature to ensure authenticity and integrity.

**Request Headers:**

- `X-Webhook-Signature`: HMAC-SHA256 signature of the payload (hex-encoded)
- `X-Webhook-Event`: The event type
- `X-Webhook-Correlation-Id`: Unique correlation ID for tracking

**Signature Generation:**

```
signature = HMAC-SHA256(payload_json, signing_secret)
```

**Verification Example (C#):**

```csharp
using System.Security.Cryptography;
using System.Text;

public bool VerifyWebhookSignature(string payload, string signature, string secret)
{
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    
    using var hmac = new HMACSHA256(keyBytes);
    var hash = hmac.ComputeHash(payloadBytes);
    var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();
    
    return string.Equals(signature, expectedSignature, 
        StringComparison.OrdinalIgnoreCase);
}
```

**Verification Example (Node.js):**

```javascript
const crypto = require('crypto');

function verifyWebhookSignature(payload, signature, secret) {
  const expectedSignature = crypto
    .createHmac('sha256', secret)
    .update(payload)
    .digest('hex');
  
  return signature === expectedSignature;
}
```

### Secret Rotation

Signing secrets can be rotated at any time using the API:

```http
POST /api/v1/webhooks/subscriptions/{id}/rotate-secret
```

After rotation:
1. The new secret is generated immediately
2. The `secretRotatedAt` timestamp is updated
3. All subsequent webhook deliveries use the new secret
4. Update your webhook endpoint to use the new secret

**Best Practices:**
- Rotate secrets regularly (e.g., every 90 days)
- Use a secret management service (Azure Key Vault, AWS Secrets Manager, etc.)
- Never hardcode secrets in your application

## Verification Handshake

When creating a webhook subscription, the system performs a verification handshake:

1. **Subscription Created:** A verification token is generated
2. **Challenge Sent:** The system sends a POST request to your endpoint:
   ```json
   {
     "type": "webhook.verification",
     "token": "verification-token-123",
     "timestamp": "2026-01-31T12:00:00.000Z"
   }
   ```
3. **Response Expected:** Your endpoint must respond with a 2xx status and echo the token in the response body
4. **Verification Complete:** The subscription status changes to `Active`

**Example Endpoint Implementation:**

```javascript
app.post('/webhook', (req, res) => {
  const { type, token } = req.body;
  
  if (type === 'webhook.verification') {
    // Echo the verification token
    return res.json({ token });
  }
  
  // Handle normal webhook events
  // ...
});
```

## Retry Policy

Failed webhook deliveries are automatically retried with exponential backoff:

| Attempt | Delay | Calculation |
|---------|-------|-------------|
| 1 | 0s | Initial attempt |
| 2 | 5s | Base delay |
| 3 | 10s | 5s × 2^1 |
| 4 | 20s | 5s × 2^2 |

**Configuration:**
- **Max Retry Attempts:** 3 (configurable per subscription)
- **Initial Delay:** 5 seconds (configurable)
- **Exponential Backoff:** Enabled by default (can be disabled)

**Failure Handling:**
- After exhausting all retries, the delivery is marked as `Failed`
- The subscription's `consecutiveFailures` counter increments
- After 5 consecutive failures, the subscription is marked as `Degraded`
- Administrators can view failure details via the API

## API Endpoints

### Create Webhook Subscription

```http
POST /api/v1/webhooks/subscriptions
Content-Type: application/json

{
  "name": "My Integration",
  "endpointUrl": "https://myapp.example.com/webhooks",
  "subscribedEvents": ["data.changed", "approval.granted"],
  "description": "Syncs data changes to external CRM",
  "maxRetryAttempts": 3,
  "retryDelaySeconds": 5,
  "useExponentialBackoff": true
}
```

**Response:**

```json
{
  "id": 1,
  "name": "My Integration",
  "endpointUrl": "https://myapp.example.com/webhooks",
  "subscribedEvents": ["data.changed", "approval.granted"],
  "status": "PendingVerification",
  "verifiedAt": null,
  "consecutiveFailures": 0,
  "createdAt": "2026-01-31T12:00:00.000Z",
  "createdBy": "user@example.com"
}
```

### List Subscriptions

```http
GET /api/v1/webhooks/subscriptions
```

### Get Subscription

```http
GET /api/v1/webhooks/subscriptions/{id}
```

### Update Subscription

```http
PUT /api/v1/webhooks/subscriptions/{id}
Content-Type: application/json

{
  "name": "Updated Integration",
  "endpointUrl": "https://myapp.example.com/webhooks",
  "subscribedEvents": ["data.changed", "approval.granted", "export.completed"],
  "maxRetryAttempts": 5
}
```

### Activate Subscription

```http
POST /api/v1/webhooks/subscriptions/{id}/activate
```

### Pause Subscription

```http
POST /api/v1/webhooks/subscriptions/{id}/pause
```

### Rotate Signing Secret

```http
POST /api/v1/webhooks/subscriptions/{id}/rotate-secret
```

**Response:**

```json
{
  "message": "Signing secret rotated successfully"
}
```

### Delete Subscription

```http
DELETE /api/v1/webhooks/subscriptions/{id}
```

### Get Delivery History

```http
GET /api/v1/webhooks/subscriptions/{id}/deliveries?skip=0&take=50
```

**Response:**

```json
[
  {
    "id": 123,
    "webhookSubscriptionId": 1,
    "eventType": "data.changed",
    "correlationId": "data-change-12345",
    "status": "Succeeded",
    "attemptCount": 1,
    "lastHttpStatusCode": 200,
    "createdAt": "2026-01-31T12:34:56.789Z",
    "lastAttemptAt": "2026-01-31T12:34:57.123Z",
    "completedAt": "2026-01-31T12:34:57.456Z"
  }
]
```

### Get Failed Deliveries

```http
GET /api/v1/webhooks/subscriptions/{id}/failed-deliveries?skip=0&take=50
```

### Get Event Catalogue

```http
GET /api/v1/webhooks/events
```

**Response:**

```json
{
  "supportedEvents": [
    "data.changed",
    "approval.requested",
    "approval.granted",
    "approval.rejected",
    "export.started",
    "export.completed",
    "export.failed"
  ]
}
```

## Subscription Statuses

| Status | Description |
|--------|-------------|
| `PendingVerification` | Awaiting verification handshake |
| `Active` | Verified and receiving events |
| `Paused` | Temporarily disabled by user |
| `Degraded` | Experiencing consecutive failures |
| `Disabled` | Permanently disabled |

## Best Practices

### 1. Idempotency

Always design webhook endpoints to be idempotent. Use the `correlationId` to detect duplicate deliveries:

```javascript
const processedEvents = new Set();

app.post('/webhook', async (req, res) => {
  const { correlationId, event, data } = req.body;
  
  if (processedEvents.has(correlationId)) {
    // Already processed, return success
    return res.status(200).json({ status: 'ok' });
  }
  
  // Process the event
  await processEvent(event, data);
  
  // Mark as processed
  processedEvents.add(correlationId);
  
  res.status(200).json({ status: 'ok' });
});
```

### 2. Quick Response

Respond to webhooks quickly (within 5 seconds). Perform heavy processing asynchronously:

```javascript
app.post('/webhook', async (req, res) => {
  // Acknowledge immediately
  res.status(202).json({ status: 'accepted' });
  
  // Process asynchronously
  setImmediate(async () => {
    await processWebhook(req.body);
  });
});
```

### 3. Error Handling

Return appropriate HTTP status codes:
- **2xx:** Success (delivery will be marked as succeeded)
- **4xx:** Client error (delivery will fail without retry)
- **5xx:** Server error (delivery will be retried)

### 4. Logging

Log all webhook deliveries for debugging:

```javascript
app.post('/webhook', async (req, res) => {
  const { event, correlationId } = req.body;
  
  logger.info('Webhook received', {
    event,
    correlationId,
    timestamp: new Date().toISOString()
  });
  
  // Process webhook...
});
```

### 5. Monitoring

Monitor webhook health:
- Track delivery success rates
- Alert on consecutive failures
- Monitor processing time
- Set up dashboard for webhook metrics

## Troubleshooting

### Webhook Not Receiving Events

1. **Check subscription status:** Ensure status is `Active`
2. **Verify event types:** Confirm you're subscribed to the correct events
3. **Check endpoint:** Ensure your endpoint is accessible and responding with 2xx
4. **Review delivery history:** Check for errors in failed deliveries

### Signature Verification Failing

1. **Use raw body:** Ensure you're verifying the raw request body, not parsed JSON
2. **Check secret:** Verify you're using the current signing secret
3. **Character encoding:** Use UTF-8 encoding for both payload and secret
4. **Case sensitivity:** Signatures are case-insensitive (lowercase hex)

### Subscription Marked as Degraded

1. **Check endpoint health:** Ensure your endpoint is accessible
2. **Review failed deliveries:** Look for patterns in error messages
3. **Check rate limits:** Ensure you're not hitting rate limits
4. **Reactivate:** Once issues are resolved, use the activate endpoint

## Support

For additional support or questions about webhook integration:
- Review the [Integration Connector Framework Guide](INTEGRATION_CONNECTOR_FRAMEWORK_GUIDE.md)
- Check the API documentation at `/api/v1/webhooks/events`
- Contact the development team
