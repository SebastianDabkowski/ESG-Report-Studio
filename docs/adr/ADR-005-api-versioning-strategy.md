# ADR-005: API Versioning Strategy for Integration Readiness

Status: Accepted  
Date: 2026-01-31  

## Context

As the ESG Report Studio platform evolves and becomes ready for external integrations, we need a stable, versioned API contract that allows:
- Breaking changes to be introduced in a controlled manner
- External systems to integrate without fear of unexpected breakage
- Older API versions to continue functioning during a documented deprecation period
- Consistent error handling across all API endpoints

Without API versioning, any breaking change to the API would immediately break all existing integrations, making the platform unsuitable for production use by external systems.

## Decision

We adopt **URL-based API versioning** using the `/api/v{version}` pattern, implemented through ASP.NET Core's `Asp.Versioning.Mvc` package.

### Key Design Decisions

1. **Versioning Strategy: URL Segment**
   - Pattern: `/api/v1/resource`, `/api/v2/resource`
   - Chosen over header versioning for discoverability and caching
   - Version is explicit in the URL, making it easy to test and debug
   - Supports versioned OpenAPI documentation per version

2. **Version Format**
   - Major version only (v1, v2, v3)
   - Minor/patch changes handled without version bumps (backward compatible)
   - Breaking changes require new major version

3. **Default Behavior**
   - Default version: v1.0
   - Unspecified version requests default to v1 (`AssumeDefaultVersionWhenUnspecified = true`)
   - API version reported in response headers (`ReportApiVersions = true`)

4. **Error Handling**
   - Standardized `ErrorResponse` model with:
     - HTTP status code
     - Error title and detail message
     - **Correlation ID** for request tracing
     - Timestamp
     - Request path
     - Optional validation errors
   - Global exception handler middleware ensures consistency
   - Correlation IDs propagated from existing `CorrelationIdMiddleware`

5. **OpenAPI Documentation**
   - Each API version has its own OpenAPI specification
   - Version-specific metadata in API documentation
   - Swagger UI supports version selection

6. **Deprecation Policy**
   - Deprecated versions marked with `[ApiVersion("1.0", Deprecated = true)]`
   - Minimum 6-month support window for deprecated versions
   - Deprecation announced in release notes and API documentation
   - Clients receive deprecation warnings in response headers

### Implementation Details

**NuGet Packages Added:**
- `Asp.Versioning.Mvc` (9.0.0)
- `Asp.Versioning.Mvc.ApiExplorer` (9.0.0)

**Configuration in Program.cs:**
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
```

**Controller Attributes:**
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MyController : ControllerBase
{
    // Actions
}
```

**Error Response Model:**
- Includes correlation ID from `CorrelationIdMiddleware`
- Consistent format across all errors (validation, exceptions, 404s)
- Development mode includes stack traces
- Production mode sanitizes error details

## Alternatives Considered

### 1. Header-based Versioning
**Approach:** Version specified in custom header like `X-API-Version: 1.0`

**Rejected because:**
- Less discoverable (not visible in URL)
- Harder to test (requires header configuration)
- Caching complications (URLs appear identical)
- Not ideal for browser-based testing

### 2. Query String Versioning
**Approach:** `/api/resource?api-version=1.0`

**Rejected because:**
- Pollutes query string namespace
- Less clean URLs
- Can conflict with other query parameters
- Not as widely adopted

### 3. Content Negotiation (Accept Header)
**Approach:** `Accept: application/vnd.esgreport.v1+json`

**Rejected because:**
- Complex to implement and test
- Poor tooling support
- Not intuitive for developers
- Over-engineering for our use case

### 4. No Versioning (Breaking Changes in Place)
**Rejected because:**
- Unacceptable for production integrations
- Would break all clients on every breaking change
- No migration path for consumers
- Violates principle of stable API contracts

## Consequences

### Positive

1. **Stability for Integrations**
   - External systems can rely on API contracts
   - Breaking changes isolated to new versions
   - Controlled migration path

2. **Clear Communication**
   - Version explicit in URL
   - Easy to document and communicate
   - OpenAPI specs per version

3. **Backward Compatibility**
   - Old versions continue working during deprecation window
   - Clients upgrade at their own pace
   - Reduced risk of integration failures

4. **Consistent Error Handling**
   - All errors follow same format
   - Correlation IDs enable end-to-end tracing
   - Better debugging and support experience

5. **Production Readiness**
   - Platform suitable for external integrations
   - Meets enterprise stability requirements
   - Enables ecosystem development

### Negative

1. **Maintenance Burden**
   - Must maintain multiple API versions simultaneously
   - Increased testing surface
   - Documentation overhead per version

2. **Code Duplication**
   - Controllers/DTOs may need duplication for breaking changes
   - Potential for inconsistent behavior across versions

3. **URL Length**
   - Slightly longer URLs with version segment
   - Minor inconvenience, but worth the tradeoff

### Neutral

1. **Migration Requirement**
   - Existing controllers need updating with version attributes
   - Route patterns must include version placeholder
   - One-time effort, minimal code change

## Compliance with Acceptance Criteria

✅ **Breaking changes only in new versions:**
- API versioning enables introducing v2 while maintaining v1
- Controllers explicitly marked with `[ApiVersion]` attribute

✅ **Older versions continue working:**
- Framework supports multiple concurrent versions
- Deprecation policy provides 6-month window
- Default version fallback ensures smooth transitions

✅ **Consistent error format with correlation ID:**
- `ErrorResponse` model standardizes all errors
- `GlobalExceptionHandlerMiddleware` ensures consistency
- Correlation IDs from existing middleware included in all errors

✅ **URL versioning enforced consistently:**
- All controllers use `/api/v{version:apiVersion}` pattern
- Route constraint validates version format
- ApiExplorer substitutes version in URLs

✅ **OpenAPI specs per version:**
- `ApiExplorer` configured for version grouping
- `ApiVersionTransformer` adds version metadata
- Swagger UI supports version selection

## Notes

- Controllers in the `Integrations` subfolder already used `/api/v1/` pattern, now formalized
- Legacy controllers without versions will be updated to v1 during implementation
- Deprecation announcements will be communicated via release notes and API headers
- Future consideration: Sunset header (`Sunset: Sat, 31 Dec 2026 23:59:59 GMT`) for deprecated versions
