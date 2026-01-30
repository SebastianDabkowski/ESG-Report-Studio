# Retention Policy Configuration Guide

## Overview

This guide explains how to configure and manage audit data retention policies in ESG Report Studio.

## Quick Start

### 1. Create Your First Retention Policy

The simplest retention policy applies to all tenants and data categories:

```bash
curl -X POST https://your-domain/api/retention/policies \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{
    "dataCategory": "all",
    "retentionDays": 365,
    "allowDeletion": true
  }'
```

This creates a 1-year retention policy for all data.

### 2. Test with Dry Run

Before deleting any data, always preview what will be deleted:

```bash
curl -X POST https://your-domain/api/retention/cleanup \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{
    "dryRun": true
  }'
```

Review the response:
```json
{
  "success": true,
  "wasDryRun": true,
  "recordsIdentified": 127,
  "recordsDeleted": 0,
  "executedAt": "2024-01-15T10:45:00Z"
}
```

### 3. Execute Cleanup

If the preview looks good, run the actual cleanup:

```bash
curl -X POST https://your-domain/api/retention/cleanup \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{
    "dryRun": false
  }'
```

## Retention Policy Concepts

### Data Categories

You can set different retention periods for different types of data:

- **`all`**: Applies to all data types (default)
- **`audit-log`**: Only audit log entries
- **`evidence`**: Only evidence files and metadata

### Tenant Specificity

Policies can be:
- **Global**: Applies to all tenants (`tenantId: null`)
- **Tenant-specific**: Applies only to one tenant (`tenantId: "tenant-123"`)

### Report Type Specificity

Policies can be:
- **Universal**: Applies to all report types (`reportType: null`)
- **Type-specific**: Applies to specific reporting modes (`reportType: "simplified"` or `"extended"`)

### Priority Resolution

When multiple policies could apply, the most specific policy wins:

1. Tenant + Report Type + Category (Priority: 18)
2. Tenant + Category (Priority: 13)
3. Tenant + Report Type (Priority: 15)
4. Tenant only (Priority: 10)
5. Report Type + Category (Priority: 8)
6. Category only (Priority: 3)
7. Default/Global (Priority: 0)

## Common Configuration Scenarios

### Scenario 1: Single Organization, Simple Policy

You have one organization and want to keep audit data for 2 years:

```bash
POST /api/retention/policies
{
  "dataCategory": "all",
  "retentionDays": 730,
  "allowDeletion": true
}
```

### Scenario 2: Multi-Tenant with Different Requirements

Default: 1 year for most tenants
```bash
POST /api/retention/policies
{
  "dataCategory": "all",
  "retentionDays": 365,
  "allowDeletion": true
}
```

Regulated tenant: 7 years, no auto-deletion
```bash
POST /api/retention/policies
{
  "tenantId": "regulated-financial-corp",
  "dataCategory": "all",
  "retentionDays": 2555,
  "allowDeletion": false
}
```

### Scenario 3: Different Retention by Data Type

Audit logs: 2 years
```bash
POST /api/retention/policies
{
  "dataCategory": "audit-log",
  "retentionDays": 730,
  "allowDeletion": true
}
```

Evidence: 5 years (longer for compliance)
```bash
POST /api/retention/policies
{
  "dataCategory": "evidence",
  "retentionDays": 1825,
  "allowDeletion": true
}
```

### Scenario 4: Testing/Staging vs Production

Staging environment: 90 days
```bash
POST /api/retention/policies
{
  "tenantId": "staging-tenant",
  "dataCategory": "all",
  "retentionDays": 90,
  "allowDeletion": true
}
```

Production: 3 years
```bash
POST /api/retention/policies
{
  "tenantId": "production-tenant",
  "dataCategory": "all",
  "retentionDays": 1095,
  "allowDeletion": true
}
```

## Managing Retention Policies

### View All Policies

```bash
curl -X GET "https://your-domain/api/retention/policies" \
  -H "X-User-Id: admin-user-id"
```

### View Active Policies Only

```bash
curl -X GET "https://your-domain/api/retention/policies?activeOnly=true" \
  -H "X-User-Id: admin-user-id"
```

### Check Which Policy Applies

To see which policy would be used for a specific context:

```bash
curl -X GET "https://your-domain/api/retention/policies/applicable?dataCategory=audit-log&tenantId=tenant-123" \
  -H "X-User-Id: admin-user-id"
```

### Update a Policy

```bash
curl -X PATCH "https://your-domain/api/retention/policies/policy-123" \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{
    "retentionDays": 1095,
    "allowDeletion": false
  }'
```

### Deactivate a Policy

```bash
curl -X DELETE "https://your-domain/api/retention/policies/policy-123" \
  -H "X-User-Id: admin-user-id"
```

Note: This deactivates but doesn't delete the policy (preserved in audit trail).

## Cleanup Operations

### Manual Cleanup

1. **Preview what will be deleted:**
   ```bash
   curl -X POST https://your-domain/api/retention/cleanup \
     -H "X-User-Id: admin-user-id" \
     -H "Content-Type: application/json" \
     -d '{"dryRun": true}'
   ```

2. **Review the results carefully**

3. **Execute cleanup:**
   ```bash
   curl -X POST https://your-domain/api/retention/cleanup \
     -H "X-User-Id: admin-user-id" \
     -H "Content-Type: application/json" \
     -d '{"dryRun": false}'
   ```

### Tenant-Specific Cleanup

To limit cleanup to a specific tenant:

```bash
curl -X POST https://your-domain/api/retention/cleanup \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{
    "dryRun": false,
    "tenantId": "tenant-123"
  }'
```

### Scheduled Cleanup (Recommended)

For production environments, schedule cleanup operations:

1. Create a cron job or scheduled task
2. Run with dry-run first
3. If successful, run actual cleanup
4. Archive deletion reports to secure storage

Example cron (monthly cleanup):
```cron
# Run on first day of month at 2 AM
0 2 1 * * /path/to/cleanup-script.sh
```

Example cleanup script:
```bash
#!/bin/bash

# Dry run first
dry_run=$(curl -s -X POST https://your-domain/api/retention/cleanup \
  -H "X-User-Id: admin-user-id" \
  -H "Content-Type: application/json" \
  -d '{"dryRun": true}')

# Check if dry run was successful
if echo "$dry_run" | grep -q '"success":true'; then
  # Execute actual cleanup
  curl -X POST https://your-domain/api/retention/cleanup \
    -H "X-User-Id: admin-user-id" \
    -H "Content-Type: application/json" \
    -d '{"dryRun": false}'
else
  echo "Dry run failed, skipping cleanup"
  exit 1
fi
```

## Deletion Reports

### View Deletion Reports

```bash
curl -X GET "https://your-domain/api/retention/deletion-reports" \
  -H "X-User-Id: admin-user-id"
```

### Verify Report Integrity

Each deletion report includes:
- **contentHash**: SHA-256 hash of the report metadata
- **signature**: Cryptographic signature for tamper detection

To verify a report hasn't been tampered with:
1. Retrieve the original report
2. Recalculate the content hash
3. Compare with stored hash
4. Verify signature matches

### Archive Deletion Reports

Best practices:
1. Download deletion reports after each cleanup
2. Store in write-once, read-many (WORM) storage
3. Keep separate from operational database
4. Include in regular backups
5. Maintain for regulatory retention period (often 7+ years)

## Access Control

### Who Can Do What

| Operation | Admin | Auditor | Report-Owner | Contributor |
|-----------|-------|---------|--------------|-------------|
| View policies | ✅ | ✅ | ❌ | ❌ |
| Create/update policies | ✅ | ❌ | ❌ | ❌ |
| Run cleanup | ✅ | ❌ | ❌ | ❌ |
| View deletion reports | ✅ | ✅ | ❌ | ❌ |
| View audit log | ✅ | ✅ | ✅ | Own actions only |
| Export audit log | ✅ | ✅ | ❌ | ❌ |

### Setting Up Admin Users

Ensure you have at least one user with admin role:

```sql
-- Example: Update user role in database
UPDATE Users SET Role = 'admin' WHERE Email = 'admin@company.com';
```

### Security Best Practices

1. **Limit admin access**: Only give admin role to trusted personnel
2. **Use service accounts**: For scheduled cleanup, use dedicated service accounts
3. **Audit admin actions**: Review audit log for admin operations regularly
4. **Multi-factor authentication**: Require MFA for admin users
5. **Separate duties**: Different people for policy creation vs cleanup execution

## Compliance Considerations

### Regulatory Requirements

Different regulations have different retention requirements:

| Regulation | Typical Retention | Notes |
|------------|------------------|-------|
| GDPR | Varies by purpose | Right to erasure may apply |
| SOX | 7 years | Financial audit data |
| SEC | 6 years | Securities records |
| HIPAA | 6 years | Healthcare records |
| ISO 27001 | 1-3 years | Security logs |

### Recommended Settings by Industry

**Financial Services:**
```json
{
  "dataCategory": "all",
  "retentionDays": 2555,  // 7 years
  "allowDeletion": false   // Manual approval required
}
```

**Healthcare:**
```json
{
  "dataCategory": "all",
  "retentionDays": 2190,  // 6 years
  "allowDeletion": true
}
```

**General Business:**
```json
{
  "dataCategory": "audit-log",
  "retentionDays": 1095,  // 3 years
  "allowDeletion": true
}
```

### Legal Holds (Future Feature)

For situations where data must be preserved beyond normal retention:
- Litigation
- Regulatory investigation
- Internal investigation

When implemented, legal holds will prevent deletion even if retention period has passed.

## Troubleshooting

### No Data Being Deleted

**Problem**: Cleanup runs but `recordsDeleted` is 0

**Possible Causes**:
1. No data older than retention period
2. `allowDeletion` is false
3. Legal hold is active (future)
4. No active retention policies

**Solution**:
```bash
# Check active policies
curl -X GET "https://your-domain/api/retention/policies?activeOnly=true"

# Verify allowDeletion is true
# Check retention period is appropriate
```

### Too Much Data Being Deleted

**Problem**: Dry run shows more records than expected

**Possible Causes**:
1. Retention period too short
2. Wrong policy being applied
3. Incorrect date calculations

**Solution**:
```bash
# Check which policy applies
curl -X GET "https://your-domain/api/retention/policies/applicable?dataCategory=audit-log"

# Increase retention period if needed
curl -X PATCH "https://your-domain/api/retention/policies/policy-123" \
  -H "Content-Type: application/json" \
  -d '{"retentionDays": 730}'
```

### Permission Denied

**Problem**: 403 error when accessing retention endpoints

**Cause**: User doesn't have admin or auditor role

**Solution**: Verify user role and permissions:
```bash
# Check user details
curl -X GET "https://your-domain/api/users/user-123"
```

## Migration and Upgrades

### Adding Retention to Existing System

1. **Audit current data age:**
   ```sql
   SELECT 
     MIN(Timestamp) as OldestEntry,
     MAX(Timestamp) as NewestEntry,
     COUNT(*) as TotalEntries
   FROM AuditLog
   ```

2. **Create conservative policy:**
   ```json
   {
     "dataCategory": "all",
     "retentionDays": 1095,  // 3 years, adjust based on oldest data
     "allowDeletion": false   // Safe mode
   }
   ```

3. **Run dry-run cleanup:**
   Monitor impact before enabling deletion

4. **Gradually reduce retention:**
   Lower retention period in stages if needed

5. **Enable deletion:**
   Set `allowDeletion: true` when confident

## Monitoring and Alerts

### Key Metrics to Track

1. **Cleanup success rate**: % of successful vs failed cleanups
2. **Data volume trends**: Amount of data deleted per cleanup
3. **Policy effectiveness**: Are policies achieving desired retention?
4. **Deletion report integrity**: Verify signatures regularly

### Recommended Alerts

1. **Cleanup failure**: Alert if cleanup fails
2. **Unexpected deletion volume**: Alert if >10% variance from normal
3. **Policy changes**: Notify on any retention policy modifications
4. **Missing deletion reports**: Alert if cleanup runs without generating reports

## Support and Resources

- **API Documentation**: See RETENTION_API_DOCUMENTATION.md
- **Security Considerations**: Review security section in API docs
- **Compliance Questions**: Contact your compliance team
- **Technical Issues**: Check deletion reports and audit logs for details
