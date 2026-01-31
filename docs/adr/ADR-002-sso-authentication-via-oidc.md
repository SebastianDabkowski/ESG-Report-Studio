# ADR-002: SSO Authentication via OIDC (OpenID Connect)

Status: Proposed  
Date: 2026-01-31  

## Context

The ESG Report Studio requires secure authentication and centralized user management. Organizations need to:
- Authenticate users via their existing identity provider (IdP)
- Sync user profiles from OIDC claims to maintain current user information
- Control access based on user status in the identity provider
- Support fallback to local authentication for development/testing environments

The system must handle:
- SSO login via OIDC protocol
- Automatic user profile creation/update from claims
- Denial of access for disabled users in the IdP
- Multiple authentication modes (OIDC, local auth fallback)

## Decision

Implement OpenID Connect (OIDC) authentication using JWT Bearer tokens with the following architecture:

### Authentication Stack
- **Protocol**: OpenID Connect (OIDC)
- **Token Type**: JWT Bearer tokens
- **ASP.NET Core Package**: `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.12)
- **OIDC Package**: `Microsoft.AspNetCore.Authentication.OpenIdConnect` (9.0.12)

### Configuration Model
Authentication settings are configured via `appsettings.json` with environment-specific overrides:

```json
{
  "Authentication": {
    "EnableLocalAuth": false,
    "Oidc": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Scope": "openid profile email",
      "NameClaimType": "preferred_username",
      "EmailClaimType": "email",
      "DisplayNameClaimType": "name"
    }
  }
}
```

### User Profile Synchronization
A dedicated `IUserProfileSyncService` handles:
1. **Claim Extraction**: Maps OIDC claims to user properties using configurable claim types
2. **User Lookup**: Checks if user exists in the system
3. **Profile Update**: Updates user information if claims have changed
4. **User Creation**: Creates new user profile for first-time login
5. **Status Check**: Validates user is active before granting access

The service is invoked during token validation via JWT Bearer events.

### Token Validation Flow
1. Client presents JWT Bearer token in Authorization header
2. ASP.NET Core validates token signature and claims via OIDC provider
3. `OnTokenValidated` event fires
4. System syncs user profile from token claims
5. System checks if user is active (`IsActive` flag)
6. If user is inactive/disabled, authentication fails with error

### Disabled User Handling
Users disabled in the identity provider are denied access through two mechanisms:
1. **IdP Level**: Disabled users cannot obtain valid tokens from the IdP
2. **Application Level**: `IsActive` flag check in `UserProfileSyncService.IsUserActiveAsync()`

### Fallback Authentication
When OIDC is disabled (`Oidc.Enabled = false`):
- System allows development/testing without external IdP
- Local authentication can be enabled via `EnableLocalAuth` flag
- Useful for CI/CD pipelines and local development

### Authentication Controller
New `/api/auth` endpoints provide:
- `GET /api/auth/config` - Public endpoint returning auth configuration (OIDC enabled, authority, client ID)
- `GET /api/auth/me` - Protected endpoint returning current authenticated user
- `GET /api/auth/status` - Check authentication status

### Claim Type Mapping
Supports both standard and custom claim types via fallback mechanism:
- Primary: Configurable OIDC claim types (e.g., `preferred_username`)
- Fallback: Standard CLR claim types (e.g., `ClaimTypes.NameIdentifier`)

This allows compatibility with various identity providers (Azure AD, Okta, Auth0, Keycloak).

## Architecture Impact

### Layering
- **Api Layer**: JWT Bearer authentication middleware, authentication controller
- **Application Layer**: `IUserProfileSyncService` interface and implementation
- **Domain Layer**: No changes (user entity already exists)
- **Infrastructure Layer**: Not needed for OIDC (handled by middleware)

### Dependency Direction
- Program.cs → UserProfileSyncService (for DI registration)
- AuthenticationController → IUserProfileSyncService
- JWT Bearer events → IUserProfileSyncService

All dependencies follow the established N-Layer rules from ADR-001.

### Cross-Cutting Concerns
- **Logging**: Authentication events logged via `ILogger`
- **Configuration**: Settings injected via `IConfiguration`
- **Error Handling**: Authentication failures return 401 Unauthorized

## MVP Scope

For the MVP, the implementation supports:
- One identity provider per organization (configured globally)
- JWT Bearer token authentication
- Automatic user profile sync on login
- Basic active/inactive user status check

Not included in MVP (future enhancements):
- Multi-tenant IdP configuration (different IdP per organization)
- Role mapping from IdP claims to system roles
- Custom claims-based authorization policies
- Refresh token handling
- Token revocation

## Implementation Notes

### Token Validation Configuration
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidAudience = clientId,
    ValidIssuer = authority,
    NameClaimType = nameClaimType,
    RoleClaimType = "roles"
};
```

### Event Handlers
- `OnTokenValidated`: Syncs user profile and validates active status
- `OnAuthenticationFailed`: Logs authentication failures for diagnostics

### Testing Strategy
- Unit tests for `UserProfileSyncService` with mocked configuration
- Integration tests would require test IdP (not in MVP scope)
- Manual testing with real IdP (Azure AD, etc.)

## Alternatives Considered

### 1. Cookie-based Authentication
**Rejected**: Not suitable for API-first architecture; complicates frontend integration.

### 2. OAuth 2.0 without OIDC
**Rejected**: OIDC provides standardized user info endpoint and claims; pure OAuth requires custom user profile endpoint.

### 3. ASP.NET Core Identity
**Rejected**: Designed for local user management; OIDC is better for SSO scenarios.

### 4. IdentityServer/Duende
**Rejected**: Unnecessary complexity for MVP; we're integrating with existing IdP, not building one.

## Consequences

### Positive
- **Security**: Leverages enterprise IdP security (MFA, conditional access, etc.)
- **User Experience**: Single sign-on reduces password fatigue
- **Maintainability**: Central user management in IdP, not application database
- **Compliance**: Inherits IdP's audit trails and security policies
- **Flexibility**: Configuration-based claim mapping supports various IdPs

### Negative
- **Dependency**: Requires external IdP availability for authentication
- **Complexity**: Additional configuration required per environment
- **Testing**: Harder to test without IdP or mock server
- **Initial Setup**: Organization must configure IdP application registration

### Migration Path
For organizations currently using local authentication:
1. Configure OIDC settings in `appsettings.json`
2. Register application in IdP (Azure AD, Okta, etc.)
3. Enable `Oidc.Enabled = true`
4. Optionally keep `EnableLocalAuth = true` during transition
5. Test authentication with small user group
6. Disable local auth when satisfied

## References

- [Microsoft Identity Platform (Azure AD)](https://learn.microsoft.com/en-us/azure/active-directory/develop/)
- [OpenID Connect Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme)

## Related ADRs

- ADR-001: Architecture style and layering rules
