# API Versioning and Deprecation Policy

## Overview

This document defines the API versioning strategy and deprecation policy for the ESG Report Studio platform. These policies ensure stable, predictable API contracts that external integrations can rely on.

## Versioning Strategy

### URL-Based Versioning

All API endpoints use URL segment versioning in the format: `/api/v{version}/resource`

**Examples:**
- `/api/v1/approvals`
- `/api/v1/connectors`
- `/api/v2/reporting` (future version)

### Version Format

- **Major version only** (v1, v2, v3, etc.)
- Minor and patch changes are handled within the same major version
- Breaking changes require a new major version

### Default Behavior

- **Default version:** v1.0
- Requests without explicit version use the default version
- API version information is included in response headers

## What Constitutes a Breaking Change

A change is considered **breaking** if it:

1. **Removes or renames** an existing endpoint
2. **Changes HTTP methods** (e.g., POST → PUT)
3. **Removes or renames** fields in request/response DTOs
4. **Changes data types** of existing fields
5. **Adds required fields** to requests without default values
6. **Changes validation rules** that would reject previously valid requests
7. **Alters error response codes** for existing scenarios
8. **Modifies authentication/authorization requirements**

## Non-Breaking Changes

The following changes are **NOT** considered breaking and can be made within existing versions:

1. **Adding new endpoints**
2. **Adding optional fields** to requests
3. **Adding new fields** to responses (clients should ignore unknown fields)
4. **Adding new values** to enums (clients should handle unknown values gracefully)
5. **Relaxing validation** (accepting more input)
6. **Improving error messages** (without changing status codes)
7. **Performance improvements**
8. **Bug fixes** that restore documented behavior

## Deprecation Policy

### Announcement

When an API version is deprecated:

1. **Release notes** will announce the deprecation
2. **Documentation** will be updated with deprecation warnings
3. **Response headers** will include deprecation information:
   ```
   api-deprecated-versions: 1.0
   Sunset: Sat, 31 Dec 2026 23:59:59 GMT
   Link: </api/v2/docs>; rel="successor-version"
   ```

### Support Window

- **Minimum support:** 6 months from deprecation announcement
- **Recommended migration window:** First 3 months
- **Grace period:** Final 3 months (monitoring for stragglers)

### Deprecation Timeline Example

| Date | Action |
|------|--------|
| Jan 1, 2026 | v2.0 released, v1.0 deprecated |
| Jan-Mar 2026 | Active migration period, support for both versions |
| Apr-Jun 2026 | Grace period, v1.0 still functional but discouraged |
| Jul 1, 2026 | v1.0 removed, only v2.0 supported |

### Controller-Level Deprecation

Individual controllers or endpoints can be deprecated within a version:

```csharp
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/legacy")]
public class LegacyController : ControllerBase
{
    // Deprecated endpoints
}
```

## Introducing New Versions

### When to Create a New Version

Create a new major version when:

1. **Multiple breaking changes** are needed
2. **Fundamental architectural changes** are required
3. **Major feature additions** that conflict with existing design
4. **Security improvements** that require breaking changes

### Version Introduction Process

1. **Design Review**
   - Document all breaking changes
   - Justify the need for a new version
   - Consider backward compatibility options

2. **Implementation**
   - Create new controllers with `[ApiVersion("2.0")]`
   - Update DTOs as needed
   - Maintain v1 controllers during support window

3. **Documentation**
   - Update OpenAPI specifications
   - Create migration guide from v1 to v2
   - Document all changes in release notes

4. **Testing**
   - Ensure v1 continues working
   - Test v2 thoroughly
   - Validate migration path

5. **Communication**
   - Announce v2 availability
   - Provide migration timeline
   - Offer support for migration

## Error Handling Standards

All errors follow a consistent format with correlation IDs:

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "The 'name' field is required",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestamp": "2026-01-31T06:00:00Z",
  "path": "/api/v1/resource",
  "errors": {
    "name": ["The name field is required."]
  }
}
```

### Correlation ID Usage

- **Included in all error responses** for traceability
- **Propagated through logging** for debugging
- **Returned in response header:** `X-Correlation-ID`
- **Accepted in request header:** clients can provide their own correlation ID

## Client Guidelines

### Best Practices for API Consumers

1. **Always specify version explicitly** in production code
2. **Handle unknown fields gracefully** (ignore unknown properties)
3. **Implement proper error handling** with correlation ID logging
4. **Monitor deprecation headers** in responses
5. **Plan for migration** as soon as deprecation is announced
6. **Test against new versions** before production migration

### Example Client Header Handling

```http
GET /api/v1/approvals HTTP/1.1
Host: api.esgreportstudio.com
X-Correlation-ID: client-request-12345
Accept: application/json
```

## Monitoring and Metrics

Platform teams will monitor:

1. **Version usage distribution** (v1 vs v2 traffic)
2. **Deprecated version usage** (who's still on old versions)
3. **Error rates per version**
4. **Migration progress** during deprecation windows

## Examples

### Current State (v1.0)

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approvals")]
public class ApprovalsController : ControllerBase
{
    // Current implementation
}
```

### Future State with Deprecation (v1.0 deprecated, v2.0 current)

**v1 Controller (deprecated):**
```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/approvals")]
public class ApprovalsV1Controller : ControllerBase
{
    // Legacy implementation
}
```

**v2 Controller (current):**
```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/approvals")]
public class ApprovalsController : ControllerBase
{
    // New implementation with breaking changes
}
```

## Related Documents

- [ADR-005: API Versioning Strategy](./adr/ADR-005-api-versioning-strategy.md)
- [Architecture Documentation](../architecture.md)

## Contact

For questions about API versioning or deprecation:
- **Technical Questions:** Create an issue in the repository
- **Migration Support:** Contact the ESG Report Studio team
- **Breaking Change Proposals:** Submit an ADR for review
